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
using DSCript.Parser;

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
        

        
        
        public class Model
        {
            public UID UID;

            public Vector4 Scale;

            public BBox BoundingBox;

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

            public Matrix44 Transform;

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
                        K1 = s.TS1,
                        K2 = s.TS2,
                        K3 = s.TS3,
                        ExtraFlags = s.TextureFlags,
                    };

                    substances.Add(substance);

                    var textures = new List<String>();

                    foreach (var t in s.Textures)
                        textures.Add($"{t.Handle:X8}.dds");

                    substance.Textures = textures;
                }

                material.Substances = substances;

                return material;
            }
        }

        public class Substance
        {
            public RenderBinType Bin;

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
                var model = new Model() {
                    UID = mdl.UID,

                    Scale = mdl.Scale,

                    BoundingBox = mdl.BoundingBox,
                };

                models.Add(model);

                var lods = new List<LodInstance>();

                foreach (var p in mdl.Lods)
                {
                    if (p == null)
                        continue;

                    var lod = new LodInstance() {
                        Type = Lod.GuessLodTypeByMask(p.Mask)
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
                            Transform = new Matrix44(
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
                            
                            if ((msh.Material.Handle != m.UID) && (msh.Material.UID != 0xFFFD))
                            {
                                MaterialDataPC mtl = null;

                                if (MaterialManager.Find(msh.Material, out mtl) != 0)
                                {
                                    material = Material.Create($"mtl_{mIdx++}", mtl);
                                    material.IsGlobal = true;

                                    materials.Add(material);
                                }
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
    }
}
