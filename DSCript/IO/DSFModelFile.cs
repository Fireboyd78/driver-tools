using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSCript.Models
{
    public class DSFModelFile : ModelFile
    {
        List<ModelResourcePackage> _models;

        public new List<ModelResourcePackage> Models
        {
            get
            {
                return _models;
            }
            protected set
            {
                _models = value; 
            }
        }

        public override void Dispose()
        {
            if (ChunkFile != null)
                ChunkFile.Dispose();

            if (Models != null)
            {
                for (int i = 0; i < Models.Count; i++)
                    Models[i] = null;

                Models.Clear();
            }
        }

        public override void LoadModels()
        {
            Models = new List<ModelResourcePackage>();

            // Get resource chunks
            foreach (ChunkBlock chunk in ChunkFile.Chunks)
            {
                if (!chunk.HasParent)
                    continue;

                if (chunk.Entries.Count != 2)
                    return;

                if (chunk.Entries[0].GetChunkType() == ChunkType.ModelPackagePC_X
                    && chunk.Entries[1].GetChunkType() == ChunkType.NonRendererData)
                    Models.Add(new ModelResourcePackage(ChunkFile, chunk));
            }

            DSC.Log("Loaded {0} models from '{1}'!", Models.Count, ChunkFile.Filename);
        }

        public DSFModelFile(string filename)
        {
            ChunkFile = new ChunkFile(filename);
            LoadModels();
        }
    }
}
