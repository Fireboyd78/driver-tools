using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSCript.Spooling
{
    public enum ChunkSchemaType
    {
        /// <summary>
        /// Chunks are expected to be in sequential order. Faster, but more prone to failing.
        /// </summary>
        Sequential,

        /// <summary>
        /// Chunks are not expected to be in sequential order. Slower, but less likely to fail.
        /// </summary>
        NonSequential,

        /// <summary>
        /// Specifies the default schema type. See the <see cref="NonSequential"/> type for more info.
        /// </summary>
        Default = NonSequential
    }

    public class ChunkSchema : Dictionary<ChunkType, string>
    {
        public ChunkSchemaType SchemaType { get; set; }

        protected void SetValue(object backingField, string name, object val)
        {
            var type = backingField.GetType();
            var m    = type.GetProperty(name);

            if (m != null)
            {
                m.SetValue(backingField, val, null);
            }
            else
            {
                throw new Exception("Invalid property name specified - cannot set property value.");
            }
        }

        public List<Spooler> Process(SpoolablePackage spooler)
        {
            if (spooler == null)
                throw new Exception("Cannot process a schema on a null spooler.");
            if (Count == 0)
                throw new Exception("Cannot process an empty schema.");
            
            var spoolers = new List<Spooler>(Count);

            if (SchemaType == ChunkSchemaType.Sequential)
            {
                // sequential requires exact count
                if (spooler.Children.Count != spoolers.Capacity)
                    return null;

                var idx = 0;
                var failed = false;

                foreach (var kv in this)
                {
                    var s = spooler.Children[idx];

                    if ((ChunkType)s.Context == kv.Key)
                    {
                        spoolers.Add(s);
                    }
                    else
                    {
                        // failed to process schema :(
                        failed = true;
                        break;
                    }

                    idx += 1;
                }

                // don't return anything if it failed
                if (failed)
                    return null;
            }
            else
            {
                var anythingLoaded = false;

                foreach (var s in spooler.Children)
                {
                    var key = (ChunkType)s.Context;

                    if (this.ContainsKey(key))
                    {
                        spoolers.Add(s);

                        if (!anythingLoaded)
                            anythingLoaded = true;
                    }
                }

                // don't return anything if nothing was loaded
                if (!anythingLoaded)
                    return null;
            }

            return spoolers;
        }
        
        public bool Process(SpoolableResource<SpoolablePackage> resource)
        {
            if (Count == 0)
                throw new Exception("Cannot process an empty schema!");

            var success = false;
            var spooler = resource.GetInterface().Spooler as SpoolablePackage;

            if (spooler == null)
                throw new Exception("Cannot process a schema on a spoolable resource that has an invalid spooler.");

            if (SchemaType == ChunkSchemaType.Sequential)
            {
                if (spooler.Children.Count == this.Count)
                {
                    var idx = 0;

                    // values to set if sequential chunks found
                    var setValues = new Dictionary<string, Spooler>();
                    var failed = false;

                    foreach (var kv in this)
                    {
                        var s = spooler.Children[idx];

                        if ((ChunkType)s.Context == kv.Key)
                        {
                            setValues.Add(kv.Value, s);
                        }
                        else
                        {
                            // failed to process schema :(
                            failed = true;
                            break;
                        }

                        idx += 1;
                    }

                    if (!failed)
                    {
                        // sequential needs met, now set the values
                        foreach (var kv in setValues)
                            SetValue(resource, kv.Key, kv.Value);

                        success = true;
                    }
                }
            }
            else
            {
                var anythingLoaded = false;

                foreach (var s in spooler.Children)
                {
                    var key = (ChunkType)s.Context;

                    if (this.ContainsKey(key))
                    {
                        SetValue(resource, this[key], s);

                        if (!anythingLoaded)
                            anythingLoaded = true;
                    }
                }

                success = anythingLoaded;
            }

            return success;
        }

        public ChunkSchema() : this(ChunkSchemaType.Default)
        {
        }

        public ChunkSchema(ChunkSchemaType schemaType) : base()
        {
            SchemaType = schemaType;
        }
    }
}
