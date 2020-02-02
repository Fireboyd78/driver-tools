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
                    Zestiness = _cm.Zestiness,
                    Flags = _cm.Flags,

                    Children = children,
                };

                CollisionModels.Add(entry);
            }
        }

        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            
        }
    }
}
