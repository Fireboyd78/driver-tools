using System.IO;
using System.Linq;
using System.Text;

namespace DSCript.Models
{
    // defines directional transforms
    // e.g. { 0,0,1,0 } for Y means Z is up
    public struct TransformAxis
    {
        public Vector4 X;
        public Vector4 Y;
        public Vector4 Z;

        public TransformAxis(Stream stream)
        {
            X = stream.Read<Vector4>();
            Y = stream.Read<Vector4>();
            Z = stream.Read<Vector4>();
        }
    }
}
