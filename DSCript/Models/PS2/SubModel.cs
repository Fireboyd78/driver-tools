namespace DSCript.Models
{
    public class SubModelPS2
    {
        public bool HasBoundBox { get; set; }
        
        public Vector3 BoxOffset { get; set; }
        public Vector3 BoxScale { get; set; }

        // TODO: Link to actual texture data
        public int TextureId { get; set; }
        public int TextureSource { get; set; }

        public int Type { get; set; }
        public int Flags { get; set; }

        public int Unknown1 { get; set; }
        public int Unknown2 { get; set; }

        // TODO: Read actual model data!
        public byte[] DataBuffer { get; set; }
    }
}
