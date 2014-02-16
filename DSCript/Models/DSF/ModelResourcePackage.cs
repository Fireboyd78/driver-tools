using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSCript.Models
{
    public class ModelResourcePackage
    {
        public ChunkFile ChunkFile { get; protected set; }

        public ChunkBlock ModelResourceChunk { get; protected set; }

        public List<ChunkBlock> ModelPackages
        {
            get
            {
                List<ChunkBlock> models = new List<ChunkBlock>();

                ChunkEntry mdxn = ModelResourceChunk.Entries[0];
                ChunkBlock mdxnChunk = ChunkFile.GetChunkFromEntry(mdxn);

                foreach (ChunkEntry entry in mdxnChunk.Entries)
                {
                    if (entry.GetChunkType() != ChunkType.ResourceChunk)
                        continue;
                    
                    models.Add(ChunkFile.GetChunkFromEntry(entry));
                }

                return models;
            }
        }

        public ChunkBlock NonRenderChunk
        {
            get
            {
                ChunkEntry nonr = ModelResourceChunk.Entries[1];

                return ChunkFile.Chunks.Find((c) => c.HasParent && c.Parent == nonr);
            }
        }

        public virtual void Load()
        {

        }

        public ModelResourcePackage(ChunkFile chunkFile, ChunkBlock modelResourceChunk)
        {
            ChunkFile = chunkFile;
            ModelResourceChunk = modelResourceChunk;
            
        }
    }
}
