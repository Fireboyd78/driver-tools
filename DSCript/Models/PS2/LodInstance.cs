namespace DSCript.Models
{
    public class LodInstancePS2
    {
        public TransformAxis Rotation { get; set; }
        public Vector4 Translation { get; set; }

        public bool HasRotation { get; set; }
        public bool HasTranslation { get; set; }

        public SubModelPS2 Model { get; set; }
    }
}
