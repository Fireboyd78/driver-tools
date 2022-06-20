using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DSCript.Spooling
{
    public interface ISpoolableResource<T> : ISpoolableResource
        where T : Spooler
    {
        new T Spooler { get; set; }
    }

    /// <summary>
    /// Represents an abstract spoolable resource class. Inherited classes should be created using the <see cref="SpoolableResourceFactory.Create{T}(Spooler)"/> method.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="Spooler"/> this spoolable resource is based on.</typeparam>
    public abstract class SpoolableResource<T> : ISpoolableResource<T>
        where T : Spooler
    {
        Spooler ISpoolableResource.Spooler
        {
            get { return this.Spooler; }
            set
            {
                if (!(value is T))
                    throw new Exception("FATAL ERROR: Spooler type mismatch!");

                this.Spooler = (T)value;
            }
        }

        T ISpoolableResource<T>.Spooler
        {
            get { return this.Spooler; }
            set { this.Spooler = value; }
        }

        void ISpoolableResource.Load()
        {
            this.VerifyAccess();
            this.Load();
        }

        void ISpoolableResource.Save()
        {
            this.VerifyAccess();
            this.Save();
        }

        protected T Spooler { get; set; }

        protected abstract void Load();
        protected abstract void Save();

        public static explicit operator T(SpoolableResource<T> resource)
        {
            return resource.Spooler;
        }

        internal void VerifyAccess()
        {
            if (Spooler == null)
                throw new Exception("Cannot perform operation on a null spooler!");
        }

        /// <summary>
        /// Returns the public interface for a spoolable resource.
        /// </summary>
        /// <returns>The public interface for a spoolable resource.</returns>
        public ISpoolableResource GetInterface()
        {
            return (ISpoolableResource)this;
        }

        public virtual bool AreChangesPending
        {
            get { return Spooler.AreChangesPending; }
        }

        public virtual void CommitChanges()
        {
            if (Spooler.AreChangesPending)
            {
                Save();
                Spooler.CommitChanges();
            }
        }

        public virtual void NotifyChanges(bool dirty = false)
        {
            Spooler.NotifyChanges(dirty);
        }

        public virtual void Dispose()
        {
            // TODO: figure out safest way to dispose of this
        }

        protected internal SpoolableResource() { }
    }
}
