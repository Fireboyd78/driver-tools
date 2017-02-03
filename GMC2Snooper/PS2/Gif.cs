using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GMC2Snooper.PS2
{
    public enum GifPrimitive : int
    {
        Point			    = 0x00,	// Point primitive
        Line			    = 0x01,	// Line primitive
        LineStrip		    = 0x02,	// Line strip primitive
        Triangle		    = 0x03,	// Triangle primitive
        TriangleStrip	    = 0x04,	// Triangle strip primitive
        TriangleFan	        = 0x05,	// Triangle fan primitive
        Sprite			    = 0x06,	// Sprite primitive
    }

    public enum GifFormat : int
    {
        Packed	            = 0x00,	// Packed GIF packet
        RegList	            = 0x01,	// Reglist GIF packet
        Image	            = 0x02,	// Image GIF packet
        Disabled            = 0x03, // Disabled GIF packet (???)
    }

    public enum GifRegister : int
    {
        Prim			    = 0x00,	// Drawing primitive setting.
        RgbaQ			    = 0x01,	// Vertex color setting.
        St				    = 0x02,	// Specification of vertex texture coordinates.
        Uv				    = 0x03,	// Specification of vertex texture coordinates.
        XyzF2			    = 0x04,	// Setting for vertex coordinate values.
        Xyz2			    = 0x05,	// Setting for vertex coordinate values.
        Tex0			    = 0x06,	// Texture information setting.
        Tex0_1			    = 0x06,	// Texture information setting. (Context 1)
        Tex0_2			    = 0x07,	// Texture information setting. (Context 2)
        Clamp			    = 0x08,	// Texture wrap mode.
        Clamp_1			    = 0x08,	// Texture wrap mode. (Context 1)
        Clamp_2			    = 0x09,	// Texture wrap mode. (Context 2)
        Fog				    = 0x0A,	// Vertex fog value setting.
        XyzF3			    = 0x0C,	// Setting for vertex coordinate values. (Without Drawing Kick)
        Xyz3			    = 0x0D,	// Setting for vertex coordinate values. (Without Drawing Kick)
        Ad				    = 0x0E,	// GIFtag Address+Data
        Nop				    = 0x0F,	// GIFtag No Operation
    }
}
