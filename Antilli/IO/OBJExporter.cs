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

namespace Antilli.IO
{
    public static class OBJExporter
    {
        public static void ExportOBJ(string filename, ModelPackage model)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(String.Format(
@"# ModelPackageToObj Exporter v0.1b by CarLuver69
# Date: {0}", System.DateTime.Now)).Append("\r\n\r\n");

            int startIdx = 0;

            for (int m = 0; m < model.Meshes.Count; m++)
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
                        model.Meshes[m].Faces[t].Point1+1+startIdx,
                        model.Meshes[m].Faces[t].Point2+1+startIdx,
                        model.Meshes[m].Faces[t].Point3+1+startIdx)).Append("\r\n");
                }

                sb.Append(String.Format("# {0} faces", model.Meshes[m].Faces.Count)).Append("\r\n\r\n");

                startIdx += numVertices;
            }

            using (StreamWriter f = new StreamWriter(filename))
            {
                f.Write(sb.ToString());
            }
        }
    }
}
