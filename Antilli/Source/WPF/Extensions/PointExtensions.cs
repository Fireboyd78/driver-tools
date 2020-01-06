using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Windows.Media.Media3D
{
    public static class PointExtensions
    {
        public static Point3D DecodeN(this Point enc)
        {
            var frc = new Func<double, double>((src) => {
                return src - (Math.Floor(src));
            });

            var radToDeg = new Func<double, double>((angle) => {
                return angle * (180.0 / Math.PI);
            });

            var cosTheta = new Point(0, 0);
            var cosPhi = new Point(0, 0);

            enc.X = frc(enc.X);
            enc.Y = frc(enc.Y);

            enc.X = ((enc.X * 2) - 1) * Math.PI;
            enc.Y = ((enc.Y * 2) - 1) * Math.PI;

            enc.X = radToDeg(enc.X);
            enc.Y = radToDeg(enc.Y);

            cosTheta.X = Math.Cos(enc.Y);
            cosTheta.Y = Math.Sin(enc.Y);

            cosPhi.X = Math.Cos(enc.X);
            cosPhi.Y = Math.Sin(enc.X);

            return new Point3D() {
                X = (cosTheta.Y * cosPhi.Y),  // X
                Y = -(cosTheta.X * cosPhi.Y), // Z
                Z = cosPhi.X                  // Y
            };
        }
    }
}
