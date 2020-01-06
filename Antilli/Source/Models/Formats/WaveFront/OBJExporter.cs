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
map_Ka {1}
map_Kd {1}" + "\r\n";

        static readonly string DefaultMaterial =
@"newmtl default_mtl
Ns 10.0000
Ni 1.5000
d 0.5000
Tr 0.5000
Tf 1.0000 1.0000 1.0000
illum 2
Ka 0.0000 0.0000 0.0000
Kd 0.0000 0.0000 0.0000
Ks 0.0000 0.0000 0.0000
Ke 0.0000 0.0000 0.0000" + "\r\n";

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
                MessageBox.Show("There are no models to export!", "OBJ Exporter", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return ExportResult.Failed;
            }

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var mtlBuilder  = new StringBuilder();
            var objBuilder  = new StringBuilder();

            var header = String.Format(
                "# Driver Model .OBJ Exporter v0.82b\r\n" +
                "# Exported: {0}\r\n", DateTime.Now);

            objBuilder.AppendLine(header);
            mtlBuilder.AppendLine(header);

            objBuilder.AppendFormat("mtllib {0}.mtl", filename).AppendLines(2);

            var materials = new List<MaterialDataPC>();
            
            var startIndex = 0;
            var modelIndex = 0;

            var hasNullMaterial = false;
            var hasDummyMaterial = false;

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
                            int materialType = MaterialManager.Find(mesh.Material, out material);

                            var globalMaterial = (mesh.Material.UID != modelPackage.UID);

                            switch (materialType)
                            {

                            // 
                            // Missing/Null/Undefined Material
                            //
                            case  0:
                            case -1:
                            case -128:
                                if (!hasNullMaterial)
                                {
                                    mtlBuilder.AppendLine(NullMaterial);
                                    hasNullMaterial = true;
                                }

                                // definitely not a global material
                                globalMaterial = false;

                                faces.AppendLine($"# couldn't find material: {mesh.Material}");
                                faces.AppendLine("usemtl null_mtl");
                                break;

                            //
                            // Default/Null Material
                            //
                            case -2:
                            case -3:
                                if (!hasDummyMaterial)
                                {
                                    mtlBuilder.AppendLine(DefaultMaterial);
                                    hasDummyMaterial = true;
                                }

                                globalMaterial = false;

                                faces.AppendLine("usemtl default_mtl");
                                break;
                            }

                            // build material(s)
                            if (materialType > 0)
                            {
                                var mtlIdx = mesh.Material.Handle + 1;
                                var mtlSrc = mesh.Material.UID;
                                
                                var mtlName = String.Format("{0}_{1}",
                                    (globalMaterial) ? "global_mat" : "mat",
                                    mtlIdx);

                                // add material if needed
                                if (!materials.Contains(material))
                                {
                                    ITextureData ddsTexture = null;

                                    var ddsName = (globalMaterial) ? $"{mtlSrc:X4}-{mtlIdx:D4}" : $"{mtlIdx:D4}";

                                    for (int s = 0; s < material.Substances.Count; s++)
                                    {
                                        var substance = material.Substances[s];

                                        for (int t = 0; t < substance.Textures.Count; t++)
                                        {
                                            var texture = substance.Textures[t];
                                            
                                            var texFmt = (texture.UID != 0x01010101) ? "{0:X8}_{1:X8}" : "#{1:X8}";
                                            var texName = String.Format(texFmt, texture.UID, texture.Handle);
                                            
                                            var texFile = String.Format("{0}_{1}_{2}#{3}.dds", ddsName, (s + 1), (t + 1), texName);
                                            var texPath = Path.Combine(path, texFile);

                                            if (ddsTexture == null)
                                            {
                                                // include only primary texture
                                                ddsTexture = texture;
                                                mtlBuilder.AppendLine(MaterialTemplate, mtlName, texFile);
                                            }
                                            
                                            FileManager.WriteFile(texPath, texture.Buffer);
                                        }
                                    }

                                    materials.Add(material);
                                }

                                faces.AppendFormat("usemtl {0}", mtlName).AppendLine();
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
