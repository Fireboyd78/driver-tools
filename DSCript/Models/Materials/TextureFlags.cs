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

    public enum TextureRenderMethods : uint
    {
        Disabled                    = 0,    // 0000 0000
        NoRender                    = 1,    // 0000 0001
        SelfCull1                   = 2,    // 0000 0010
        NormalCull2                 = 4,    // 0000 0100
        SelfCullWrongTexture        = 8,    // 0000 1000

        NormalCull1                 = 3,    // 0000 0011
        NormalCull3                 = 5,    // 0000 0101
        SelfCullWrongTextureZDepth1 = 9,    // 0000 1001

        NormalCull4                 = 6,    // 0000 0110
        SelfCullWrongTextureZDepth2 = 10,   // 0000 1010
        SelfCullZDepth1             = 12,   // 0000 1100

        SelfCull2                   = 7,    // 0000 0111
        SelfCullWrongTextureZDepth3 = 11,   // 0000 1011
        SelfCullZDepth2             = 13,   // 0000 1101
        SelfCullZDepth3             = 14,   // 0000 1110

        SelfCullZDepth4             = 15,   // 0000 1111

        //==============================================

        SelfCullZDepth5             = 16,   // 0001 0000
        SelfCullZDepth6             = 17,   // 0001 0001
        SelfCullZDepth7             = 18,   // 0001 0010
        SelfCullDisplacementMap     = 20,   // 0001 0100
        NormalCull6                 = 24,   // 0001 1000

        SelfCullZDepth8             = 19,   // 0001 0011
        SelfReverseCull             = 21,   // 0001 0101
        NormalCull7                 = 25,   // 0001 1001

        ReverseCull                 = 22,   // 0001 0110
        SelfCull3                   = 26,   // 0001 1010
        SelfCullZDepth9             = 28,   // 0001 1100

        NormalCull5                 = 23,   // 0001 0111
        ReverseCullEmissive         = 27,   // 0001 1011
        SelfCullZDepth10            = 29,   // 0001 1101
        Emissive                    = 30,   // 0001 1110
        
        SelfCullZDepth11            = 31    // 0001 1111
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
