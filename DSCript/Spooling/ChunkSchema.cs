using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSCript.Spooling
{
    public class ChunkSchema : Dictionary<ChunkType, string>
    {
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

        public bool Process(SpoolableResource<SpoolablePackage> resource)
        {
            if (Count == 0)
                throw new Exception("Cannot process an empty schema!");

            var completed = true;
            var spooler = resource.GetInterface().Spooler as SpoolablePackage;

            if (spooler == null)
                throw new Exception("Cannot process a schema on a spoolable resource that has an invalid spooler.");

            if (spooler.Children.Count == this.Count)
            {
                var idx = 0;

                foreach (var kv in this)
                {
                    var s = spooler.Children[idx];

                    if ((ChunkType)s.Magic == kv.Key)
                    {
                    #if DEBUG
                        Console.WriteLine("Setting '{0}'...", kv.Value);
                    #endif
                        SetValue(resource, kv.Value, s);
                    }
                    else
                    {
                        // failed to process schema :(
                        completed = false;
                        break;
                    }

                    idx += 1;
                }
            }
            else
            {
                completed = false;
            }

            return completed;
        }

        public ChunkSchema() : base() { }
    }
}
