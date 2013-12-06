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
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

using HelixToolkit.Wpf;

namespace Antilli.Models
{
    public class DriverModel3D
    {
        static Color[] colors = {
                                Color.FromArgb(255, 255, 128, 128),
                                Color.FromArgb(255, 128, 255, 128),
                                Color.FromArgb(255, 128, 128, 255),
                                Color.FromArgb(255, 128, 32, 32),
                                Color.FromArgb(255, 32, 128, 32),
                                Color.FromArgb(255, 32, 32, 128),
                                Color.FromArgb(255, 128, 255, 32),
                                Color.FromArgb(255, 32, 128, 255),
                                Color.FromArgb(255, 255, 128, 32),
                                Color.FromArgb(255, 128, 128, 255),
                                Color.FromArgb(255, 128, 255, 255),
                                Color.FromArgb(255, 128, 255, 128),
                                Color.FromArgb(255, 32, 255, 255),
                                Color.FromArgb(255, 255, 32, 255),
                                Color.FromArgb(255, 255, 255, 255),
                                Color.FromArgb(255, 32, 32, 32),
                                Color.FromArgb(255, 31, 41, 76),
                                Color.FromArgb(255, 41, 76, 31),
                                Color.FromArgb(255, 76, 41, 31),
                                Color.FromArgb(255, 76, 31, 41),
                                Color.FromArgb(255, 76, 31, 76),
                                Color.FromArgb(255, 31, 76, 76),
                                Color.FromArgb(255, 31, 76, 31),
                            };

        public static bool UseBlendWeights { get; set; }

        static DriverModel3D()
        {
            UseBlendWeights = false;
        }

        public Point3DCollection BlendedPositions { get; set; }
        public Point3DCollection Positions { get; set; }
        public Vector3DCollection Normals { get; set; }
        public PointCollection TextureCoordinates { get; set; }
        public Int32Collection TriangleIndices { get; set; }

        public PCMPMaterial Material { get; set; }

        public IndexedPrimitive Primitive { get; set; }

        public static implicit operator GeometryModel3D(DriverModel3D model)
        {
            return model.ToGeometry();
        }

        public GeometryModel3D ToGeometry()
        {
            MeshGeometry3D mesh = new MeshGeometry3D() {
                Positions = (UseBlendWeights) ? BlendedPositions : Positions,
                Normals = Normals,
                TextureCoordinates = TextureCoordinates,
                TriangleIndices = TriangleIndices
            };

            for (int v = 0; v < TextureCoordinates.Count; v++)
            {
                Point vx = TextureCoordinates[v];

                vx.Y = -vx.Y;

                TextureCoordinates[v] = vx;
            }

            //-- Generate a random color
            //int colorIdx = new Random((int)DateTime.Now.ToBinary() * TriangleIndices.Count).Next(0, colors.Length);
            //Random random = new Random((int)DateTime.Now.ToBinary() / Positions.Count * (TriangleIndices.Count / 2));
            //
            //Color mixColor = Color.FromArgb(
            //        255,
            //        (byte)random.Next(random.Next(0, 254), 255),
            //        (byte)random.Next(random.Next(0, 254), 255),
            //        (byte)random.Next(random.Next(0, 254), 255)
            //    );
            //
            //SolidColorBrush matColor = new SolidColorBrush(Color.Add(colors[colorIdx], mixColor));

            SolidColorBrush matColor = new SolidColorBrush(Color.FromArgb(255, 180, 180, 180));
            //DiffuseMaterial material = new DiffuseMaterial(matColor);

            DiffuseMaterial material = null;
            MaterialGroup matGroup = new MaterialGroup();

            //DSCript.DSC.Log("Adding material {0}", Primitive.MaterialId + 1);

            bool hasTransparency = false;

            if (Material != null)
            {
                PCMPSubMaterial subMaterial = Material.SubMaterials[0];

                bool hasDamage = subMaterial.Textures.Count > 1;
                bool hasAlphaMask = subMaterial.Textures.Count > 2;

                hasTransparency = subMaterial.Unk1 == 1049 || (subMaterial.Unk1 == 1047 && subMaterial.Textures[0].Type == 1) || subMaterial.Unk1 == 1054 || subMaterial.Unk1 == 98334 || subMaterial.Unk1 == 99358;

                PCMPTextureInfo texInfo = (UseBlendWeights && hasDamage) ? (hasAlphaMask) ? subMaterial.Textures[2] : subMaterial.Textures[1] : subMaterial.Textures[0];

                BitmapSource bmap = null;

                if (!TextureCache.IsTextureCached(texInfo))
                {
                    bmap = (hasTransparency) ? texInfo.GetBitmapSource(true) : texInfo.GetBitmapSource(false);

                    //--This is temporarily hardcoded, a proper exporter will be coming soon.
                    //texInfo.ExportFile(String.Format(@"C:\Users\Tech\Desktop\TESTING\Export\{0}_{1}.dds", texInfo.CRC32, subMaterial.Unk1));

                    TextureCache.CacheTexture(texInfo, bmap);
                }
                else
                {
                    bmap = TextureCache.GetCachedTexture(texInfo);
                }

                if (hasTransparency && ((subMaterial.Unk1 & 0x1E) == 0x1E))
                {
                    EmissiveMaterial emis = new EmissiveMaterial() {
                        Brush = new ImageBrush() {
                            ImageSource = bmap,
                            TileMode = TileMode.Tile,
                            Stretch = Stretch.Fill,
                            ViewportUnits = BrushMappingMode.Absolute,
                        }
                    };

                    matGroup.Children.Add(emis);
                }

                material = new DiffuseMaterial() {
                    Brush = new ImageBrush() {
                        ImageSource = bmap,
                        TileMode = TileMode.Tile,
                        Stretch = Stretch.Fill,
                        ViewportUnits = BrushMappingMode.Absolute,
                    }
                };

                matGroup.Children.Add(material);
            }
            else
            {
                material = new DiffuseMaterial() {
                    Brush = new SolidColorBrush(Color.FromArgb(64, 255, 64, 128))
                };

                matGroup.Children.Add(material);
            }

            return new GeometryModel3D() {
                Geometry = mesh,
                Material = matGroup,
                BackMaterial = matGroup
            };
        }

        private void AddVertex(Vertex[] vertexBuffer, int vertexIndex)
        {
            Vertex vertex = vertexBuffer[vertexIndex];

            Positions.Add(vertex.Positions);
            Normals.Add(vertex.Normals);
            TextureCoordinates.Add(vertex.UVs);

            if (BlendedPositions != null)
                BlendedPositions.Add(Vertex.Tween(vertex.Positions, vertex.BlendWeights, 1.0));
        }

        public DriverModel3D(ModelPackage modelsPackage, IndexedPrimitive primitive)
        {
            Primitive = primitive;

            Vertex[] vertices = modelsPackage.Vertices.Buffer;
            ushort[] indices = modelsPackage.Indices.Buffer;

            int nVerts = vertices.Length;

            Positions = new Point3DCollection(nVerts);
            Normals = new Vector3DCollection(nVerts);
            TextureCoordinates = new PointCollection(nVerts);

            if (modelsPackage.Vertices.VertexType != FVFType.Vertex12)
                BlendedPositions = new Point3DCollection(nVerts);

            if (modelsPackage.Type == ModelPackageType.ModelPackagePC)
            {
                for (int v = 0; v <= primitive.NumVertices; v++)
                {
                    int vIdx = v + primitive.BaseVertexIndex + primitive.MinIndex;

                    if (vIdx == vertices.Length)
                        break;

                    AddVertex(vertices, vIdx);
                }
            }
            else
            {
                for (int v = 0; v < vertices.Length; v++)
                {
                    AddVertex(vertices, v);
                }
            }

            TriangleIndices = new Int32Collection();

            for (int i = 0; i < primitive.PrimitiveCount; i++)
            {
                int idx = primitive.StartIndex;
                int vIdx = primitive.BaseVertexIndex;

                int i0, i1, i2;

                if (primitive.PrimitiveType == D3DPRIMITIVETYPE.D3DPT_TRIANGLESTRIP)
                {
                    if (i % 2 == 1.0)
                    {
                        i0 = indices[idx + i];
                        i1 = indices[idx + (i + 1)];
                        i2 = indices[idx + (i + 2)];
                    }
                    else
                    {
                        i0 = indices[idx + (i + 2)];
                        i1 = indices[idx + (i + 1)];
                        i2 = indices[idx + i];
                    }

                    // When reading in the vertices, the YZ-axis was flipped
                    // Therefore i0 and i2 need to be flipped for proper face orientation
                    // This was AFTER learning the hard way...
                    if ((i0 != i1) && (i0 != i2) && (i1 != i2))
                    {
                        TriangleIndices.Add(i2 - primitive.MinIndex);
                        TriangleIndices.Add(i1 - primitive.MinIndex);
                        TriangleIndices.Add(i0 - primitive.MinIndex);
                    }
                }
                else if (primitive.PrimitiveType == D3DPRIMITIVETYPE.D3DPT_TRIANGLELIST)
                {
                    DSCript.DSC.Log("Loading a triangle list primitive!");

                    i0 = indices[idx + i];
                    i1 = indices[idx + (i + 1)];
                    i2 = indices[idx + (i + 2)];

                    TriangleIndices.Add(i2 - primitive.MinIndex);
                    TriangleIndices.Add(i1 - primitive.MinIndex);
                    TriangleIndices.Add(i0 - primitive.MinIndex);
                }
                else
                {
                    throw new Exception("Unknown primitive type!");
                }
            }
            

            // Add material

            if (primitive.TextureFlag != (uint)PackageType.VehicleGlobals)
            {
                if (modelsPackage.HasTextures && primitive.MaterialId < modelsPackage.MaterialData.Materials.Count)
                    Material = modelsPackage.MaterialData.Materials[primitive.MaterialId];
            }
            else
            {
                if (ModelPackage.HasGlobalTextures && primitive.MaterialId < ModelPackage.GlobalTextures.Count)
                    Material = ModelPackage.GlobalTextures[primitive.MaterialId];
            }
            //if (primitive.TextureFlag == 0xFFFD)
            //    DSCript.DSC.Log("Added reference to material {0}", primitive.MaterialId);
        }
    }
}
