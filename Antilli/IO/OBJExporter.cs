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

using Antilli.Models;

namespace Antilli.IO
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

        static string GenTitle()
        {
            return String.Format(
@"# Driver Model .OBJ Exporter v0.6489b by CarLuver69
# Exported: {0}" + "\r\n", DateTime.Now);
        }

        public static ExportResult Export(string path, string filename, ModelPackage modelPackage, long uid, bool exportMaterials)
        {
            if (modelPackage.Meshes.Count < 1)
            {
                MessageBoxEx.Show("There are no models to export!", "OBJ Exporter", MessageBoxExFlags.ErrorBoxOK);
                return ExportResult.Failed;
            }

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            StringBuilder sbMtl = new StringBuilder();

            if (exportMaterials)
                sbMtl.AppendLine(GenTitle());
            else
                sbMtl = null;

            if (exportMaterials)
                sbMtl.AppendLine(GenTitle());
            else
                sbMtl = null;

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(GenTitle());

            sb.AppendFormat("mtllib {0}.mtl", filename).AppendLines(2);

            int startIndex = 0;
            int modelIdx = 0;

            List<PCMPMaterial> materials = new List<PCMPMaterial>();

            foreach (PartsGroup part in modelPackage.Parts)
            {
                if (uid != -1 && part.UID != uid)
                    continue;

                sb.AppendFormatEx("# ============== Group {0} ==============", ++modelIdx).AppendLines(2);

                for (int g = 0; g < part.Parts.Count; g++)
                {
                    MeshGroup group = part.Parts[g].Group;

                    if (group == null)
                        continue;

                    string lodType = (LODTypes.ContainsKey(g)) ? LODTypes[g] : LODTypes[-1];

                    Point3DCollection vertices = new Point3DCollection();
                    Vector3DCollection normals = new Vector3DCollection();
                    PointCollection coords = new PointCollection();

                    Int32Collection indices = new Int32Collection();

                    StringBuilder faces = new StringBuilder();

                    int minIndex = 0;

                    for (int m = 0; m < group.Meshes.Count; m++)
                    {
                        IndexedPrimitive prim = group.Meshes[m];

                        DriverModel3D model = new DriverModel3D(modelPackage, prim);

                        int mtlIdx = prim.MaterialId + 1;

                        int vCount = model.Positions.Count;
                        int tCount = model.TriangleIndices.Count;

                        for (int v = 0; v < vCount; v++)
                        {
                            vertices.Add(model.Positions[v]);
                            normals.Add(model.Normals[v]);
                            coords.Add(model.TextureCoordinates[v]);
                        }

                        //for (int t = 0; t < tCount; t += 3)
                        //{
                        //    indices.Add(model.TriangleIndices[t] + tIdx);
                        //    indices.Add(model.TriangleIndices[t + 1] + tIdx);
                        //    indices.Add(model.TriangleIndices[t + 2] + tIdx);
                        //}

                        bool isSharedTexture = (prim.TextureFlag == ((uint)modelPackage.PackageType) || prim.TextureFlag == 0xFFFD || prim.TextureFlag == 0) ? false : true;

                        string mtlName = String.Format("{0}_{1}",
                                (isSharedTexture)
                                    ? "shared_mat"
                                    : "mat", mtlIdx);

                        PCMPMaterial material = (!isSharedTexture)
                            ? modelPackage.MaterialData.Materials[prim.MaterialId]
                            : ModelPackage.GlobalTextures[prim.MaterialId];

                        if (exportMaterials && !materials.Contains(material))
                        {
                            string ddsName = (isSharedTexture)
                                    ? String.Format("shared_{0}", mtlIdx)
                                    : String.Format("{0}_{1}", uid, mtlIdx);

                            sbMtl.AppendFormatEx(
@"newmtl {0}
    Ns 10.0000
    Ni 1.5000
    d 1.0000
    Tr 0.0000
    Tf 1.0000 1.0000 1.0000
    illum 2
    Ka 0.0000 0.0000 0.0000
    Kd 0.0000 0.0000 0.0000
    Ks 0.0000 0.0000 0.0000
    Ke 0.0000 0.0000 0.0000
    map_Ka {1}_1_1.dds
    map_Kd {1}_1_1.dds", mtlName, ddsName).AppendLines(2);

                            for (int s = 0; s < material.SubMaterials.Count; s++)
                            {
                                int texIdx = 1;

                                DSC.Log("material {0} - submaterial {1} - has {2} textures", mtlIdx, s + 1, material.SubMaterials.Count);

                                foreach (PCMPTextureInfo texture in material.SubMaterials[s].Textures)
                                    texture.ExportFile(String.Format(@"{0}\{1}_{2}_{3}.dds", path, ddsName, s + 1, texIdx++));
                            }

                            materials.Add(material);
                        }

                        faces.AppendFormat("usemtl {0}", mtlName).AppendLine();

                        for (int t = 0; t < tCount; t += 3)
                        {
                            faces.AppendFormat("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}",
                                ((model.TriangleIndices[t] + minIndex + 1) + startIndex),
                                ((model.TriangleIndices[t + 1] + minIndex + 1) + startIndex),
                                ((model.TriangleIndices[t + 2] + minIndex + 1) + startIndex)).AppendLine();
                        }

                        minIndex += vCount;
                    }

                    int nVerts = vertices.Count;
                    int nTris = indices.Count;

                    for (int vx = 0; vx < nVerts; vx++)
                        sb.AppendFormatEx("v {0:F4} {1:F4} {2:F4}", vertices[vx].X, vertices[vx].Y, vertices[vx].Z).AppendLine();

                    sb.AppendLine();

                    for (int vn = 0; vn < nVerts; vn++)
                        sb.AppendFormatEx("vn {0:F4} {1:F4} {2:F4}", normals[vn].X, normals[vn].Y, normals[vn].Z).AppendLine();

                    sb.AppendLine();

                    for (int vt = 0; vt < nVerts; vt++)
                        sb.AppendFormatEx("vt {0:F4} {1:F4} 0.0000", coords[vt].X, coords[vt].Y).AppendLine();

                    sb.AppendLine();

                    sb.AppendFormatEx("g {0}_{1:D2}_{2}", uid, modelIdx, lodType).AppendLine();
                    sb.AppendLine("s 1");

                    sb.Append(faces.ToString());

                    //for (int t = 0; t < nTris; t += 3)
                    //{
                    //    sb.AppendFormat2("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}",
                    //        indices[t] + 1 + startIndex,
                    //        indices[t + 1] + 1 + startIndex,
                    //        indices[t + 2] + 1 + startIndex).AppendLine();
                    //}

                    sb.AppendLine();

                    startIndex += nVerts;

                    //-- This splits meshes up by texture
                    //-- Leaving this here if ever needed
                    //for (int m = 0; m < group.Meshes.Count; m++)
                    //{
                    //    IndexedPrimitive prim = group.Meshes[m];
                    //
                    //    DriverModel3D model = new DriverModel3D(modelPackage, prim);
                    //
                    //    int nVerts = model.Positions.Count;
                    //    int nTris = model.TriangleIndices.Count;
                    //
                    //    //sb.AppendLine("#");
                    //    //sb.AppendFormat2("# Mesh {0}/{1}", m + 1, group.Meshes.Count).AppendLine();
                    //    //sb.AppendLine("#");
                    //
                    //    //sb.AppendLine();
                    //
                    //    for (int vx = 0; vx < nVerts; vx++)
                    //        sb.AppendFormat2("v {0:F4} {1:F4} {2:F4}", model.Positions[vx].X, model.Positions[vx].Y, model.Positions[vx].Z).AppendLine();
                    //
                    //    sb.AppendLine();
                    //
                    //    for (int vn = 0; vn < nVerts; vn++)
                    //        sb.AppendFormat2("vn {0:F4} {1:F4} {2:F4}", model.Normals[vn].X, model.Normals[vn].Y, model.Normals[vn].Z).AppendLine();
                    //
                    //    sb.AppendLine();
                    //
                    //    for (int vt = 0; vt < nVerts; vt++)
                    //        sb.AppendFormat2("vt {0:F4} {1:F4} 0.0000", model.TextureCoordinates[vt].X, model.TextureCoordinates[vt].Y).AppendLine();
                    //
                    //    sb.AppendLine();
                    //
                    //    sb.AppendFormat2("g Mesh_{0}_{1}_{2}_{3}", modelIdx, g, m, lodType).AppendLine();
                    //    sb.AppendLine("s 1");
                    //
                    //    for (int t = 0; t < nTris; t += 3)
                    //    {
                    //        sb.AppendFormat2("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}",
                    //            model.TriangleIndices[t] + 1 + startIndex,
                    //            model.TriangleIndices[t + 1] + 1 + startIndex,
                    //            model.TriangleIndices[t + 2] + 1 + startIndex).AppendLine();
                    //    }
                    //
                    //    sb.AppendLine();
                    //
                    //    startIndex += nVerts;
                    //}
                }
            }

            string filePath = String.Format(@"{0}\{1}.obj", path, filename);

            if (sbMtl != null)
            {
                string mtlFilePath = String.Format(@"{0}\{1}.mtl", path, filename);

                using (StreamWriter f = new StreamWriter(mtlFilePath, false, Encoding.Default, sbMtl.Length))
                    f.Write(sbMtl.ToString());
            }

            using (StreamWriter f = new StreamWriter(filePath, false, Encoding.Default, sb.Length))
                f.Write(sb.ToString());

            return ExportResult.Success;
        }
    }
}
