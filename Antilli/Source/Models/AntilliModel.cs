using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using DSCript;
using DSCript.Models;

using Antilli.Parser;

namespace Antilli
{
    public enum AntilliModelType
    {
        Vehicle,
        /*
            TODO: Implement more types
        */
    }
    
    public class AntilliModel
    {
        static readonly string[] sLodNames = {
            "high",
            "med",
            "low",
            "vlow",
            "shadow",
        };

        static readonly int[] sLodSlots = {
            0,
            1,
            2,
            4,
            5,
        };

        static int GetLodSlotIndex(int type)
        {
            switch (type)
            {
            case 0x1E:  return 0;
            case 0x1C:  return 1;
            case 0x18:  return 2;
            case 1:     return 3;
            case 0:     return 4;
            }

            return -1;
        }
        
        public class Model
        {
            public UID UID;

            public Vector4 V1;

            public Matrix Transform1;
            public Matrix Transform2;

            public List<LodInstance> Lods;
        }

        public class LodInstance
        {
            public int Type;

            public List<SubModel> SubModels;
        }

        public class SubModel
        {
            public string Name;

            public Matrix Transform;

            public List<Mesh> Meshes;
        }

        public class Mesh
        {
            public Material Material;

            public List<Vertex> Vertices;
            public List<int> Indices;
        }

        public class Material
        {
            public string Name;

            public bool IsGlobal;
            public bool IsAnimation;

            public float AnimationSpeed;

            public List<Substance> Substances;

            public static Material Create(string name, MaterialDataPC mtl)
            {
                var material = new Material() {
                    Name = name,

                    IsAnimation = (mtl.Type == MaterialType.Animated),
                    AnimationSpeed = mtl.AnimationSpeed,
                };
                
                var substances = new List<Substance>();

                foreach (var s in mtl.Substances)
                {
                    var substance = new Substance() {
                        Bin = s.Bin,
                        Flags = s.Flags,
                        K1 = (s.Mode & 0xFF),
                        K2 = (s.Mode >> 8),
                        K3 = (s.Type & 0xFF),
                        ExtraFlags = (s.Type >> 8),
                    };

                    substances.Add(substance);

                    var textures = new List<String>();

                    foreach (var t in s.Textures)
                        textures.Add($"{t.Hash:X8}.dds");

                    substance.Textures = textures;
                }

                material.Substances = substances;

                return material;
            }
        }

        public class Substance
        {
            public int Bin;
            public int Flags;

            public int K1;
            public int K2;
            public int K3;

            public int ExtraFlags;

            public List<String> Textures;
        }
        
        public float Version;

        public int UID;

        public AntilliModelType Type;
        public int Flags;

        public VertexDeclaration VertexDecl;

        public List<Model> Models;
        public List<Material> Materials;

        public static AntilliModel Create(ModelPackage modelPackage)
        {
            var modelFile = modelPackage.ModelFile as Driv3rVehiclesFile;

            // not yet
            if (modelFile == null)
                return null;
            
            var m = new AntilliModel() {
                Version     = 1.05f,

                // HACK: FORCE VEHICLE!
                UID         = 0,
                Type        = AntilliModelType.Vehicle,
                Flags       = 0,
            };

            var mIdx = 0;
            
            var materials = new List<Material>();
            
            foreach (var mtl in modelPackage.Materials)
            {
                var material = Material.Create($"mtl_{mIdx++}", mtl);

                materials.Add(material);
            }
            
            var models = new List<Model>();
            
            foreach (var mdl in modelPackage.Models)
            {
                var t11 = mdl.Transform[0];
                var t12 = mdl.Transform[1];
                var t13 = mdl.Transform[2];
                var t14 = mdl.Transform[3];

                var t21 = mdl.Transform[4];
                var t22 = mdl.Transform[5];
                var t23 = mdl.Transform[6];
                var t24 = mdl.Transform[7];

                var model = new Model() {
                    UID = mdl.UID,

                    V1 = mdl.Scale,

                    Transform1 = new Matrix(
                        t11.X, t11.Y, t11.Z, t11.W,
                        t12.X, t12.Y, t12.Z, t12.W,
                        t13.X, t13.Y, t13.Z, t13.W,
                        t14.X, t14.Y, t14.Z, t14.W
                    ),
                    Transform2 = new Matrix(
                        t21.X, t21.Y, t21.Z, t21.W,
                        t22.X, t22.Y, t22.Z, t22.W,
                        t23.X, t23.Y, t23.Z, t23.W,
                        t24.X, t24.Y, t24.Z, t24.W
                    ),
                };

                models.Add(model);

                var lods = new List<LodInstance>();

                foreach (var p in mdl.Lods)
                {
                    if (p == null)
                        continue;

                    var lod = new LodInstance() {
                        Type = GetLodSlotIndex(p.Type)
                    };

                    lods.Add(lod);

                    var subModels = new List<SubModel>();
                    
                    foreach (var g in p.Instances)
                    {
                        var t1 = g.Transform[0];
                        var t2 = g.Transform[1];
                        var t3 = g.Transform[2];
                        var t4 = g.Transform[3];

                        var subModel = new SubModel() {
                            Transform = new Matrix(
                                t1.X, t1.Y, t1.Z, t1.W,
                                t2.X, t2.Y, t2.Z, t2.W,
                                t3.X, t3.Y, t3.Z, t3.W,
                                t4.X, t4.Y, t4.Z, t4.W
                            )
                        };

                        subModels.Add(subModel);
                        
                        var meshes = new List<Mesh>();

                        foreach (var msh in g.SubModels)
                        {
                            Material material = null;

                            if ((msh.SourceUID != m.UID) && (msh.SourceUID != 0xFFFD))
                            {
                                var mtl = msh.GetMaterial();

                                material = Material.Create($"mtl_{mIdx++}", mtl);
                                material.IsGlobal = true;

                                materials.Add(material);
                            }

                            // don't adjust vertices
                            var indices = new List<int>();
                            var vertices = msh.GetVertices(false, ref indices);
                            
                            var mesh = new Mesh() {
                                Material = material,

                                Vertices = vertices,
                                Indices = indices,
                            };

                            meshes.Add(mesh);
                        }

                        subModel.Meshes = meshes;
                    }

                    lod.SubModels = subModels;
                }

                model.Lods = lods;
            }

            m.Models = models;
            m.Materials = materials;

            return m;
        }

        public void SaveXML(string filename)
        {
            var xml = new XDocument();

            var root = new XElement("ModelPackage");

            root.SetAttributeValue("Version", Version);
            root.SetAttributeValue("UID", UID);
            root.SetAttributeValue("Type", Type.ToString());

            var materials = new XElement("Materials");

            root.Add(materials);

            foreach (var m in Materials)
            {
                var material = new XElement((m.IsAnimation) ? "Animation" : "Group");
                
                if (m.IsAnimation)
                    material.SetAttributeValue("Speed", m.AnimationSpeed);

                materials.Add(material);

                foreach (var s in m.Substances)
                {
                    var substance = new XElement("Substance");

                    substance.SetAttributeValue("Bin", s.Bin);
                    substance.SetAttributeValue("Flags", s.Flags);
                    substance.SetAttributeValue("Registers", $"{s.K1},{s.K2},{s.K3}");
                    substance.SetAttributeValue("SlotFlags", s.ExtraFlags);

                    material.Add(substance);
                    
                    foreach (var t in s.Textures)
                    {
                        var texture = new XElement("Texture");
                        texture.SetAttributeValue("Map", t);

                        substance.Add(texture);
                    }
                }
            }
            
            root.Save(filename);
        }

        public void SaveASCII(string filename)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine($"# Antilli Model Format\n");

            sb.AppendLine($"version {Version:F2}\n");
            sb.AppendLine($"uid {UID}\n");

            sb.AppendLine($"type {Type.ToString()}");
            sb.AppendLine($"flags none\n");

            foreach (var m in Materials)
            {
                sb.Append($"material {m.Name}");

                if (m.IsGlobal)
                    sb.Append($":global");

                sb.AppendLine($" {{");
                
                if (m.IsAnimation)
                    sb.AppendLine($"\tanimation {m.AnimationSpeed:F2}");
                
                foreach (var s in m.Substances)
                {
                    sb.AppendLine($"\tsubstance {{");
                    sb.AppendLine($"\t\tbin {s.Bin}");
                    sb.AppendLine($"\t\tflags {s.Flags}");
                    sb.AppendLine($"\t\tinfo {s.K1} {s.K2} {s.K3}");

                    sb.AppendLine($"\t\ttextures {{");

                    if (s.ExtraFlags != 0)
                        sb.AppendLine($"\t\t\tflags {s.ExtraFlags}");

                    foreach (var t in s.Textures)
                        sb.AppendLine($"\t\t\tmap \"{t}\"");

                    sb.AppendLine($"\t\t}}");
                    sb.AppendLine($"\t}}");
                }
                
                sb.AppendLine($"}}\n");
            }

            File.WriteAllText(filename, sb.ToString());
        }
    }
}
