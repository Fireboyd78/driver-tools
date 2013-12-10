using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Xceed.Wpf.Toolkit.PropertyGrid;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace DSCript.IO
{
    public sealed class SubChunkBlock : BlockOld
    {
        internal const string subChunkInfo = "Sub-Chunk Information";

        private uint _localOffset;

        [Category(subChunkInfo)]
        [PropertyOrder(10)]
        [Description("An enumeration from DSCript describing this type of sub-chunk")]
        public ChunkType Type
        {
            get
            {
                return (Enum.IsDefined(typeof(ChunkType), Magic)) ? (ChunkType)Magic : ChunkType.Unknown;
            }
        }

        [Category(BlockOld.blockInfo)]
        [PropertyOrder(3)]
        [DisplayName("Magic")]
        [Description("The sub-chunk type identifier")]
        public uint Magic { get; set; }

        [Category(subChunkInfo)]
        [PropertyOrder(11)]
        [Description("The local offset to this sub-chunk")]
        public uint Offset
        {
            get { return _localOffset; }
            set
            {
                _localOffset = value;
                BaseOffset = Parent.BaseOffset + _localOffset;
            }
        }

        [Category(subChunkInfo)]
        [PropertyOrder(12)]
        [Description("Unknown")]
        public byte Unk1 { get; set; }

        // this will appear UNDER 'Description' !!!
        [Category(subChunkInfo)]
        [PropertyOrder(16)]
        [DisplayName("Desc. Length")]
        [Description("The length of the description string (not read by the game)")]
        public byte StrLen { get; set; }

        [Category(subChunkInfo)]
        [PropertyOrder(13)]
        [Description("Unknown")]
        public byte Unk2 { get; set; }

        [Category(subChunkInfo)]
        [PropertyOrder(14)]
        [Description("Unknown")]
        public byte Unk3 { get; set; }

        [Category(subChunkInfo)]
        [PropertyOrder(15)]
        [Description("A short description for this sub-chunk leftover from development")]
        public string Description { get; set; }

        [Browsable(false)]
        public SubChunkBlock(int id, ChunkBlockOld parent)
        {
            ID = id;
            Parent = parent;
        }
    }
}
