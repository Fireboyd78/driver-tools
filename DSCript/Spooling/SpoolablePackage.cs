using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public override void NotifyChanges()
        {
            // reset the size
            _size = 0;

            base.NotifyChanges();
        }

        public override void CommitChanges()
        {
            int count = Children.Count;
            var size = ChunkHeader.SizeOf + (count * ChunkEntry.SizeOf);

            if (count > 0)
            {
                Spooler last = null;

                for (int i = 0; i < count; i++)
                {
                    var child = Children[i];

                    // finalize any dirty spoolers,
                    // along with any spoolers following it
                    if (child.AreChangesPending)
                    {
                        child.CommitChanges();

                        if (last != null)
                            size = (last.Offset + last.Size + last.Description.Length);

                        for (int s = i; s < count; s++)
                        {
                            var spooler = Children[s];

                            // Calculate spooler offset
                            spooler.Offset = Memory.Align(size, (1 << (byte)spooler.Alignment));
                            spooler.CommitChanges();

                            size = (spooler.Offset + spooler.Size + spooler.Description.Length);
                            last = spooler;
                        }

                        size = Memory.Align(size, (1 << (byte)last.Alignment));
                        IsModified = true;
                    }

                    // stop processing if we found a dirty spooler
                    if (IsModified)
                        break;

                    last = child;
                }
            }

            if (IsModified || (_size != size))
                _size = size;

            // finalize changes
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
#if !USE_OLD_SIZE_CALCULATION
                if (AreChangesPending)
                {
                    Debug.WriteLine("**** Package.Size accessed with pending changes, committing...");
                    CommitChanges();
                }

                Debug.Assert(!AreChangesPending, "Attempted to access the size of a package with pending changes!");

                return _size;
#else
                int count = Children.Count;
                
                // header size
                int size = ChunkHeader.SizeOf + (count * ChunkEntry.SizeOf);

                if (count > 0)
                {
                    int dirtyIndex = -1;

                    // Check if any of the spoolers are dirty or need calculating
                    for (int i = 0; i < count; i++)
                    {
                        var s = Children[i];

                        if (s.AreChangesPending || (s.Offset < size))
                        {
                            dirtyIndex = i;
                            break;
                        }
                    }
                    
                    if (dirtyIndex == -1)
                    {
                        // if we need to calculate the size,
                        // and assuming all of the children are valid,
                        // we can start calculating at the very end
                        if (IsModified || (_size < size))
                        {
                            dirtyIndex = count;
                        }
                        else
                        {
                            // no need to calculate the size
                            return _size;
                        }
                    }

                    if (dirtyIndex > 0)
                    {
                        // use the spooler just before the dirty one
                        // in some cases, we use the very last one
                        var cleanSpooler = Children[dirtyIndex - 1];

                        size = (cleanSpooler.Offset + cleanSpooler.Size + cleanSpooler.Description.Length);
                    }

                    Spooler last = null;

                    // start at our dirty spooler and recalculate all spoolers onwards
                    // if all spoolers are clean, we won't waste any time ;)
                    for (int s = dirtyIndex; s < count; s++)
                    {
                        var spooler = Children[s];

                        // Calculate spooler offset
                        spooler.Offset = Memory.Align(size, (1 << (byte)spooler.Alignment));

                        size = (spooler.Offset + spooler.Size + spooler.Description.Length);
                        last = spooler;
                    }

                    size = Memory.Align(size, (1 << (byte)last.Alignment));
                }

                return (_size = size);
#endif
            }
        }

        internal void SetSizeInternal(int size)
        {
            _size = size;
        }

        public SpoolablePackage() { }
        public SpoolablePackage(int size)
        {
            _size = size;
        }

        public SpoolablePackage(ref ChunkEntry entry)
            : base(ref entry)
        {
            _size = entry.Size;
        }
    }
}
