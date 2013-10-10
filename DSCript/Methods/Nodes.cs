using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

using DSCript;
using DSCript.IO;
using DSCript.Object;

namespace DSCript.Methods
{
    public static class Nodes
    {
        private static int __i = 1; // HACK: num of chunks parsed to save performance!!

        public static TreeViewItem GenerateTreeNode(SubChunkBlock chunk)
        {
            return new TreeViewItem {
                Header = Chunks.Magic2Str(chunk.Magic),
                Tag = new NodeTag(chunk)
            };
        }

        public static void RecurseHierarchy(ChunkReader ChunkFile, int s, int n, TreeViewItem theNode)
        {
            for (int ss = __i; ss < ChunkFile.Chunk.Count; ss++)
            {
                //DSC.Log("{0} :: {1}", ss, __i);
                if (ChunkFile.Chunk[ss].Parent.Parent.ID == s && ChunkFile.Chunk[ss].Parent.ID == n)
                {
                    for (int nn = 0; nn < ChunkFile.Chunk[ss].Subs.Count; nn++)
                    {
                        TreeViewItem nd = GenerateTreeNode(ChunkFile.Chunk[ss].Subs[nn]);

                        theNode.Items.Add(nd);

                        RecurseHierarchy(ChunkFile, ss, nn, nd);
                    }
                    ++__i;
                    break;
                }
            }
        }

        public static void CreateTreeView(ChunkReader ChunkFile, TreeView tv)
        {
            if (tv.HasItems)
            {
                tv.Items.Clear();
                tv.UpdateLayout();

                DSC.Log("Cleared nodes.");
            }

            TreeViewItem fNode =
                new TreeViewItem 
                {
                    Header = Path.GetFileName(ChunkFile.Filename)
                };
            tv.Items.Add(fNode);

            DSC.Log("Adding nodes...");

            for (int n = 0; n < ChunkFile.Chunk[0].SubCount; n++)
            {
                TreeViewItem nd = GenerateTreeNode(ChunkFile.Chunk[0].Subs[n]);

                fNode.Items.Add(nd);

                RecurseHierarchy(ChunkFile, 0, n, nd);

                NodeTag bd = (NodeTag)nd.Tag;
            }

            DSC.Log("Finished adding {0} nodes!", __i);

            tv.UpdateLayout();

            //-- This is terribly slow for bigger files
            //-- But it expands the tree after loading everything
            //--  * Maybe a check can be added for lots of nodes?
            //fNode.ExpandSubtree();

            __i = 1; // reset chunks read
        }
    }
}
