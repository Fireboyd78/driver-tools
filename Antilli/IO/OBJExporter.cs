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
using System.Windows.Media;
using System.Windows.Media.Media3D;

using DSCript;

using Antilli.Models;

namespace Antilli.IO
{
    /*-------------------------------------------------------------------
     * 
     * Some code borrowed from here:
     * http://wiki.unity3d.com/index.php?title=ExportOBJ
     * 
     * ------------------------------------------------------------------*/
    public static class OBJFile
    {
        static Dictionary<int, string> LODTypes = new Dictionary<int, string>(8) {
            { 0, "HIGH"      },
            { 1, "MEDIUM"    },
            { 2, "LOW"       },
            { 3, "VERYLOW"   },
            { 4, "HYPERLOW"  },
            { 5, "SHADOW"    },
            { 6, "UNKNOWN"   },
            {-1, "UNDEFINED" },
        };

        public static void Export(string exportFile, ModelsPackage modelPackage, long uid)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("# Driver Model .OBJ Exporter v0.3516b by CarLuver69");
            sb.AppendFormat("# Exported: {0}", DateTime.Now).AppendLines(2);

            int startIndex = 0;
            int modelIdx = 0;

            foreach (PartsGroup part in modelPackage.Parts)
            {
                if (uid != -1 && part.UID != uid)
                    continue;

                sb.AppendFormat2("# ============== Group {0} ==============", ++modelIdx).AppendLines(2);

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

                    int tIdx = 0;

                    for (int m = 0; m < group.Meshes.Count; m++)
                    {
                        IndexedPrimitive prim = group.Meshes[m];

                        DriverModel3D model = new DriverModel3D(modelPackage, prim);

                        int vCount = model.Positions.Count;
                        int tCount = model.TriangleIndices.Count;

                        for (int v = 0; v < vCount; v++)
                        {
                            vertices.Add(model.Positions[v]);
                            normals.Add(model.Normals[v]);
                            coords.Add(model.TextureCoordinates[v]);
                        }

                        for (int t = 0; t < tCount; t += 3)
                        {
                            indices.Add(model.TriangleIndices[t] + tIdx);
                            indices.Add(model.TriangleIndices[t + 1] + tIdx);
                            indices.Add(model.TriangleIndices[t + 2] + tIdx);
                        }

                        tIdx += vCount;
                    }

                    int nVerts = vertices.Count;
                    int nTris = indices.Count;

                    for (int vx = 0; vx < nVerts; vx++)
                        sb.AppendFormat2("v {0:F4} {1:F4} {2:F4}", vertices[vx].X, vertices[vx].Y, vertices[vx].Z).AppendLine();

                    sb.AppendLine();

                    for (int vn = 0; vn < nVerts; vn++)
                        sb.AppendFormat2("vn {0:F4} {1:F4} {2:F4}", normals[vn].X, normals[vn].Y, normals[vn].Z).AppendLine();

                    sb.AppendLine();

                    for (int vt = 0; vt < nVerts; vt++)
                        sb.AppendFormat2("vt {0:F4} {1:F4} 0.0000", coords[vt].X, coords[vt].Y).AppendLine();

                    sb.AppendLine();

                    sb.AppendFormat2("g Mesh{0}_{1}_{2}", modelIdx, g + 1, lodType).AppendLine();
                    sb.AppendLine("s 1");

                    for (int t = 0; t < nTris; t += 3)
                    {
                        sb.AppendFormat2("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}",
                            indices[t] + 1 + startIndex,
                            indices[t + 1] + 1 + startIndex,
                            indices[t + 2] + 1 + startIndex).AppendLine();
                    }

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

            using (StreamWriter f = new StreamWriter(exportFile))
                f.Write(sb.ToString());

            MessageBox.Show(String.Format("Successfully exported {0}!", exportFile), "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
