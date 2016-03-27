using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSCript.Spooling
{
    /// <summary>
    /// A static class for creating new spoolable resources.
    /// </summary>
    public static class SpoolableResourceFactory
    {
        /// <summary>
        /// Creates a new spoolable resource.
        /// </summary>
        /// <typeparam name="T">The type of spoolable resource to create.</typeparam>
        /// <returns>A new spoolable resource of the specified type.</returns>
        public static T Create<T>()
            where T : ISpoolableResource, new()
        {
            return new T();
        }

        /// <summary>
        /// Creates a new spoolable resource from the specified spooler.
        /// </summary>
        /// <typeparam name="T">The type of spoolable resource to create.</typeparam>
        /// <param name="spooler">The spooler of which a new spoolable resource will be created from.</param>
        /// <returns>A new spoolable resource of the specified type, created from the specified spooler.</returns>
        public static T Create<T>(Spooler spooler)
            where T : ISpoolableResource, new()
        {
            return new T() { Spooler = spooler };
        }
        
        /// <summary>
        /// Creates a new spoolable resource from the specified spooler, and alternatively loads the content from it immediately.
        /// </summary>
        /// <typeparam name="T">The type of spoolable resource to create.</typeparam>
        /// <param name="spooler">The spooler of which a new spoolable resource will be created from.</param>
        /// <param name="loadSpooler">Determines if the spooler should be loaded upon creation.</param>
        /// <returns>A new spoolable resource of the specified type, created from the specified spooler. If specified, the content is loaded first before returning.</returns>
        public static T Create<T>(Spooler spooler, bool loadSpooler)
            where T : ISpoolableResource, new()
        {
            var resource = new T() { Spooler = spooler };

            if (loadSpooler)
                resource.Load();

            return resource;
        }

        public static void Save<T>(T resource)
            where T : ISpoolableResource
        {
            resource.Save();
        }

        /// <summary>
        /// Returns the spooler as a new spoolable resource of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of spoolable resource to create.</typeparam>
        /// <returns>A new spoolable resource of the specified type.</returns>
        public static T AsResource<T>(this Spooler @this)
            where T : ISpoolableResource, new()
        {
            return Create<T>(@this);
        }

        /// <summary>
        /// Returns the spooler as a new spoolable resource of the specified type, and alternatively loads the content from it immediately.
        /// </summary>
        /// <typeparam name="T">The type of spoolable resource to create.</typeparam>
        /// <param name="loadSpooler">Determines if the spooler should be loaded upon creation.</param>
        /// <returns>A new spoolable resource of the specified type. If specified, the content is loaded first before returning.</returns>
        public static T AsResource<T>(this Spooler @this, bool loadSpooler)
            where T : ISpoolableResource, new()
        {
            return Create<T>(@this, loadSpooler);
        }
    }
}
