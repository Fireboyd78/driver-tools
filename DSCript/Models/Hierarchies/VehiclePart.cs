using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DSCript.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0x20)]
    public struct VehiclePartData
    {
        public short PartType;
        public short SlotType;

        public int Flags;
        public int TypeFlags; // characteristics of the part (brakelight, bumper, etc) -- seems kinda hacky

        public short NumChildren;

        public short Unknown; // possibly a parent to rotate around?
        public short Hinge;

        public byte ModelId;

        public short PhysicsId;
        public short PositionId;

        public short OffsetId;
        public short TransformId;

        public short AxisId;
        public short Reserved; // ;)
    }

    public class VehiclePart
    {
        public VehiclePartType PartType { get; set; }
        public VehiclePartSlotType SlotType { get; set; }

        public int Flags { get; set; }

        public int TypeFlags { get; set; }

        public List<VehiclePart> Children { get; set; }

        public int Unknown { get; set; }
        public int Hinge { get; set; }

        public Model ModelPart { get; set; }

        public Vector4 MarkerPoint { get; set; }

        public VehicleHierarchyData.CenterPoint CenterPoint { get; set; }
        public VehicleHierarchyData.Thing3 Thing3 { get; set; }

        public VehicleHierarchyData.PDLEntry CollisionModel { get; set; }
    }
}
