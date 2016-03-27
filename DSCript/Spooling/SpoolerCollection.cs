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
                item.Parent = null;

            SetDirtyFlag();

            base.ClearItems();
        }

        protected override void InsertItem(int index, Spooler item)
        {
            VerifyAccess(item);
            item.Parent = spoolablePackage;

            SetDirtyFlag();

            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            var item = Items[index];

            if (item != null)
                item.Parent = null;

            SetDirtyFlag();

            base.RemoveItem(index);
        }

        protected override void SetItem(int index, Spooler item)
        {
            if (item != null)
            {
                VerifyAccess(item);
                item.Parent = spoolablePackage;
            }

            // detach existing item
            Items[index].Parent = null;

            SetDirtyFlag();

            base.SetItem(index, item);
        }

        /// <summary>
        /// Returns the <see cref="SpoolablePackage"/> this collection is attached to.
        /// </summary>
        /// <returns>The <see cref="SpoolablePackage"/> this collection is attached to.</returns>
        public SpoolablePackage GetSpoolablePackage()
        {
            return spoolablePackage;
        }

        internal void SetDirtyFlag()
        {
            spoolablePackage.IsDirty = true;
            spoolablePackage.IsModified = true;
        }

        internal SpoolerCollection(SpoolablePackage spoolablePackage)
        {
            if (spoolablePackage == null)
                throw new ArgumentNullException("spoolablePackage", "Collection must be attached to a spoolable package.");

            this.spoolablePackage = spoolablePackage;
        }
    }
}
