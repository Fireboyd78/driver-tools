using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using DSCript;
using DSCript.Models;
using DSCript.Spooling;

namespace Antilli
{
    public class DebugModelFile : FileChunker
    {
        public override bool CanSave
        {
            get { return true; }
        }

        protected List<SpoolableBuffer> QueuedPackages { get; set; }

        protected void FixupModelPackage(SpoolableBuffer modelPackage)
        {
            var version = modelPackage.Version;

            if (version != 6)
                throw new InvalidOperationException("Can't fixup model package because of unknown version.");

            var patch_buffer = new byte[256];

            Memory.Fill((MagicNumber)0x99999999, patch_buffer);

            var hex2str = new Func<byte[], string>((buf) => {
                var sb = new StringBuilder();

                for (int i = 0; i < buf.Length; i++)
                {
                    if (i > 0)
                        sb.Append(" ");

                    sb.Append($"{buf[i]:X2}");
                }

                return sb.ToString();
            });
            
            using (var ms = modelPackage.GetMemoryStream())
            {
                var basePatchOffset = 0;

                var patch_base = new Action<string>((desc) => {
                    basePatchOffset = (int)ms.Position;

                    //Debug.WriteLine($"**** patching '{desc}' @ {basePatchOffset:X8} ****");
                });

                var write_patch = new Action<int, bool, string>((size, enabled, desc) => {
                    if (enabled)
                    {
                        var tmpBuf = new byte[size];

                        var baseOffset = ms.Position;
                        var relOffset = (baseOffset - basePatchOffset);

                        var orig = ms.Read(tmpBuf, 0, size);

                        //Debug.WriteLine($" ({relOffset:X2}) {{ {hex2str(tmpBuf)} }} : '{desc}'");

                        Buffer.BlockCopy(patch_buffer, 0, tmpBuf, 0, size);

                        ms.Position = baseOffset;
                        ms.Write(tmpBuf, 0, size);
                    }
                    else
                    {
                        ms.Position += size;
                    }
                });

                var header = new ModelPackageData(version, ms);

                // time to figure out what's important and what's not
                // we're verifying what's actually "junk data" or not!

                // patch parts + lods
                for (int i = 0; i < header.PartsCount; i++)
                {
                    var partOffset = header.PartsOffset + (i * header.PartSize);
                    var lodsOffset = partOffset + 0xA8;

                    for (int l = 0; l < 7; l++)
                    {
                        ms.Position = lodsOffset + (l * header.LODSize);

                        patch_base($"part {i + 1}, lod {l + 1}");

                        ms.Position += 0xC;

                        write_patch(4, true, "2 counts?");
                        write_patch(4, true, "number of meshes?");
                    }
                }
                
                // patch submodels
                for (int i = 0; i < header.MeshGroupsCount; i++)
                {
                    ms.Position = header.MeshGroupsOffset + (i * header.MeshGroupSize);

                    patch_base($"submodel {i + 1}");

                    ms.Position += 4;
                    write_patch(4, true, "junk after offset");

                    // skip transforms and mesh count
                    ms.Position += 0x42;

                    write_patch(2, false, "unknown count?");

                    write_patch(4, true, "junky-looking stuff");
                    write_patch(4, true, "another count?");
                    write_patch(4, true, "junk!?");
                }

                // ;)
                ms.Position = 0x48;
                ms.Write("ANTILLI!");
                
                // commit our changes
                modelPackage.SetBuffer(ms.ToArray());
            }
        }
        
        protected override void OnSpoolerLoaded(Spooler sender, EventArgs e)
        {
            if ((sender is SpoolableBuffer) && ((ChunkType)sender.Context == ChunkType.ModelPackagePC))
            {
                QueuedPackages.Add((SpoolableBuffer)sender);
            }

            base.OnSpoolerLoaded(sender, e);
        }

        protected override void OnFileLoadBegin()
        {
            QueuedPackages = new List<SpoolableBuffer>();

            base.OnFileLoadBegin();
        }

        protected override void OnFileLoadEnd()
        {
            // apply fixups
            Debug.WriteLine($"Applying fixups to {QueuedPackages.Count} package(s)");

            foreach (var spooler in QueuedPackages)
            {
                FixupModelPackage(spooler);
            }

            base.OnFileLoadEnd();
        }

        public DebugModelFile() { }
        public DebugModelFile(string filename) : base(filename) { }
    }
}
