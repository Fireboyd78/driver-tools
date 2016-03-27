using System;
using System.IO;

namespace DSCript.Spooling
{
    public interface ISpoolableResource
    {
        /// <summary>
        /// Gets or sets the spooler this resource will access during its lifetime.
        /// </summary>
        Spooler Spooler { get; set; }

        /// <summary>
        /// Loads the contents of the spooler into the resource.
        /// </summary>
        void Load();

        /// <summary>
        /// Saves the contents of the resource into the spooler.
        /// </summary>
        void Save();
    }
}
