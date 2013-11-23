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

using Antilli.Models;

namespace Antilli.IO
{
    /*-------------------------------------------------------------------
     * 
     * This exporter made possible by:
     * http://wiki.unity3d.com/index.php?title=ExportOBJ
     * 
     * ------------------------------------------------------------------*/
    public static class OBJExporter
    {
        const string highLod = "HIGH";
        const string medLod = "MED";
        const string lowLod = "LOW";
        const string vLowLod = "VLOW";
        const string shadow = "SHADOW";
        const string unknown1 = "UNKNOWN1";
        const string unknown2 = "UNKNOWN2";
        const string undefined = "UNDEFINED";

        public static void ExportOBJ(string filename, ModelsPackage modelPackage, PartsGroup partBasedOn)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat(
@"# ModelPackageToObj Exporter v0.2b by CarLuver69
# Date: {0}", DateTime.Now).AppendLines(2);

            var verts = modelPackage.Vertices.Buffer;

            int i = 0;

            int startIndex = 0;

            foreach (PartsGroup part in modelPackage.Parts)
            {
                int g = 0;

                MeshGroup group = part.Parts[g].Group;

                while (g < part.Parts.Count && part.Parts[g].Group == null)
                {
                    if (g + 1 > part.Parts.Count)
                        break;

                    group = part.Parts[g++].Group;
                }

                if (group != null && ((partBasedOn != null && part.UID == partBasedOn.UID) || partBasedOn == null))
                {
                    string groupIdentifier = undefined;

                    switch (g)
                    {
                    case 0:
                        groupIdentifier = highLod; break;
                    case 1:
                        groupIdentifier = medLod; break;
                    case 2:
                        groupIdentifier = lowLod; break;
                    case 3:
                        groupIdentifier = unknown1; break;
                    case 4:
                        groupIdentifier = vLowLod; break;
                    case 5:
                        groupIdentifier = shadow; break;
                    case 6:
                        groupIdentifier = unknown2; break;
                    }

                    foreach (IndexedPrimitive prim in group.Meshes)
                    {
                        DriverModel3D model = new DriverModel3D(modelPackage, prim);

                        int nVerts = 0;

                        for (int vx = 0; vx < model.Positions.Count; vx++)
                        {
                            sb.AppendFormat("v {0:F4} {1:F4} {2:F4}", model.Positions[vx].X, model.Positions[vx].Y, model.Positions[vx].Z).AppendLine();
                            nVerts++;
                        }

                        sb.AppendLine();

                        for (int vn = 0; vn < model.Normals.Count; vn++)
                            sb.AppendFormat("vn {0:F4} {1:F4} {2:F4}", model.Normals[vn].X, model.Normals[vn].Y, model.Normals[vn].Z).AppendLine();

                        sb.AppendLine();

                        for (int vt = 0; vt < model.TextureCoordinates.Count; vt++)
                            sb.AppendFormat("vt {0:F4} {1:F4} 0.0000", model.TextureCoordinates[vt].X, model.TextureCoordinates[vt].Y).AppendLine();

                        sb.AppendLine();

                        sb.AppendFormat("g Mesh{0}_{1}", groupIdentifier, (i++ + 1)).AppendLine();
                        sb.AppendLine("s 1");

                        for (int t = 0; t < model.TriangleIndices.Count; t += 3)
                        {
                            sb.AppendFormat("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}",
                                model.TriangleIndices[t] + 1 + startIndex,
                                model.TriangleIndices[t + 1] + 1 + startIndex,
                                model.TriangleIndices[t + 2] + 1 + startIndex).AppendLine();
                        }

                        startIndex += nVerts;

                        sb.AppendLines(2);
                    }
                }
            }

            using (StreamWriter f = new StreamWriter(filename))
            {
                f.Write(sb.ToString());
            }

            DSCript.DSC.Log("Successfully exported {0}!", filename);

            /*
            sb.AppendFormat("# TOTAL VERTICES: {0}", verts.Length).AppendLines(2);

            for (int v = 0; v < verts.Length; v++)
            {
                sb.AppendFormat("v {0:F4} {1:F4} {2:F4}",
                    verts[v].Positions.X,
                    verts[v].Positions.Y,
                    verts[v].Positions.Z).AppendLine();
            }

            sb.AppendLine();

            for (int vn = 0; vn < verts.Length; vn++)
            {
                sb.AppendFormat("vn {0:F4} {1:F4} {2:F4}",
                    verts[vn].Normals.X,
                    verts[vn].Normals.Y,
                    verts[vn].Normals.Z).AppendLine();
            }

            sb.AppendLine();

            for (int vt = 0; vt < verts.Length; vt++)
            {
                sb.AppendFormat("vt {0:F4} {1:F4} 0.0000",
                    verts[vt].UVs.X,
                    -verts[vt].UVs.Y).AppendLine();
            }

            sb.AppendLine();

            var partsGroups = modelPackage.Parts;

            for (int i = 0; i < partsGroups.Count; i++)
            {
                var parts = partsGroups[i].Parts;

                sb.AppendFormat(
@"############
# Model {0}
############", i).AppendLines(2);

                for (int g = 0; g < parts.Count; g++)
                {
                    var group = parts[g].Group;

                    string groupIdentifier = undefined;

                    switch (g)
                    {
                    case 0:
                        groupIdentifier = highLod; break;
                    case 1:
                        groupIdentifier = medLod; break;
                    case 2:
                        groupIdentifier = lowLod; break;
                    case 3:
                        groupIdentifier = unknown1; break;
                    case 4:
                        groupIdentifier = vLowLod; break;
                    case 5:
                        groupIdentifier = shadow; break;
                    case 6:
                        groupIdentifier = unknown2; break;
                    }

                    if (group != null)
                    {
                        sb.AppendFormat("g Mesh{0}_{1}", groupIdentifier, i).AppendLine();
                        sb.AppendLine("s 1");

                        for (int m = 0; m < group.Meshes.Count; m++)
                        {
                            var meshInfo = group.Meshes[m];

                            Mesh mesh = Mesh.Create(modelPackage, meshInfo, false);

                            //sb.AppendFormat("# Parts Group: {0}", g).AppendLine();
                            //sb.AppendFormat("# Mesh: {0}", m).AppendLine();
                            //sb.AppendFormat("# Defined Vertices/Faces: {0}/{1}", meshInfo.NumVertices, meshInfo.PrimitiveCount).AppendLine();
                            //sb.AppendFormat("# Exported Vertices/Faces: {0}/{1}", mesh.Vertices.Count, mesh.Faces.Count).AppendLines(2);

                            for (int t = 0; t < mesh.Faces.Count; t++)
                            {
                                int i0, i1, i2;

                                i0 = mesh.Faces[t].P1 + 1 + meshInfo.BaseVertexIndex;
                                i1 = mesh.Faces[t].P2 + 1 + meshInfo.BaseVertexIndex;
                                i2 = mesh.Faces[t].P3 + 1 + meshInfo.BaseVertexIndex;

                                sb.AppendFormat("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}", i0, i1, i2).AppendLine();
                            }    
                        }

                        sb.AppendLines(2);
                    }
                }
            }

            using (StreamWriter f = new StreamWriter(filename))
            {
                f.Write(sb.ToString());
            }

            DSCript.DSC.Log("Successfully exported {0}!", filename);*/
        }


        public static void ExportOBJ_Old(string filename, ModelPackage model)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(String.Format(
@"# ModelPackageToObj Exporter v0.1b by CarLuver69
# Date: {0}", System.DateTime.Now)).Append("\r\n\r\n");

            int startIdx = 0;

            for (int m = 0; m < model.Meshes.Count; m++)
            {
                if (model.Meshes[m].Data.Parent.Parent.ID == 0)
                {
                    sb.Append(String.Format(
@"#
# Mesh {0}
#", m)).Append("\r\n\r\n");


                    int numVertices = 0;

                    for (int v = 0; v < model.Meshes[m].Vertices.Count; v++)
                    {
                        sb.Append(String.Format("v {0:F4} {1:F4} {2:F4}",
                            -model.Meshes[m].Vertices[v].Position.X,
                            model.Meshes[m].Vertices[v].Position.Y,
                            model.Meshes[m].Vertices[v].Position.Z)).Append("\r\n");
                        ++numVertices;
                    }

                    sb.Append(String.Format("# {0} vertices", model.Meshes[m].Vertices.Count)).Append("\r\n\r\n");

                    for (int vn = 0; vn < model.Meshes[m].Vertices.Count; vn++)
                    {
                        sb.Append(String.Format("vn {0:F4} {1:F4} {2:F4}",
                            -model.Meshes[m].Vertices[vn].Normals.X,
                            model.Meshes[m].Vertices[vn].Normals.Y,
                            model.Meshes[m].Vertices[vn].Normals.Z)).Append("\r\n");
                    }

                    sb.Append(String.Format("# {0} vertex normals", model.Meshes[m].Vertices.Count)).Append("\r\n\r\n");

                    for (int vt = 0; vt < model.Meshes[m].Vertices.Count; vt++)
                    {
                        sb.Append(String.Format("vt {0:F4} {1:F4}",
                            model.Meshes[m].Vertices[vt].UVMap.U,
                            -model.Meshes[m].Vertices[vt].UVMap.V)).Append("\r\n");
                    }

                    sb.Append(String.Format("# {0} texture coords", model.Meshes[m].Vertices.Count)).Append("\r\n\r\n");

                    sb.Append(String.Format("g Mesh{0}", m)).Append("\r\n");
                    sb.Append("s 1").Append("\r\n");

                    for (int t = 0; t < model.Meshes[m].Faces.Count; t++)
                    {
                        sb.Append(String.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}",
                            model.Meshes[m].Faces[t].Point1 + 1 + startIdx,
                            model.Meshes[m].Faces[t].Point2 + 1 + startIdx,
                            model.Meshes[m].Faces[t].Point3 + 1 + startIdx)).Append("\r\n");
                    }

                    sb.Append(String.Format("# {0} faces", model.Meshes[m].Faces.Count)).Append("\r\n\r\n");

                    startIdx += numVertices;
                }
            }

            using (StreamWriter f = new StreamWriter(filename))
            {
                f.Write(sb.ToString());
            }
        }
    }
}
