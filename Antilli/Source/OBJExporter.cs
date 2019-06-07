using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using DSCript;
using DSCript.Models;

namespace Antilli
{
    public enum ExportResult
    {
        Success = 0,
        Failed = 1
    }

    /*-------------------------------------------------------------------
     * 
     * Some code borrowed from here:
     * http://wiki.unity3d.com/index.php?title=ExportOBJ
     * 
     * ------------------------------------------------------------------*/
    public static class OBJFile
    {
        static Dictionary<int, string> LODTypes = new Dictionary<int, string>(8) {
            { 0, "H"      },
            { 1, "M"    },
            { 2, "L"       },
            { 3, "UNKNOWN1"   },
            { 4, "VL"  },
            { 5, "SHADOW"    },
            { 6, "UNKNOWN2"   },
            {-1, "UNDEFINED" },
        };

        static readonly string MaterialTemplate =
@"newmtl {0}
Ns 10.0000
Ni 1.5000
d 1.0000
Tr 0.0000
Tf 1.0000 1.0000 1.0000
illum 2
Ka 1.0000 1.0000 1.0000
Kd 1.0000 1.0000 1.0000
Ks 0.0000 0.0000 0.0000
Ke 0.0000 0.0000 0.0000
map_Ka {1}_1_1.dds
map_Kd {1}_1_1.dds" + "\r\n";

        static readonly string NullMaterial =
@"newmtl null_mtl
Ns 10.0000
Ni 1.5000
d 0.5000
Tr 0.5000
Tf 1.0000 1.0000 1.0000
illum 2
Ka 1.0000 0.2500 0.5000
Kd 1.0000 0.2500 0.5000
Ks 0.0000 0.0000 0.0000
Ke 0.0000 0.0000 0.0000" + "\r\n";

        public static ExportResult Export(string path, string filename, ModelPackage modelPackage, UID uid, bool splitMeshByMaterial = false, bool bakeTransforms = false)
        {
            if (modelPackage.SubModels.Count < 1)
            {
                MessageBoxEx.Show("There are no models to export!", "OBJ Exporter", MessageBoxExFlags.ErrorBoxOK);
                return ExportResult.Failed;
            }

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var mtlBuilder  = new StringBuilder();
            var objBuilder  = new StringBuilder();

            var header = String.Format(
                "# Driver Model .OBJ Exporter v0.78b\r\n" +
                "# Exported: {0}\r\n", DateTime.Now);

            objBuilder.AppendLine(header);
            mtlBuilder.AppendLine(header);

            objBuilder.AppendFormat("mtllib {0}.mtl", filename).AppendLines(2);

            var materials = new List<MaterialDataPC>();
            
            var startIndex = 0;
            var modelIndex = 0;

            var hasNullMaterial = false;

            foreach (var part in modelPackage.Models)
            {
                if (part.UID != uid)
                    continue;

                objBuilder.AppendLine("# ---- Model {0} ---- #", ++modelIndex);
                objBuilder.AppendLine();

                for (int g = 0; g < part.Lods.Count; g++)
                {
                    var groups = part.Lods[g].Instances;

                    var lodType = (LODTypes.ContainsKey(g)) ? LODTypes[g] : LODTypes[-1];

                    foreach (var group in groups)
                    {
                        if (group == null)
                            continue;

                        var vPos = new StringBuilder();
                        var vNor = new StringBuilder();
                        var vTex = new StringBuilder();

                        var faces = new StringBuilder();

                        var minIndex = 0;
                        var nVerts = 0;

                        var m1 = group.Transform[0];
                        var m2 = group.Transform[1];
                        var m3 = group.Transform[2];
                        var m4 = group.Transform[3];

                        for (int m = 0; m < group.SubModels.Count; m++)
                        {
                            var mesh = group.SubModels[m];

                            MaterialDataPC material = null;
                            int materialType = modelPackage.FindMaterial(mesh.Material, out material);

                            // build material(s)
                            if (materialType > 0)
                            {
                                var mtlIdx = mesh.Material.Handle + 1;

                                bool isGlobalTexture = (materialType == 1);

                                var mtlName = String.Format("{0}_{1}",
                                    (isGlobalTexture) ? "global_mat" : "mat",
                                    mtlIdx);

                                // add material if needed
                                if (!materials.Contains(material))
                                {
                                    var ddsName = String.Format("{0}_{1}", (isGlobalTexture) ? "global" : filename, mtlIdx);

                                    mtlBuilder.AppendLine(MaterialTemplate, mtlName, ddsName);

                                    for (int s = 1, texIdx = 1; s <= material.Substances.Count; s++)
                                    {
                                        //DSC.Log("material {0} - submaterial {1} - has {2} textures", mtlIdx, s, material.Substances.Count);

                                        foreach (var texture in material.Substances[s - 1].Textures)
                                        {
                                            var texFilename = String.Format("{0}_{1}_{2}.dds", ddsName, s, texIdx++);
                                            FileManager.WriteFile(Path.Combine(path, texFilename), texture.Buffer);
                                        }
                                    }

                                    materials.Add(material);
                                }

                                faces.AppendFormat("usemtl {0}", mtlName).AppendLine();
                            }
                            else
                            {
                                if (!hasNullMaterial)
                                {
                                    mtlBuilder.AppendLine(NullMaterial);
                                    hasNullMaterial = true;
                                }

                                faces.AppendLine("usemtl null_mtl");
                            }

                            var indices = new List<int>();
                            var vertices = mesh.GetVertices(true, ref indices);
                            
                            var vCount = vertices.Count;
                            var tCount = indices.Count;

                            // add vertices
                            foreach (var vertex in vertices)
                            {
                                Vector3 pos = vertex.Position;
                                Vector3 normal = vertex.Normal;
                                Vector2 uv = vertex.UV;

                                if (bakeTransforms)
                                {
                                    pos = new Vector3() {
                                        X = (pos.X * m1.X) + (pos.Y * m2.X) + (pos.Z * m3.X) + m4.X,
                                        Y = (pos.X * m1.Y) + (pos.Y * m2.Y) + (pos.Z * m3.Y) + m4.Y,
                                        Z = (pos.X * m1.Z) + (pos.Y * m2.Z) + (pos.Z * m3.Z) + m4.Z,
                                    };
                                }
                                
                                vPos.AppendLine("v {0:F4} {1:F4} {2:F4}", pos.X, pos.Y, pos.Z);
                                vNor.AppendLine("vn {0:F4} {1:F4} {2:F4}", normal.X, normal.Y, normal.Z);
                                vTex.AppendLine("vt {0:F4} {1:F4} 0.0000", uv.X, -uv.Y);

                                nVerts++;
                            }

                            if (splitMeshByMaterial)
                            {
                                faces.AppendLine("g Model{0:D2}_{1}_{2}", modelIndex, lodType, m + 1);
                                faces.AppendLine("s 1");
                            }

                            // add faces
                            for (int t = 0; t < tCount; t += 3)
                            {
                                faces.AppendFormat("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}",
                                    ((indices[t] + minIndex + 1) + startIndex),
                                    ((indices[t + 1] + minIndex + 1) + startIndex),
                                    ((indices[t + 2] + minIndex + 1) + startIndex)).AppendLine();
                            }

                            minIndex += vCount;
                        }

                        // blender support
                        objBuilder.AppendLine($"o Model{modelIndex:D2}_{lodType}");

                        objBuilder.AppendLine(vPos);
                        objBuilder.AppendLine(vNor);
                        objBuilder.AppendLine(vTex);

                        if (!splitMeshByMaterial)
                        {
                            objBuilder.AppendLine("g Model{0:D2}_{1}", modelIndex, lodType);
                            objBuilder.AppendLine("s 1");
                        }

                        objBuilder.AppendLine(faces.ToString());

                        startIndex += nVerts;
                    }
                }
            }

            var filePath = Path.Combine(path, String.Format("{0}.obj", filename));

            if (mtlBuilder != null)
            {
                var mtlFilePath = Path.Combine(path, String.Format("{0}.mtl", filename));

                using (StreamWriter f = new StreamWriter(mtlFilePath, false, Encoding.Default, mtlBuilder.Length))
                    f.Write(mtlBuilder.ToString());
            }

            using (StreamWriter f = new StreamWriter(filePath, false, Encoding.Default, objBuilder.Length))
                f.Write(objBuilder.ToString());

            return ExportResult.Success;
        }
    }
}
