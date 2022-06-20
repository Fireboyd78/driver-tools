using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;

namespace DSCript.Models
{
    public class Vertex : ICopyCat<Vertex>
    {
        bool ICopyCat<Vertex>.CanCopy(CopyClassType copyType)               => true;
        bool ICopyCat<Vertex>.CanCopyTo(Vertex obj, CopyClassType copyType) => true;

        bool ICopyCat<Vertex>.IsCopyOf(Vertex obj, CopyClassType copyType)
        {
            // TODO: this could be very useful to implement
            throw new System.NotImplementedException();
        }

        Vertex ICopyClass<Vertex>.Copy(CopyClassType copyType)
        {
            return new Vertex()
            {
                Position = Position,
                Normal = Normal,
                UV = UV,

                Color = Color,

                BlendWeight = BlendWeight,

                Tangent = Tangent,
                TangentVector = TangentVector,

                PositionW = PositionW,
                NormalW = NormalW,
            };
        }

        void ICopyClassTo<Vertex>.CopyTo(Vertex obj, CopyClassType copyType)
        {
            obj.Position = Position;
            obj.Normal = Normal;
            obj.UV = UV;

            obj.Color = Color;

            obj.BlendWeight = BlendWeight;

            obj.Tangent = Tangent;
            obj.TangentVector = TangentVector;

            obj.PositionW = PositionW;
            obj.NormalW = NormalW;
        }

        /// <summary>Gets or sets the position of the vertex.</summary>
        public Vector3 Position;

        /// <summary>Gets or sets the normals of the vertex.</summary>
        public Vector3 Normal;

        /// <summary>Gets or sets the UV mapping of the vertex.</summary>
        public Vector2 UV;

        /// <summary>Gets or sets the RGBA diffuse color of the vertex.</summary>
        public ColorRGBA Color;

        /// <summary>Gets or sets the blending weights of the vertex.</summary>
        public Vector4 BlendWeight;

        /// <summary>Gets or sets the tangent of the vertex.</summary>
        public float Tangent;

        public Vector4 TangentVector;

        public Vector3 PositionW;
        public Vector3 NormalW;

        public void ApplyScale(Vector3 scale, bool upscale = false)
        {
            if (upscale)
            {
                Position /= scale;
                PositionW /= scale;
            }
            else
            {
                Position *= scale;
                PositionW *= scale;
            }
        }

        public void FixDirection()
        {
            Position = new Vector3(-Position.X, Position.Z, Position.Y);
            Normal = new Vector3(-Normal.X, Normal.Z, Normal.Y);

            PositionW = new Vector3(-PositionW.X, PositionW.Z, PositionW.Y);
            NormalW = new Vector3(-NormalW.X, NormalW.Z, NormalW.Y);
        }

        public void Reset()
        {
            Position = new Vector3();
            Normal = new Vector3();
            UV = new Vector2();
            Color = new ColorRGBA(0, 0, 0, 255);

            BlendWeight = new Vector4();

            Tangent = 1.0f;
        }
    }
}
