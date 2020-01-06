namespace DSCript.Models
{
    public enum VertexUsageType : byte
    {
        Unused = 0,

        Position,
        Normal,
        TextureCoordinate,

        BlendWeight,
        Tangent,

        Color,

        BiNormal,
        BlendIndices,
    }
}
