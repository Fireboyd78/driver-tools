using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DSCript.Models
{
    public struct VertexDeclInfo
    {
        public static readonly VertexDeclInfo Empty = new VertexDeclInfo();

        public VertexDataType DataType;
        public VertexUsageType UsageType;
        public short UsageIndex;
        
        public override int GetHashCode()
        {
            return DataType.GetHashCode() * 131
                ^ UsageType.GetHashCode() * 223
                ^ UsageIndex.GetHashCode() * 95;
        }

        public bool Equals(VertexDeclInfo decl)
        {
            return decl.DataType == DataType
                && decl.UsageType == UsageType
                && decl.UsageIndex == UsageIndex;
        }

        public override bool Equals(object obj)
        {
            if (obj is VertexDeclInfo)
                return Equals((VertexDeclInfo)obj);

            return false;
        }

        public int SizeOf
        {
            get { return GetDataSize(DataType); }
        }

        public Type TypeOf
        {
            get { return GetDataType(DataType); }
        }
        
        public static int GetDataSize(VertexDataType type)
        {
            switch (type)
            {
            case VertexDataType.None:       return 0;

            case VertexDataType.Float:
            case VertexDataType.Color:      return 4;

            case VertexDataType.Vector2:    return 8;
            case VertexDataType.Vector3:    return 12;
            case VertexDataType.Vector4:    return 16;
            }

            throw new InvalidEnumArgumentException("Invalid vertex data type.", (int)type, typeof(VertexUsageType));
        }

        public static Type GetDataType(VertexDataType type)
        {
            switch (type)
            {
            case VertexDataType.None:       return null;
            case VertexDataType.Float:      return typeof(float);
            case VertexDataType.Color:      return typeof(ColorRGBA);
            case VertexDataType.Vector2:    return typeof(Vector2);
            case VertexDataType.Vector3:    return typeof(Vector3);
            case VertexDataType.Vector4:    return typeof(Vector4);
            }

            throw new InvalidEnumArgumentException("Invalid vertex data type.", (int)type, typeof(VertexUsageType));
        }
        
        public VertexDeclInfo(VertexDataType dataType, VertexUsageType usageType, short usageIndex = 0)
        {
            DataType = dataType;
            UsageType = usageType;
            UsageIndex = usageIndex;
        }
    }
}
