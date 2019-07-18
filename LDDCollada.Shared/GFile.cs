using System;
using System.IO;
using SharpDX;

namespace LDDCollada
{
    [Flags]
    public enum GFileFlags : uint
    {
        None = 0,
        TexCoords = 1 << 0,
        Normals = 1 << 1,
        Flexible = 1 << 2,
        Unknown1 = 1 << 3,
        Unknown2 = 1 << 4,
        Unknown3 = 1 << 5,
    }

    public class GFile
    {
        public uint VertexCount;
        public uint IndexCount;
        public GFileFlags Flags;
        public Vector3[] Positions = null;
        public Vector3[] Normals = null; // Null if Flags does not contain GFileFlags.Normals
        public Vector2[] TexCoords = null; // Null if Flags does not contain GFileFlags.TexCoords
        public uint[] Indices = null;

        public GFile(BinaryReader reader)
        {
            uint magic = reader.ReadUInt32();
            VertexCount = reader.ReadUInt32();
            IndexCount = reader.ReadUInt32();
            Flags = (GFileFlags)reader.ReadUInt32();

            Positions = new Vector3[VertexCount];
            for (int i = 0; i < VertexCount; i++)
            {
                Positions[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }

            if (Flags.HasFlag(GFileFlags.Normals))
            {
                Normals = new Vector3[VertexCount];
                for (int i = 0; i < VertexCount; i++)
                {
                    Normals[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                }
            }

            if (Flags.HasFlag(GFileFlags.TexCoords))
            {
                TexCoords = new Vector2[VertexCount];
                for (int i = 0; i < VertexCount; i++)
                {
                    TexCoords[i] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                }
            }

            Indices = new uint[IndexCount];
            for (int i = 0; i < IndexCount; i++)
            {
                Indices[i] = reader.ReadUInt32();
            }
        }
    }
}
