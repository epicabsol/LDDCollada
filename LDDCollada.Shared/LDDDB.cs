using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.IO;
using SharpDX;
using System.Linq;

namespace LDDCollada
{
    public static class LDDDB
    {
        public static string PartDBPath { get; private set; }
        public static bool CopyTextures { get; private set; }
        public static bool ColorizeTextures { get; private set; }
        public static bool DuplicateDecalBricks { get; private set; }
        public static bool FlipTextureCoordinates { get; private set; }

        private static Dictionary<string, Geometry> PrimitiveCache;
        private static Dictionary<string, XDocument> AssemblyCache;
        private static Dictionary<int, Material> Materials;

        public static void Load(string partDBPath, bool copyTextures, bool colorizeTextures, bool duplicateDecalBricks, bool flipTextureCoordinates)
        {
            PartDBPath = partDBPath;
            CopyTextures = copyTextures;
            ColorizeTextures = colorizeTextures;
            DuplicateDecalBricks = duplicateDecalBricks;
            FlipTextureCoordinates = flipTextureCoordinates;

            // Load which assemblies exist
            AssemblyCache = new Dictionary<string, XDocument>();
            foreach (string filename in System.IO.Directory.EnumerateFiles(Path.Combine(PartDBPath, "Assemblies")))
            {
                AssemblyCache.Add(System.IO.Path.GetFileNameWithoutExtension(filename), null);
            }

            // Load which primitives exist
            PrimitiveCache = new Dictionary<string, Geometry>();
            foreach (string filename in Directory.EnumerateFiles(Path.Combine(PartDBPath, "Primitives")))
            {
                PrimitiveCache.Add(System.IO.Path.GetFileNameWithoutExtension(filename), null);
            }

            // Load materials
            Materials = new Dictionary<int, Material>();
            XDocument materialDoc = XDocument.Load(Path.Combine(PartDBPath, "Materials.xml"));
            foreach (XElement materialElement in materialDoc.Root.Elements("Material"))
            {
                Material m = new Material(materialElement);
                Materials.Add(m.MatID, m);
            }
        }

        public static Geometry GetPrimitive(string designID)
        {
            if (PrimitiveCache.ContainsKey(designID))
            {
                Geometry result = PrimitiveCache[designID];
                if (result == null)
                {
                    result = new Geometry();
                    foreach (string geofile in Directory.EnumerateFiles(Path.Combine(PartDBPath, "Primitives", "LOD0")).Where(s => Path.GetFileNameWithoutExtension(s) == designID))
                    {
                        using (FileStream stream = new FileStream(geofile, FileMode.Open, FileAccess.Read, FileShare.Read))
                        using (BinaryReader reader = new BinaryReader(stream))
                        {
                            GFile geo = new GFile(reader);
                            result.PutGFile(geofile, geo);
                        }
                    }
                    PrimitiveCache[designID] = result;
                }
                return result;
            }
            return null;
        }

        public static bool HasPrimitive(string designID)
        {
            return PrimitiveCache.ContainsKey(designID);
        }

        public static XDocument GetAssembly(string designID)
        {
            if (AssemblyCache.ContainsKey(designID))
            {
                XDocument result = AssemblyCache[designID];
                if (result == null)
                {
                    result = XDocument.Load(Path.Combine(PartDBPath, "Assemblies", designID + ".xml"));
                    AssemblyCache[designID] = result;
                }
                return result;
            }
            return null;
        }

        public static bool HasAssembly(string designID)
        {
            return AssemblyCache.ContainsKey(designID);
        }

        public static Material GetMaterial(int materialID)
        {
            if (Materials.ContainsKey(materialID))
            {
                return Materials[materialID];
            }
            else
            {
                return null;
            }
        }

        public static bool HasMaterial(int materialID)
        {
            return Materials.ContainsKey(materialID);
        }

        public static Matrix DecodeLDDTransform(string input)
        {
            string[] parts = input.Split(',');
            return new Matrix(Single.Parse(parts[0]), Single.Parse(parts[1]), Single.Parse(parts[2]), 0.0f,
                Single.Parse(parts[3]), Single.Parse(parts[4]), Single.Parse(parts[5]), 0.0f,
                Single.Parse(parts[6]), Single.Parse(parts[7]), Single.Parse(parts[8]), 0.0f,
                Single.Parse(parts[9]), Single.Parse(parts[10]), Single.Parse(parts[11]), 1.0f);
        }
    }
}
