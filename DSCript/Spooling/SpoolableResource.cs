using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSCript.Spooling
{
    /// <summary>
    /// Represents an abstract spoolable resource class. Inherited classes should be created using the <see cref="Create&lt;T&gt;"/> method.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="Spooler"/> this spoolable resource is based on.</typeparam>
    public abstract class SpoolableResource<T> : ISpoolableResource
        where T : Spooler
    {
        Spooler ISpoolableResource.Spooler
        {
            get { return this.Spooler; }
            set
            {
                if (value is T)
                    this.Spooler = (T)value;
                else
                    throw new Exception("FATAL ERROR: Spooler type mismatch!");
            }
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

        protected internal SpoolableResource() { }
    }
}
