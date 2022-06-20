using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace DSCript.Spooling
{
    /// <summary>
    /// Represents a collection of spoolers. This class cannot be inherited.
    /// </summary>
    public sealed class SpoolerCollection : Collection<Spooler>
    {
        private SpoolablePackage spoolablePackage;

        private void VerifyAccess(Spooler item)
        {
            if (item.Parent != null)
            {
                if (item.Parent == spoolablePackage)
                    throw new Exception("Cannot add a spooler that is already attached to the current collection.");

                throw new Exception("Cannot add a spooler that is already attached to another collection.");
            }
            else if (item == spoolablePackage)
            {
                throw new Exception("Cannot add a spooler into its own collection of spoolers.");
            }
        }

        protected override void ClearItems()
        {
            foreach (var item in Items)
            {
                item.Parent = null;
                item.IsDirty = false;
                item.Offset = 0;
            }

            base.ClearItems();

            UpdateSpooler(null);
        }

        protected override void InsertItem(int index, Spooler item)
        {
            VerifyAccess(item);

            item.Parent = spoolablePackage;

            base.InsertItem(index, item);

            UpdateSpooler(item);
        }

        protected override void RemoveItem(int index)
        {
            var item = Items[index];

            if (item != null)
            {
                // remove the item
                base.RemoveItem(index);

                // detach the existing item
                item.Parent = null;
                item.IsDirty = false;
                item.Offset = 0;

                UpdateSpooler(null);
            }
        }

        protected override void SetItem(int index, Spooler item)
        {
            if (item != null)
            {
                VerifyAccess(item);

                item.Parent = spoolablePackage;
            }

            if (Items[index] != null)
            {
                // detach existing item
                Items[index].Parent = null;
                Items[index].IsDirty = false;
                Items[index].Offset = 0;
            }

            // replace with the new item
            base.SetItem(index, item);

            UpdateSpooler(item);
        }

        /// <summary>
        /// Returns the <see cref="SpoolablePackage"/> this collection is attached to.
        /// </summary>
        /// <returns>The <see cref="SpoolablePackage"/> this collection is attached to.</returns>
        public SpoolablePackage GetSpoolablePackage()
        {
            return spoolablePackage;
        }

        internal void UpdateSpooler(Spooler spooler)
        {
            // if the package is already dirty, don't update anything
            if (!spoolablePackage.IsDirty)
            {
                if (spooler != null)
                {
                    // mark it as dirty so it gets recalculated
                    spooler.IsDirty = true;
                    spooler.Offset = 0;
                }

                spoolablePackage.NotifyChanges(true);

                var parent = spoolablePackage.Parent;

                while (parent != null)
                {
                    var grandparent = parent.Parent;

                    if (grandparent == null)
                    {
                        // force a size recalculation from the furthest parent possible
                        parent.CommitChanges();
                        break;
                    }

                    parent = grandparent;
                }

                // update ourselves
                if (parent == null)
                    spoolablePackage.CommitChanges();
            }
        }

        internal SpoolerCollection(SpoolablePackage spoolablePackage)
        {
            if (spoolablePackage == null)
                throw new ArgumentNullException("spoolablePackage", "Collection must be attached to a spoolable package.");

            this.spoolablePackage = spoolablePackage;
        }

        internal SpoolerCollection(SpoolablePackage spoolablePackage, IEnumerable<Spooler> spoolers)
            : this(spoolablePackage)
        {
            var count = 0;

            foreach (var spooler in spoolers)
            {
                spooler.Parent = spoolablePackage;
                spooler.IsDirty = false;

                base.InsertItem(count++, spooler);
            }
        }
    }
}
