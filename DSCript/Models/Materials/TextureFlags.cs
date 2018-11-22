using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSCript.Models
{
    public enum TextureTypeFlags : uint
    {
        RenderMethod        = 0x1F,
        DoubleSided         = 0x400,
        AlphaTest           = 0x10000,
        ReverseCull         = 0x8000
    }
    
    public enum TextureRenderFlags : uint
    {
        SpecularNoiseMap    = 0x102,
        SpecularMap         = 0x201,
    }

    public enum TextureStateFlags : uint
    {
        NormalMap           = 0x2,
        ColorMask           = 0x4,
        Damage              = 0x8,
        ColorMaskDamage     = 0x10,
    }
}
