using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSCript.Spooling
{
    public sealed class SpoolablePackage : Spooler
    {
        private int _size = 0;
        private SpoolerCollection _children;

        /// <summary>
        /// Gets the collection of children attached to this spooler.
        /// </summary>
        public SpoolerCollection Children
        {
            get
            {
                if (_children == null)
                    _children = new SpoolerCollection(this);

                return _children;
            }
        }

        public override bool AreChangesPending
        {
            get
            {
                if (IsModified)
                    return true;

                foreach (var child in Children)
                {
                    if (child.AreChangesPending)
                        return true;
                }

                return false;
            }
        }

        public override void CommitChanges()
        {
            foreach (var child in Children)
                child.CommitChanges();

            base.CommitChanges();
        }

        public override void Dispose()
        {
            // hopefully the detached children get cleaned up
            // otherwise we're in for some SERIOUS memory leaks
            Children.Clear();
            EnsureDetach();
        }

        public override int Size
        {
            get
            {
                int count = Children.Count;
                
                // header size
                int size = (0x10 + (count * 0x10));

                if (count > 0)
                {
                    // helps to recompute sizes when needed
                    int dirtyIndex = -1;

                    // Check to see if the size needs to be (re)calculated
                    if (!IsDirty && _size > 0)
                    {
                        // Check if any of the spoolers are dirty
                        // If not, we'll use the existing size
                        for (int i = 0; i < count; i++)
                        {
                            var s = Children[i];

                            if (s.AreChangesPending)
                            {
                                dirtyIndex = i;
                                break;
                            }
                        }
                    }
                    else
                    {
                        // we need to calculate the size
                        dirtyIndex = 0;
                    }

                    // dirtyIndex is set when a dirty spooler is found (or if _size is not computed)
                    if (dirtyIndex > -1)
                    {
                        if (dirtyIndex > 0)
                        {
                            // use the last clean spooler as a starting point (if applicable)
                            var cleanSpooler = Children[dirtyIndex - 1];

                            size = (cleanSpooler.Offset + cleanSpooler.Size + cleanSpooler.Description.Length);
                        }

                        // start at our dirty spooler and recalculate all spoolers onwards
                        for (int s = dirtyIndex; s < count; s++)
                        {
                            var spooler = Children[s];

                            // Calculate spooler offset
                            size = Memory.Align(size, (1 << (byte)spooler.Alignment));

                            spooler.Offset = size;

                            size += (spooler.Size + spooler.Description.Length);
                        }

                        size = Memory.Align(size, (1 << (byte)Children.Last().Alignment));
                    }
                    else
                    {
                        // no need to calculate the size
                        return _size;
                    }
                }

                return (_size = size);
            }
        }

        public SpoolablePackage() { }
        public SpoolablePackage(int size)
        {
            _size = size;
        }
    }
}
