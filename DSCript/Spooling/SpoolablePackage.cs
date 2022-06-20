using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace DSCript.Spooling
{
    public sealed class SpoolablePackage : Spooler, ICopySpooler<SpoolablePackage>
    {
        bool ICopyCat<SpoolablePackage>.CanCopy(CopyClassType copyType)                         => true;
        bool ICopyCat<SpoolablePackage>.CanCopyTo(SpoolablePackage obj, CopyClassType copyType) => true;

        bool ICopyCat<SpoolablePackage>.IsCopyOf(SpoolablePackage obj, CopyClassType copyType)
        {
            throw new NotImplementedException();
        }

        SpoolablePackage ICopyClass<SpoolablePackage>.Copy(CopyClassType copyType)
        {
            return GetCopy(copyType);
        }

        void ICopyClassTo<SpoolablePackage>.CopyTo(SpoolablePackage obj, CopyClassType copyType)
        {
            CopyTo(obj, copyType);
        }

        protected override bool CanCopy(CopyClassType copyType)
        {
            return true;
        }

        protected override bool CanCopyTo(Spooler obj, CopyClassType copyType)
        {
            return (obj is SpoolablePackage);
        }

        protected override bool IsCopyOf(Spooler obj, CopyClassType copyType)
        {
            throw new NotImplementedException();
        }

        protected override Spooler Copy(CopyClassType copyType)
        {
            return GetCopy(copyType);
        }

        protected override void CopyTo(Spooler obj, CopyClassType copyType)
        {
            var spooler = obj as SpoolablePackage;

            if (spooler == null)
                throw new Exception("Cannot copy spoolable package; type mismatch!");

            CopyTo(spooler, copyType);
        }

        private SpoolablePackage GetCopy(CopyClassType copyType)
        {
            var package = new SpoolablePackage();
            
            CopyTo(package, copyType);

            return package;
        }

        private void CopyTo(SpoolablePackage obj, CopyClassType copyType)
        {
            obj.IsDirty = true;

            CopyParamsTo(obj);

            if (copyType == CopyClassType.DeepCopy)
            {
                foreach (var child in Children)
                {
                    var copy = CopyCatFactory.GetCopy(child, CopyClassType.DeepCopy);

                    obj.Children.Add(copy);
                }
            }

            // finalize our copy
            obj.CommitChanges();
        }

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
                if (IsDirty)
                {
                    Debug.WriteLine("**** AreChangesPending called on a dirty package!");
                    return true;
                }
                
                return base.AreChangesPending;
            }
        }

        private int AlignToSpooler(int offset, Spooler spooler)
        {
            return Memory.Align(offset, (1 << (byte)spooler.Alignment));
        }

        private void ProcessSpooler(Spooler spooler, bool dirty, ref int offset)
        {
            // recalculate it?
            if (dirty)
            {
                // calculate offset
                spooler.Offset = AlignToSpooler(offset, spooler);
                spooler.CommitChanges();
            }

            // update to include the spooler
            offset = (spooler.Offset + spooler.Size + spooler.Description.Length);
        }

        public override void CommitChanges()
        {
            // size recalculation needed?
            if (IsDirty)
            {
                var count = Children.Count;

                // minimum size of chunk + list
                var size = ChunkHeader.SizeOf + (count * ChunkEntry.SizeOf);

                if (count > 0)
                {
                    Spooler last = null;

                    // start recalculating once we encounter a dirty/pending spooler
                    var dirty = false;

                    // process all children
                    for (int i = 0; i < count; i++)
                    {
                        var spooler = Children[i];

                        if (!dirty &&
                            (spooler.IsDirty || (spooler.Offset < size) || spooler.AreChangesPending))
                        {
                            // first dirty/pending spooler found,
                            // recalculate all spoolers after this
                            dirty = true;
                        }

                        ProcessSpooler(spooler, dirty, ref size);
                        last = spooler;
                    }

                    // align the size to the final spooler's alignment
                    size = AlignToSpooler(size, last);
                }

                // update our size
                _size = size;
            }

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
                if (IsDirty)
                {
                    Debug.WriteLine("**** Package.Size accessed with pending calculations, committing...");

                    CommitChanges();

                    // notify our parents we've been recalculated
                    NotifyAllParents(true);
                }

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

            // do not recalculate size
            IsDirty = false;
        }

        protected internal override void SetCommon(ref ChunkEntry entry)
        {
            base.SetCommon(ref entry);

            _size = entry.Size;
        }
        
        public SpoolablePackage() { }
        public SpoolablePackage(int size)
        {
            _size = size;
        }

        public SpoolablePackage(IEnumerable<Spooler> spoolers)
        {
            // insert children without flagging them as dirty
            _children = new SpoolerCollection(this, spoolers);
            
            // we'll need our size calculated
            IsDirty = true;
        }

        public SpoolablePackage(ref ChunkEntry entry)
            : base(ref entry)
        {
            _size = entry.Size;
        }
    }
}
