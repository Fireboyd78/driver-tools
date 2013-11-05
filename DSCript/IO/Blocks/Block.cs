using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace DSCript.IO
{
    public class Block
    {
        protected const string blockInfo = "Block Information";

        [Category(blockInfo)]
        [PropertyOrder(1)]
        [Description("The ID representing this block")]
        public int ID { get; set; }

        [Category(blockInfo)]
        [PropertyOrder(2)]
        [Description("The parent that holds this block")]
        [ExpandableObject]
        public Block Parent { get; set; }

        [Category(blockInfo)]
        [PropertyOrder(4)]
        [DisplayName("Base Offset")]
        [Description("The base offset of the block within the file")]
        public uint BaseOffset { get; set; }

        [Category(blockInfo)]
        [PropertyOrder(5)]
        [Description("The size of the block within the file")]
        public uint Size { get; set; }

        public Block()
        {
            ID = 0;
            Parent = null;
        }
    }
}
