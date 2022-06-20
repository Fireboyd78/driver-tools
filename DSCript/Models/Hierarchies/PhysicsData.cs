using System;
using System.Collections.Generic;
using System.IO;

namespace DSCript.Models
{
    public class PhysicsData : IDetail
    {
        public List<PhysicsCollisionModel> CollisionModels { get; set; }
        
        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            var pdlOffset = (int)stream.Position;
            var pdl = provider.Deserialize<PhysicsInfo>(stream);

            CollisionModels = new List<PhysicsCollisionModel>(pdl.CollisionModelsCount);

            var primLookup = new Dictionary<int, int>();
            var primitives = new List<PhysicsPrimitive>();

            //
            // Physics primitives
            //

            stream.Position = pdlOffset + pdl.PrimitivesOffset;

            for (int i = 0; i < pdl.PrimitivesCount; i++)
            {
                var ptr = (int)stream.Position;
                var data = provider.Deserialize<PhysicsPrimitive>(stream);

                primLookup.Add(ptr, i);
                primitives.Add(data);
            }

            //
            // Physics collision models
            //

            stream.Position = pdlOffset + pdl.CollisionModelsOffset;

            for (int i = 0; i < pdl.CollisionModelsCount; i++)
            {
                var _cm = provider.Deserialize<PhysicsCollisionModel.Detail>(stream);

                List<PhysicsPrimitive> children = null;

                if (_cm.Count != 0)
                {
                    var childrenIdx = -1;

                    if (primLookup.TryGetValue(_cm.Offset, out childrenIdx))
                        children = primitives.GetRange(childrenIdx, _cm.Count);

                    if (children == null)
                        throw new InvalidOperationException("Failed to get physics data children!");
                }

                var entry = new PhysicsCollisionModel() {
                    BoundingRadius = _cm.BoundingRadius,
                    Flags = _cm.Flags,

                    Children = children,
                };

                CollisionModels.Add(entry);
            }
        }

        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            var pdlOffset = (int)stream.Position;
            
            var collisionModelsOffset = 0x20;
            var collisionModelsCount = CollisionModels.Count;

            var primitiveListOffset = collisionModelsOffset + (CollisionModels.Count * 0x10);
            var primitiveOffset = pdlOffset + primitiveListOffset;
            var primitiveCount = 0;

            var luPrimitives = new Dictionary<PhysicsPrimitive, int>();

            stream.Position = pdlOffset + collisionModelsOffset;

            // write out collision models
            foreach (var collisionModel in CollisionModels)
            {
                var ptr = (int)stream.Position;

                var cm = new PhysicsCollisionModel.Detail()
                {
                    Count = collisionModel.Children.Count,
                    Offset = -1,
                    BoundingRadius = collisionModel.BoundingRadius,
                    Flags = collisionModel.Flags,
                };

                foreach (var primitive in collisionModel.Children)
                {
                    if (!luPrimitives.ContainsKey(primitive))
                    {
                        luPrimitives.Add(primitive, primitiveOffset);

                        // write out primitive
                        stream.Position = primitiveOffset;
                        {
                            provider.Serialize(stream, primitive);

                            // update primitives offset/count
                            primitiveOffset = (int)stream.Position;
                            primitiveCount++;
                        }
                    }

                    if (cm.Offset == -1)
                        cm.Offset = luPrimitives[primitive];
                }

                // back to collision models list
                stream.Position = ptr;

                // write out the collision model info
                provider.Serialize(stream, cm);
            }

            var pdl = new PhysicsInfo()
            {
                CollisionModelsCount = collisionModelsCount,
                CollisionModelsOffset = collisionModelsOffset,

                PrimitivesCount = primitiveCount,
                PrimitivesOffset = primitiveListOffset,
            };

            // write out header
            stream.Position = pdlOffset;

            provider.Serialize(stream, pdl);

            // now go to the end (where next primitive would start)
            stream.Position = primitiveOffset;
        }
    }
}
