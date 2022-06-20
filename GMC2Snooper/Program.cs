using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using DSCript;
using DSCript.Models;
using DSCript.Spooling;

using GMC2Snooper.PS2;

namespace GMC2Snooper
{
    class Program
    {
        static VifParser VIF;

        static string Filename = "";

        static int StartIdx = -1;

        static bool Interactive = false;
        static bool BatchRunner = false;
        static bool ViewImages = false;

        static bool RawData = false;

        static bool bDumpTextures = false;
        static bool bDumpMaterials = false;
        static bool bDumpModels = false;

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();

            Console.Title = "GMC2 Snooper";
            
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: gmc2snooper <file> <:index> [:-options] [:--]");
                Console.WriteLine("  Loads the first model package at an index from a chunk file.");
                Console.WriteLine("  If no index is specified, the first one will be loaded.");
                Console.WriteLine("  Additional arguments must begin with '-' and come after the index.");
                Console.WriteLine("  Append '--' at the end of your arguments to interactively load each model.");
                Console.WriteLine("  ** NOTE: File must be a valid PS2 CHNK file from Driv3r or Driver: PL! **");
                return;
            }
            else
            {
                Filename = args[0];

                for (int i = (args.Length - 1); i != 0; i--)
                {
                    var arg = args[i];

                    if (arg == "--" && !Interactive)
                    {
                        Interactive = true;
                        continue;
                    }

                    if (arg.StartsWith("-"))
                    {
                        switch (arg.TrimStart('-'))
                        {
                        case "b":
                        case "batch":
                            BatchRunner = true;
                            continue;
                        case "dV":
                        case "vifdump":
                            bDumpModels = true;
                            continue;
                        case "dM":
                        case "matdump":
                            bDumpMaterials = true;
                            continue;
                        case "dT":
                        case "texdump":
                            bDumpTextures = true;
                            continue;
                        case "viewer":
                            ViewImages = true;
                            continue;
                        case "raw":
                            RawData = true;
                            continue;
                        default:
                            Console.WriteLine($"Unknown argument '{arg}'!");
                            continue;
                        }
                    }

                    if (StartIdx == -1)
                    {
                        StartIdx = int.Parse(arg);
                        continue;
                    }
                }

                // set default index
                if (StartIdx == -1)
                    StartIdx = 1;
            }

            if (!File.Exists(Filename))
            {
                Console.WriteLine("ERROR: File not found.");
                return;
            }

            if (RawData)
            {
                DumpRawData();
            }
            else
            {
                if (StartIdx <= 0)
                {
                    Console.WriteLine("ERROR: Index cannot be zero or negative.");
                    return;
                }

                var chunker = new FileChunker();
                var modPacks = new List<SpoolableBuffer>();

                chunker.SpoolerLoaded += (s, e) => {
                    if (s.Context == 0x32434D47)
                        modPacks.Add((SpoolableBuffer)s);
                };

                chunker.Load(Filename);

                if (modPacks.Count == 0)
                {
                    Console.WriteLine($"ERROR: No model packages were found.");
                    return;
                }

                var idx = (StartIdx - 1);

                if (idx >= modPacks.Count)
                {
                    Console.WriteLine($"ERROR: Index was larger than the actual number of models available.");
                    return;
                }

                if (BatchRunner && Interactive)
                {
                    Console.WriteLine("WARNING: Interactive mode disabled due to batch mode being specified.");
                    Interactive = false;
                }

                // disable image viewer for batched runs
                if (BatchRunner)
                    ViewImages = false;

                while (idx < modPacks.Count)
                {
                    var gmc2 = new ModelPackagePS2();
                    var spooler = modPacks[idx];

                    var parent = spooler.Parent;

                    Console.WriteLine($">> ModelPackage index: {StartIdx}");
                    Console.WriteLine($">> ModelPackage offset: 0x{spooler.BaseOffset:X}");

                    if (parent != null)
                        Console.WriteLine($">> ModelPackage parent: 0x{parent.Context:X8}");

                    using (var ms = spooler.GetMemoryStream())
                    {
                        gmc2.LoadBinary(ms);
                        Console.WriteLine($">> Processed {gmc2.Models.Count} models / {gmc2.Materials.Count} materials.");
                    }

                    VIF = new VifParser();
                    
                    if (bDumpModels)
                    {
                        Console.WriteLine(">> Dumping model info...");
                        DumpModelInfo(gmc2);
                    }

                    if (bDumpMaterials)
                    {
                        Console.WriteLine(">> Dumping material info...");
                        DumpMaterials(gmc2);
                    }

                    if (ViewImages)
                        ProcessTextures(gmc2, idx);

                    if (Interactive)
                    {
                        if ((idx + 1) < modPacks.Count)
                        {
                            Console.WriteLine("Press 'SPACE' to load the next model, or press any key to exit.");

                            if (Console.ReadKey().Key == ConsoleKey.Spacebar)
                            {
                                ++idx;
                                continue;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Operation completed -- no more models left to process.");
                            Console.WriteLine("Press any key to exit.");
                            Console.ReadKey();
                        }
                    }

                    if (BatchRunner)
                    {
                        if ((idx + 1) < modPacks.Count)
                        {
                            ++idx;
                            continue;
                        }
                        else
                        {
                            Console.WriteLine("Operation completed successfully.");
                        }
                    }

                    // that's all, folks!
                    break;
                }
            }
        }

        public static void DumpRawData()
        {
            var buffer = File.ReadAllBytes(Filename);

            VIF = new VifParser();

            using (var ms = new MemoryStream(buffer))
            {
                while (ms.Position < ms.Length)
                {
                    // check alignment
                    if ((ms.Position & 0x3) != 0)
                        ms.Align(4);
                    
                    try
                    {
                        VIF.ReadTag(ms, UnpackValues);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($">> VIFcode read error: '{e.Message}', terminating...");
                        Environment.Exit(1);
                    }

                    DumpVIFTag();

                    //--switch ((VifCommandType)VIF.Code.CMD)
                    //--{
                    //--case VifCommandType.ITop:
                    //--    vBuf.Top = VIF.ITops;
                    //--    break;
                    //--
                    //--case VifCommandType.MsCal:
                    //--case VifCommandType.MsCalf:
                    //--case VifCommandType.MsCnt:
                    //--    // swap buffers
                    //--    vBuf = GetBuffer(VIF);
                    //--    break;
                    //--}
                }
            }
        }

        public class Vertex
        {
            public Vector4 Position;
            public Vector4 Normal;
            public Vector4 Color;

            public Vector3 BlendWeight;
            public Vector2 UV;

            public bool Skip;
            
            public void ApplyScale(Vector4 positionScale)
            {
                //--Position.X *= (100.0f * positionScale.X);
                //--Position.Y *= (100.0f * positionScale.Y);
                //--Position.Z *= (100.0f * positionScale.Z);
                //--Position.W *= (100.0f * positionScale.W);

                Position.X += positionScale.X;
                Position.Y += positionScale.Y;
                Position.Z += positionScale.Z;
            }

            public void SetData(Vector4 vtx, int type, int col)
            {
                switch (type)
                {
                case 0:
                    {
                        switch (col)
                        {
                        case 0:
                            Position = vtx;
                            break;
                        case 1:
                            Normal = vtx;
                            
                            Position.W = Normal.W;
                            break;
                        case 2:
                            UV.X = vtx.X;
                            UV.Y = vtx.Y;
                            //UV /= 32.0f;
                            break;
                        }
                    } break;
                case 1:
                    {
                        switch (col)
                        {
                        case 0:
                            Position = vtx;
                            break;
                        case 1:
                            Color = vtx;
                            break;
                        case 2:
                            Normal = vtx;
                            
                            UV.X = Normal.W;
                            Normal.W = Position.W;
                            break;
                        case 3:
                            BlendWeight.X = vtx.X;
                            BlendWeight.Y = vtx.Y;
                            BlendWeight.Z = vtx.Z;

                            // finish up the UV's
                            UV.Y = vtx.W;
                            //UV /= 32.0f;
                            break;
                        }
                    } break;
                default:
                    throw new InvalidOperationException($"Unsupported vertex type '{type}'");
                }
            }
        }
        
        public class MeshStrip
        {
            public int Type;
            
            public int MaterialId;
            public int MaterialSource;

            public List<Vertex> Vertices;
            
            public MeshStrip(int type)
            {
                Type = type;
            }
        }

        public class LodHolder
        {
            public int Lod;
            public List<MeshStrip> Strips;

            public LodHolder(int lod)
            {
                Lod = lod;
                Strips = new List<MeshStrip>();
            }
        }

        public class ModelHolder
        {
            public List<LodHolder> Lods;

            public ModelHolder(ModelPS2 model)
            {
                Lods = new List<LodHolder>(model.Lods.Count);
            }
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
        public struct VifBuffer
        {
            public float V1;
            public float V2;
            public float V3;
            public float V4;
        }
        
        public static StringBuilder SBU = new StringBuilder();
        
        public static VifBuffer[] VxBuffer = null;
        public static int VxIndex = 0;
        public static int VxCount = 0;
        
        public static unsafe VifBuffer* GetVertexPtr(int index)
        {
            var addrIdx = (index % VIF.Cycle.WriteLength);
            var addr = VIF.Addr + addrIdx;

            var offset = (VxIndex + (index - addrIdx)) * VIF.Cycle.Length;
            
            var rV = __makeref(VxBuffer[offset + addr]);
            var pV = *(IntPtr*)&rV;

            return (VifBuffer*)pV;
        }

        public static unsafe void SetVertex(int index, int slot, bool masked, float value)
        {
            var vtx = GetVertexPtr(index);

            var ptr = &(&vtx->V1)[slot];
            var canWrite = (masked) ? (VIF.Mask[slot] == VifWriteMaskType.Data) : true;

            if (canWrite)
                *ptr = value;
        }

        public static List<Vertex> ReadVertices(int index, int count, SubModelPS2 subModel, Vector4 scale)
        {
            var stride = VIF.Cycle.Length;
            var vertices = new List<Vertex>(count);

            for (int v = 0; v < count; v++)
            {
                var vIdx = (index + v) * stride;
                var vertex = new Vertex();

                vertices.Add(vertex);

                for (int col = 0; col < stride; col++)
                {
                    var vif = VxBuffer[vIdx + col];
                    var vtx = new Vector4(vif.V1, vif.V2, vif.V3, vif.V4);

                    //--sb.Append($"{vtx.X,-12:F4}{vtx.Y,-12:F4}{vtx.Z,-12:F4}{vtx.W,-12:F4}| ");

                    vertex.SetData(vtx, subModel.Type, col);
                }

                //--sb.AppendLine();

                // TODO: ?
                //vertex.ApplyScale(scale);

                //--if (subModel.Flags == 1)
                //--{
                //--    vertex.ApplyScale(scale);
                //--}
            }

            //--sb.AppendLine();

            return vertices;
        }

        public static unsafe void UnpackValues(VifParser parser, VifUnpackType packType, bool flag, bool masked, long[][] values)
        {
            if (VxBuffer == null)
            {
                // create a buffer we can use
                VxBuffer = new VifBuffer[20000 * VIF.Cycle.Length];
                VxIndex = 0;
            }

            VxCount = 0;

            var itof = new Func<long, float>((val) => {
                return Convert.ToSingle(val)/* / 128.0f*/;
            });
            
            switch (packType)
            {
            case VifUnpackType.S_16:
                for (int i = 0; i < values.Length; i++)
                {
                    var val = values[i][0];
                    var fV = itof(val);
                    
                    SetVertex(i, 0, masked, fV);
                    SetVertex(i, 1, masked, fV);
                    SetVertex(i, 2, masked, fV);
                    SetVertex(i, 3, masked, fV);

                    SBU.AppendLine($"-> {fV:F4}");
                }
                break;
            case VifUnpackType.V2_8:
            case VifUnpackType.V2_16:
                for (int i = 0; i < values.Length; i++)
                {
                    var x = values[i][0];
                    var y = values[i][1];

                    var fX = itof(x);
                    var fY = itof(y);
                    
                    SetVertex(i, 0, masked, fX);
                    SetVertex(i, 1, masked, fY);
                    SetVertex(i, 2, masked, fX);
                    SetVertex(i, 3, masked, fY);

                    SBU.AppendLine($"-> {fX:F4}, {fY:F4}");
                }
                break;
            case VifUnpackType.V3_8:
            case VifUnpackType.V3_16:
                for (int i = 0; i < values.Length; i++)
                {
                    var x = values[i][0];
                    var y = values[i][1];
                    var z = values[i][2];

                    var fX = itof(x);
                    var fY = itof(y);
                    var fZ = itof(z);
                    
                    SetVertex(i, 0, masked, fX);
                    SetVertex(i, 1, masked, fY);
                    SetVertex(i, 2, masked, fZ);
                    SetVertex(i, 3, masked, fX);

                    SBU.AppendLine($"-> {fX:F4}, {fY:F4}, {fZ:F4}");
                }
                break;
            case VifUnpackType.V4_8:
            case VifUnpackType.V4_16:
                for (int i = 0; i < values.Length; i++)
                {
                    var x = values[i][0];
                    var y = values[i][1];
                    var z = values[i][2];
                    var w = values[i][3];
                    
                    var fX = itof(x);
                    var fY = itof(y);
                    var fZ = itof(z);
                    var fW = itof(w);
                    
                    SetVertex(i, 0, masked, fX);
                    SetVertex(i, 1, masked, fY);
                    SetVertex(i, 2, masked, fZ);
                    SetVertex(i, 3, masked, fW);
                    
                    SBU.AppendLine($"-> {fX:F4}, {fY:F4}, {fZ:F4}, {fW:F4}");
                }
                break;
            }

            VxCount = (values.Length / VIF.Cycle.WriteLength);
        }

        static bool VertsEqual(Vertex a, Vertex b)
        {
            var posA = a.Position;
            var posB = b.Position;

            return ((posA.X == posB.X) 
                && (posA.Y == posB.Y)
                && (posA.Z == posB.Z)
                && (posA.W == posB.W));
        }

        public static void DumpModelInfo(ModelPackagePS2 gmc2)
        {
            var sb = new StringBuilder();
            var models = new List<ModelHolder>();
            
            // vif tag info :)
            for (int i = 0; i < gmc2.Models.Count; i++)
            {
                var model = gmc2.Models[i];

                Console.WriteLine($"Model {i + 1} / {gmc2.Models.Count}:");
                Console.WriteLine($"  Type: ({model.Type & 0xF}, {(model.Type & 0xF) >> 4})");
                Console.WriteLine($"  UID: {model.UID:X8}");
                Console.WriteLine($"  Handle: {model.Handle:X8}");
                Console.WriteLine($"  Unknown: ({model.Unknown1:X4},{model.Unknown2:X4})");
                Console.WriteLine($"  Box offset: ({model.BoxOffset.X:F4},{model.BoxOffset.Y:F4},{model.BoxOffset.Z:F4})");
                Console.WriteLine($"  Box scale: ({model.BoxScale.X:F4},{model.BoxScale.Y:F4},{model.BoxScale.Z:F4})");

                var holder = new ModelHolder(model);
                models.Add(holder);
                
                for (int l = 0; l < model.Lods.Count; l++)
                {
                    var lod = model.Lods[l];
                    var lodHolder = new LodHolder(l);

                    holder.Lods.Add(lodHolder);

                    Console.WriteLine($"  Lod {l + 1} / {model.Lods.Count}:");
                    Console.WriteLine($"    Mask: {lod.Mask}");
                    Console.WriteLine($"    Tris: {lod.NumTriangles}");
                    Console.WriteLine($"    Scale: ({lod.Scale.X:F4},{lod.Scale.Y:F4},{lod.Scale.Z:F4},{lod.Scale.W:F4})");

                    if (lod.IsDummy)
                        continue;
                    
                    for (int ii = 0; ii < lod.Instances.Count; ii++)
                    {
                        var instance = lod.Instances[ii];
                        var subModel = instance.Model;

                        Console.WriteLine($"    Lod Instance {ii + 1} / {lod.Instances.Count}:");

                        if (instance.HasRotation)
                        {
                            var transform = instance.Rotation;
                            Console.WriteLine($"      Rotation X: ({transform.X.X:F4},{transform.X.Y:F4},{transform.X.Z:F4},{transform.X.W:F4})");
                            Console.WriteLine($"      Rotation Y: ({transform.Y.X:F4},{transform.Y.Y:F4},{transform.Y.Z:F4},{transform.Y.W:F4})");
                            Console.WriteLine($"      Rotation Z: ({transform.Z.X:F4},{transform.Z.Y:F4},{transform.Z.Z:F4},{transform.Z.W:F4})");
                        }

                        if (instance.HasTranslation)
                        {
                            var trn = instance.Translation;
                            Console.WriteLine($"      Translation: ({trn.X:F4},{trn.Y:F4},{trn.Z:F4},{trn.W:F4})");
                        }

                        Console.WriteLine($"      SubModel:");
                        Console.WriteLine($"        Type: {subModel.Type}");
                        Console.WriteLine($"        Flags: {subModel.Flags}");
                        Console.WriteLine($"        Unknown: ({subModel.Unknown1},{subModel.Unknown2})");
                        Console.WriteLine($"        Tex id: {subModel.TextureId}");
                        Console.WriteLine($"        Tex source: {subModel.TextureSource:X4}");

                        if (subModel.HasBoundBox)
                        {
                            var v1 = subModel.BoxOffset;
                            var v2 = subModel.BoxScale;
                            Console.WriteLine($"        Box Offset: ({v1.X:F4},{v1.Y:F4},{v1.Z:F4})");
                            Console.WriteLine($"        Box Scale: ({v2.X:F4},{v2.Y:F4},{v2.Z:F4})");
                        }
                        
                        var startIndex = VxIndex;
                        var offset = 0;

                        var strip = new MeshStrip(subModel.Type) {
                            MaterialId = subModel.TextureId,
                            MaterialSource = subModel.TextureSource
                        };
                        
                        var vertices = new List<Vertex>();
                        
                        strip.MaterialId = subModel.TextureId;
                        strip.MaterialSource = subModel.TextureSource;

                        using (var ms = new MemoryStream(subModel.DataBuffer))
                        {
                            while (ms.Position < ms.Length)
                            {
                                // check alignment
                                if ((ms.Position & 0x3) != 0)
                                    ms.Align(4);

                                try
                                {
                                    VIF.ReadTag(ms, UnpackValues);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine($">> VIFcode read error: '{e.Message}', terminating...");
                                    Environment.Exit(1);
                                }

                                DumpVIFTag();

                                switch ((VifCommandType)VIF.Code.CMD)
                                {
                                case VifCommandType.MsCal:
                                case VifCommandType.MsCalf:
                                case VifCommandType.MsCnt:
                                    var verts = ReadVertices((startIndex + offset), VxCount, subModel, lod.Scale);
                                    vertices.AddRange(verts);

                                    VxIndex += VxCount;
                                    offset += VxCount;
                                    break;
                                }
                            }
                        }

                        // data was still waiting?
                        if (VxIndex == startIndex)
                        {
                            offset += VxCount;
                        
                            var lastVerts = ReadVertices((startIndex + offset), VxCount, subModel, lod.Scale);
                            vertices.AddRange(lastVerts);
                        
                            VxIndex += VxCount;
                        }

                        strip.Vertices = vertices;
                        lodHolder.Strips.Add(strip);
                    }
                }

                Console.WriteLine();
            }

            var minIndex = 0;
            var sbR = new StringBuilder();
            var sbP = new StringBuilder();

            for (int m = 0; m < models.Count; m++)
            {
                var model = models[m];

                for (int l = 0; l < model.Lods.Count; l++)
                {
                    var lod = model.Lods[l];

                    if (l != 1)
                        continue;

                    for (int s = 0; s < lod.Strips.Count; s++)
                    {
                        var strip = lod.Strips[s];

                        var sbV = new StringBuilder(); // v
                        var sbN = new StringBuilder(); // vn
                        var sbT = new StringBuilder(); // vt

                        var nVertices = 0;

                        sbP.AppendLine("[");

                        foreach (var vertex in strip.Vertices)
                        {
                            var pos = vertex.Position;
                            var nor = vertex.Normal;
                            var uv = vertex.UV;
                            
                            // NOTE: YZ-axis flipped!
                            sbV.AppendLine($"v {pos.X:F4} {pos.Z:F4} {pos.Y:F4}");
                            sbN.AppendLine($"vn {nor.X:F4} {nor.Z:F4} {nor.Y:F4}");
                            sbT.AppendLine($"vt {uv.X:F4} {uv.Y:F4} 1.0000");

                            sbP.AppendLine($"\t({pos.X:F4},{pos.Y:F4},{pos.Z:F4},{pos.W:F4}),");

                            sbR.Append($"{pos.X,-10:F4}{pos.Y,-10:F4}{pos.Z,-10:F4}{pos.W,-10:F4}| ");
                            sbR.Append($"{nor.X,-10:F4}{nor.Y,-10:F4}{nor.Z,-10:F4}{nor.W,-10:F4}| ");
                            sbR.Append($"{uv.X,-10:F4}{uv.Y,-10:F4}| ");

                            if (strip.Type == 1)
                            {
                                var clr = vertex.Color;
                                var blw = vertex.BlendWeight;

                                sbR.Append($"{clr.X,-10:F4}{clr.Y,-10:F4}{clr.Z,-10:F4}{clr.W,-10:F4}| ");
                                sbR.Append($"{blw.X,-10:F4}{blw.Y,-10:F4}{blw.Z,-10:F4}| ");
                            }

                            sbR.AppendLine();

                            ++nVertices;
                        }

                        sbP.AppendLine("],");
                        sbR.AppendLine();

                        sb.AppendLine($"# type: {strip.Type}");
                        sb.AppendLine($"# tex id: {strip.MaterialId}");
                        sb.AppendLine($"# src id: {strip.MaterialSource:X4}");
                        sb.AppendLine();

                        sb.AppendLine(sbV.ToString());
                        sb.AppendLine(sbN.ToString());
                        sb.AppendLine(sbT.ToString());

                        sb.AppendLine($"g model_{m + 1:D4}_{l + 1:D4}_{s + 1:D4}");
                        sb.AppendLine($"s 1");

                        for (int i = 2; i < nVertices; i++)
                        {
                            int i0, i1, i2;

                            if ((i % 2) != 0)
                            {
                                i0 = minIndex + i;
                                i1 = minIndex + (i - 1);
                                i2 = minIndex + (i - 2);
                            }
                            else
                            {
                                i0 = minIndex + (i - 2);
                                i1 = minIndex + (i - 1);
                                i2 = minIndex + i;
                            }

                            var v0 = strip.Vertices[i0 - minIndex];
                            var v1 = strip.Vertices[i1 - minIndex];
                            var v2 = strip.Vertices[i2 - minIndex];

                            if (VertsEqual(v0, v1) || VertsEqual(v0, v2) || VertsEqual(v1, v2))
                                continue;
                            
                            sb.AppendLine("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}", i0 + 1, i1 + 1, i2 + 1);
                        }

                        sb.AppendLine();

                        minIndex += nVertices;
                    }
                }
            }

            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "vertices.txt"), sbR.ToString());
            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "vertices.py"), sbP.ToString());
            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "model_dump.obj"), sb.ToString());
        }



        public static string[] VIFModeTypes = new[] {
            "NORMAL",
            "OFFSET",
            "DIFFERENCE",
        };

        public static string[] VIFMaskTypes = new[] {
            "DATA",
            "MASK_ROW",
            "MASK_COL",
            "WRITE_PROTECT",
        };

        public static void DumpVIFTag()
        {
            var vif = VIF.Code;

            var imdt = new VifImmediate(vif.IMDT);
            var cmd = new VifCommand(vif.CMD);

            var cmdName = "";
            var cmdInfo = "";

            var sb = new StringBuilder();

            switch ((VifCommandType)vif.CMD)
            {
            case VifCommandType.Nop:
                cmdName = "NOP";
                break;
            case VifCommandType.StCycl:
                cmdName = "STCYCL";
                cmdInfo = String.Format("{0,-10}{1,-10}",
                    $"CL:{VIF.Cycle.Length},",
                    $"WL:{VIF.Cycle.WriteLength}");
                break;
            case VifCommandType.Offset:
                cmdName = "OFFSET";
                cmdInfo = String.Format("{0,-10}", $"OFFSET:{imdt.IMDT_OFFSET:X}");
                break;
            case VifCommandType.ITop:
                var itop = imdt.IMDT_ITOP;
                cmdName = "ITOP";
                cmdInfo = String.Format("{0,-10}", $"ADDR:{itop:X} ({itop} vertices)");
                break;
            case VifCommandType.StMod:
                var mode = (int)VIF.Mode;
                cmdName = "STMOD";
                cmdInfo = String.Format("{0,-10}", $"MODE:{mode} ({VIFModeTypes[mode]})");
                break;
            case VifCommandType.MsCal:
                cmdName = "MSCAL";
                cmdInfo = String.Format("{0,-10}", $"EXECADDR:{imdt.IMDT_MSCAL:X}");
                break;
            case VifCommandType.MsCnt:
                cmdName = "MSCNT";
                sb.AppendLine($"( {VIF.DumpRegisters()} )");
                break;
            case VifCommandType.StMask:
                var mask = VIF.Mask;

                cmdName = "STMASK";
                cmdInfo = String.Format("{0,-10}", $"MASK:{(int)mask:X8}");

                sb.AppendFormat("-> {0,-16}{1,-16}{2,-16}{3,-16}",
                    "MASK_X",
                    "MASK_Y",
                    "MASK_Z",
                    "MASK_W");

                sb.AppendLine();

                for (int m = 0; m < 4; m++)
                {
                    sb.Append("-> ");

                    for (int mI = 0; mI < 4; mI++)
                        sb.AppendFormat("{0,-16}", VIFMaskTypes[(int)mask[(m * 4) + mI]]);

                    sb.AppendLine($"; V{m + 1}");
                }
                break;
            case VifCommandType.StRow:
                cmdName = "STROW";
                
                sb.AppendLine("-> {0,-16:X8}{1,-16:X8}{2,-16:X8}{3,-16:X8}", VIF.Rows[0], VIF.Rows[1], VIF.Rows[2], VIF.Rows[3]);
                break;
            case VifCommandType.StCol:
                cmdName = "STCOL";
                
                sb.AppendLine("-> {0,-16:X8}{1,-16:X8}{2,-16:X8}{3,-16:X8}", VIF.Cols[0], VIF.Cols[1], VIF.Cols[2], VIF.Cols[3]);
                break;
            case VifCommandType.Flush:
                cmdName = "FLUSH";
                break;
            case VifCommandType.Direct:
                cmdName = "DIRECT";
                cmdInfo = String.Format("{0,-10}", $"SIZE:{imdt.IMDT_DIRECT:X}");
                break;
            default:
                if (Enum.IsDefined(typeof(VifCommandType), (int)vif.CMD))
                {
                    Console.WriteLine($">> Unhandled VIF command '{(VifCommandType)vif.CMD}', I might crash!");
                }
                else
                {
                    var addr = (imdt.ADDR * 16);

                    if (imdt.FLG)
                        addr += VIF.Tops;

                    if (cmd.P == 3)
                    {
                        cmdName = cmd.ToString();
                        cmdInfo = String.Format("{0,-10}{1,-10}",
                            $"ADDR:{addr:X},",
                            $"NUM:{vif.NUM}");
                    }
                    else
                    {
                        cmdName = $"$$CMD_{vif.CMD:X2}$$";
                        cmdInfo = String.Format("{0,-10}{1,-10}{2,-10}",
                            $"ADDR:{imdt.ADDR * 16:X} ({addr:X}),",
                            $"NUM:{vif.NUM},",
                            $"IRQ:{vif.IRQ}");
                    }
                }
                break;
            }

            var props = "";

            if (imdt.FLG)
                props += "+FLAG ";
            if (imdt.USN)
                props += "+UNSIGNED ";

            Console.WriteLine($"  {cmdName,-16}{" : ",4}{props,-16}{": ",4}{cmdInfo,-8}");

            if (sb.Length > 0)
                Console.Write(sb.ToString());

            // dump unpacked values?
            if (SBU.Length > 0)
            {
                Console.Write(SBU.ToString());
                SBU = new StringBuilder();
            }
        }

        public static void DumpMaterials(ModelPackagePS2 gmc2)
        {
            var sb = new StringBuilder();

            for (int m = 0; m < gmc2.Materials.Count; m++)
            {
                var mat = gmc2.Materials[m];

                sb.AppendLine($"material[{m + 1}] {{");
                sb.AppendLine($"  type = {mat.Type.ToString()};");
                sb.AppendLine($"  anim_speed = {mat.AnimationSpeed};");
                sb.AppendLine($"  substances[{mat.Substances.Count}] = [");

                for (int s = 0; s < mat.Substances.Count; s++)
                {
                    var sub = mat.Substances[s];
                    
                    sb.AppendLine($"    substance[{s + 1}] {{");
                    sb.AppendLine($"      type = {sub.Type.ToString()};");
                    sb.AppendLine($"      bin = {sub.Bin:X4};");
                    sb.AppendLine($"      flags = {sub.Flags:X4};");
                    sb.AppendLine($"      textures[{sub.Textures.Count}] = [");

                    for (int t = 0; t < sub.Textures.Count; t++)
                    {
                        var tex = sub.Textures[t];

                        sb.AppendLine($"        texture[{t + 1}] : {tex.GUID:X16} {{");

                        sb.AppendLine($"          comptype = {tex.CompType.ToString()};");
                        sb.AppendLine($"          mipmaps = {tex.MipMaps};");
                        sb.AppendLine($"          regs = {tex.Regs:X2};");
                        sb.AppendLine($"          width = {tex.Width};");
                        sb.AppendLine($"          height = {tex.Height};");
                        sb.AppendLine($"          k = {tex.K:X4};");
                        sb.AppendLine($"          dataoffset = {tex.DataOffset:X};");
                        
                        sb.AppendLine($"          pixmaps[{tex.CLUTs.Count}] = [");

                        foreach (var clut in tex.CLUTs)
                            sb.AppendLine($"            0x{clut:X},");

                        sb.AppendLine("          ];");
                        sb.AppendLine("        },");
                    }

                    sb.AppendLine("      ];");
                    sb.AppendLine("    },");
                }

                sb.AppendLine("  ];");
                sb.AppendLine("};");
            }

            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "materials.txt"), sb.ToString());
            Console.WriteLine(sb.ToString());
        }

        private static byte[][] ReadCLUT(byte[] buffer, int count, int where, bool useAlpha)
        {
            byte[][] clut = new byte[count][];

            for (int i = 0; i < clut.Length; i++)
            {
                var pal = BitConverter.ToUInt32(buffer, where + (i * 4));

                clut[i] = new byte[4];

                byte r = 0x00;
                byte g = 0x00;
                byte b = 0x00;
                byte a = 0xFF;

                if (useAlpha)
                {
                    var alpha = (byte)((pal >> 24) & 0xFF);

                    if (alpha != 0x80)
                        a = (byte)((alpha & 0x7F) << 1);
                }

                r = (byte)(pal & 0xFF);
                g = (byte)((pal >> 8) & 0xFF);
                b = (byte)((pal >> 16) & 0xFF);

                clut[i][0] = a;
                clut[i][1] = r;
                clut[i][2] = g;
                clut[i][3] = b;
            }

            return clut;
        }

        private static Color[] Read4bppCLUT(byte[] buffer, TextureDataPS2 texture, int idx, bool useAlpha = false)
        {
            var where = texture.CLUTs[idx];
            var clut = ReadCLUT(buffer, 16, where, useAlpha);

            Color[] palette = new Color[16];

            for (int i = 0; i < 16; i++)
            {
                var color = clut[i];

                palette[i] =
                    Color.FromArgb(
                        color[0],
                        color[1],
                        color[2],
                        color[3]
                    );
            }

            return palette;
        }

        private static Color[] Read8bppCLUT(byte[] buffer, TextureDataPS2 texture, int idx, bool useAlpha = false)
        {
            var where = texture.CLUTs[idx];
            var clut = ReadCLUT(buffer, 256, where, useAlpha);

            Color[] palette = new Color[256];

            for (int i = 0; i < 256; i++)
            {
                var entry = (i & 0xE7);

                entry |= ((i >> 4) & 0x1) << 3;
                entry |= ((i >> 3) & 0x1) << 4;

                var color = clut[entry];

                palette[i] =
                    Color.FromArgb(
                        color[0],
                        color[1],
                        color[2],
                        color[3]
                    );
            }
            
            return palette;
        }
        
        private static BitmapHelper GetTextureBitmap(byte[] texBuffer, TextureDataPS2 tex, int clutIdx = -1, TextureDataPS2 clutTex = null)
        {
            if (clutIdx == -1)
                clutIdx = 0;

            // use same texture for clut lookup
            if (clutTex == null)
                clutTex = tex;
            
            switch (tex.CompType)
            {
            case PS2TextureCompType.RGBA:
                {
                    return new BitmapHelper(texBuffer, tex.Width, tex.Height, tex.CLUTs[0], PixelFormat.Format32bppArgb);
                }
            case PS2TextureCompType.PAL8:
                {
                    var img = new BitmapHelper(texBuffer, tex.Width, tex.Height, tex.CLUTs[1], PixelFormat.Format8bppIndexed);
                    var clut = Read8bppCLUT(texBuffer, clutTex, clutIdx);

                    img.Unswizzle(tex.Width, tex.Height, SwizzleType.Swizzle8bit);
                    img.SetColorPalette(clut);

                    return img;
                }
            case PS2TextureCompType.PAL4:
                {
                    var img = new BitmapHelper(texBuffer, tex.Width, tex.Height, tex.CLUTs[1], PixelFormat.Format8bppIndexed);
                    var clut = Read4bppCLUT(texBuffer, clutTex, clutIdx);

                    img.Unswizzle(tex.Width, tex.Height, SwizzleType.Swizzle4bit);
                    img.SetColorPalette(clut);

                    return img;
                }
            case PS2TextureCompType.VQ2:
                {
                    Debug.WriteLine("Processing VQ2!");

                    var width = tex.Width >> 1;
                    var height = tex.Height >> 1;
                    
                    var img = new BitmapHelper(width, height, PixelFormat.Format8bppIndexed);
                    var clut = Read8bppCLUT(texBuffer, tex, 1);

                    //var buffer = Swizzlers.UnSwizzle8(texBuffer, width, height, tex.CLUTs[2]);

#if BROKEN_VQ2_CODE
                    var quants = new byte[256 * 4];
                    var texels = new byte[(width * height) * 4];

                    Buffer.BlockCopy(texBuffer, tex.CLUTs[0], quants, 0, quants.Length);
                    Buffer.BlockCopy(texBuffer, tex.CLUTs[2], texels, 0, (width * height));

                    var buffer = Swizzlers.UnSwizzle8(texels, width, height, 0);

                    var offset = 0;
                    var size = Math.Min(width, height);

                    // Decode texture data
                    for (int y = 0; y < height; y += size)
                    {
                        for (int x = 0; x < width; x += size)
                        {
                            for (int y2 = 0; y2 < size; y2++)
                            {
                                for (int x2 = 0; x2 < size; x2++)
                                {
                                    var index = buffer[((quants[x2] << 1) | quants[y2])];
                                    var dstIdx = (((y + y2) * width) + (x + x2)) * 4;

                                    buffer[dstIdx] = index;
                                }
                            }

                            offset += (size * size);
                        }
                    }
#else
                    var buffer = Swizzlers.UnSwizzleVQ2(texBuffer, width, height, tex.CLUTs[2]);
#endif

                    img.Pixels = buffer;
                    img.SetColorPalette(clut);
                    
                    return img;
                }
            case PS2TextureCompType.VQ4:
                {
                    Debug.WriteLine("Processing VQ4!");

                    var img = new BitmapHelper(tex.Width, tex.Height, PixelFormat.Format8bppIndexed);
                    //var clut = Read8bppCLUT(texBuffer, clutTex, clutIdx);

                    //var buffer = Swizzlers.UnSwizzle8(texBuffer, tex.Width, tex.Height, tex.CLUTs[1]);

                    var buffer = Swizzlers.UnSwizzleVQ4(texBuffer, tex.Width, tex.Height, tex.CLUTs[1]);

                    img.Pixels = buffer;
                    //img.SetColorPalette(clut);

                    return img;
                }
            case PS2TextureCompType.HY2:
            case PS2TextureCompType.HY2f:
                {

                } break;
            case PS2TextureCompType.VQ4f:
                {
                    Debug.WriteLine("Processing VQ4f as PAL8!");

                    var img = new BitmapHelper(texBuffer, tex.Width, tex.Height, tex.CLUTs[1], PixelFormat.Format8bppIndexed);
                    var clut = Read8bppCLUT(texBuffer, clutTex, clutIdx);

                    img.Unswizzle(tex.Width, tex.Height, SwizzleType.Swizzle8bit);
                    img.SetColorPalette(clut);

                    return img;
                }
            }
            return null;
        }

        private static Color[] CombineCLUTs(Color[] clutR, Color[] clutG, Color[] clutB, Color[] clutA, int blendMode)
        {
            var clut = new Color[256];

            var closeMatch = new Func<int, int, bool>((a, b) => {
                var max = Math.Max(a, b);
                var min = Math.Min(a, b);
                return (max - min) < 2;
            });

            for (int i = 0; i < 256; i++)
            {
                var a = clutA[i].A;
                var r = clutR[i].R;
                var g = clutG[i].G;
                var b = clutB[i].B;

                switch (blendMode)
                {
                case 1:
                    {
                        r = clutR[i].A;
                        g = clutG[i].A;
                        b = clutB[i].A;
                        a = 0xFF;
                    } break;
                case 2:
                    {
                        r = (byte)(0xFF - (r - clutA[i].R));
                        g = (byte)(0xFF - (g - clutA[i].G));
                        b = (byte)(0xFF - (b - clutA[i].B));
                        a = 0xFF;
                    } break;
                }

                clut[i] = Color.FromArgb(a, r, g, b);
            }

            return clut;
        }

        private static BitmapHelper GetSubstanceBitmap(byte[] texBuffer, SubstanceDataPS2 substance, int blendMode, int idx = 0)
        {
            var tex = substance.Textures[idx];
            var bmap = GetTextureBitmap(texBuffer, tex);
            
            var texList = substance.Textures.GetRange(idx, 4);
            var cluts = new Color[4][];

            var alphaMask = (blendMode >= 1);

            for (int c = 0; c < 4; c++)
                cluts[c] = Read8bppCLUT(texBuffer, texList[c], 0, alphaMask);

            var palette = CombineCLUTs(cluts[0], cluts[1], cluts[2], cluts[3], blendMode);
            
            bmap.SetColorPalette(palette);
            return bmap;
        }
        
        public static void ProcessTextures(ModelPackagePS2 gmc2, int modIdx)
        {
            BMPViewer viewer = new BMPViewer();
            
            if (bDumpTextures)
            {
                var dumpDir = Path.Combine(Environment.CurrentDirectory, "texture_dump");

                if (!Directory.Exists(dumpDir))
                    Directory.CreateDirectory(dumpDir);

                File.WriteAllBytes(Path.Combine(dumpDir, $"{gmc2.UID:D4}[{modIdx:D4}]_buffer.dat"), gmc2.TextureDataBuffer ?? new byte[0]);
            }

            string[] typeNames = {
                "LOD",
                "CLEAN",
                "DAMAGE",
            };

            for (int m = 0; m < gmc2.Materials.Count; m++)
            {
                var material = gmc2.Materials[m];

                for (int s = 0; s < material.Substances.Count; s++)
                {
                    var substance = material.Substances[s];
                    var processAll = true;

                    var texName = $"{m + 1:D4}_{s + 1:D2}";

                    var addToViewer = new Action<BitmapHelper, string>((bmap, name) => {
                        if (bmap == null)
                            return;

                        viewer.AddImageByName(bmap, name);
                    });

                    var dumpTex = new Action<BitmapHelper>((bmap) => {
                        if (bmap == null)
                            return;

                        var outDir = Path.Combine(Environment.CurrentDirectory, "textures");

                        if (!Directory.Exists(outDir))
                            Directory.CreateDirectory(outDir);

                        // so apparently I can't get the real pixel data if the clut was changed?!
                        // this makes no #$%^ing sense!
                        using (var bitmap = new Bitmap(bmap.Bitmap))
                        {
                            var pixels = bitmap.ToByteArray(PixelFormat.Format8bppIndexed);
                            var filename = Path.Combine(outDir, $"{Memory.GetCRC32(pixels):X8}.bmp");

                            bitmap.Save(filename, ImageFormat.Bmp);
                        }
                    });

                    var processTex = new Action<TextureDataPS2, string>((t, name) => {
                        var img = GetTextureBitmap(gmc2.TextureDataBuffer, t);

                        if (bDumpTextures)
                            dumpTex(img);

                        addToViewer(img, name);
                    });

                    var processVTex = new Action<int, SubstanceDataPS2>((type, subst) => {
                        BitmapHelper tex = null;
                        var name = $"{texName}_{typeNames[type]}";

                        switch (type)
                        {
                        case 0:
                        case 1:
                            tex = GetTextureBitmap(gmc2.TextureDataBuffer, substance.Textures[0]);
                            break;
                        case 2:
                            tex = GetTextureBitmap(gmc2.TextureDataBuffer, substance.Textures[0], 0, substance.Textures[1]);
                            break;
                        }

                        if (bDumpTextures)
                            dumpTex(tex);

                        if (ViewImages)
                            addToViewer(tex, name);
                    });

                    var processVBlendTex = new Action<int, SubstanceDataPS2, int>((type, subst, startIdx) => {
                        var name = $"{texName}_{typeNames[type]}";

                        var texD = GetSubstanceBitmap(gmc2.TextureDataBuffer, subst, 0, startIdx);
                        var texA = GetSubstanceBitmap(gmc2.TextureDataBuffer, subst, 1, startIdx);
                        var texM = GetSubstanceBitmap(gmc2.TextureDataBuffer, subst, 2, startIdx);

                        if (bDumpTextures)
                        {
                            dumpTex(texD);
                            dumpTex(texA);
                            dumpTex(texM);
                        }

                        if (ViewImages)
                        {
                            addToViewer(texD, $"{name}");
                            addToViewer(texA, $"{name}[A]");
                            addToViewer(texM, $"{name}[M]");
                        }
                    });

                    if (substance.Type == PS2SubstanceType.Blended)
                    {
                        processAll = false;

                        // process vehicle textures
                        switch (substance.Flags)
                        {
                        case 0:
                            {
                                // lod texture
                                processVBlendTex(0, substance, 0);
                            }
                            break;
                        case 5:
                            {
                                // clean & damage textures
                                if (substance.Textures.Count > 2)
                                {
                                    processVBlendTex(1, substance, 0);
                                    
                                    // damage textures?
                                    if (substance.Textures.Count > 4)
                                        processVBlendTex(2, substance, 4);
                                }
                                else
                                {
                                    // no color mask
                                    processVTex(1, substance);
                                    processVTex(2, substance);
                                }
                            } break;
                        }
                    }

                    // do we process all textures normally?
                    if (processAll)
                    {
                        for (int t = 0; t < substance.Textures.Count; t++)
                        {
                            var texture = substance.Textures[t];
                            processTex(texture, $"{texName}_{t:D2} : {texture.GUID:X16}");
                        }
                    }
                }
            }
            
            if (ViewImages && viewer.HasImages)
            {
                viewer.Init();
                Console.WriteLine("The texture viewer is now ready. Please close it to continue.");

                Application.Run(viewer);
            }
        }
    }
}
