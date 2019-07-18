using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SharpDX;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Drawing;
using System.Text;
using System.IO.Compression;

namespace LDDCollada
{
    public static class DAEConversion
    {
        public static void ConvertLXFML(string filename)
        {
            Console.WriteLine("Converting '" + filename + "'...");

            DAESession session = new DAESession();

            session.PlaceFile(XDocument.Load(filename), Matrix.Identity, session.VisualScene, Path.GetDirectoryName(filename));

            session.Document.Save(Path.ChangeExtension(filename, ".dae"));
        }

        public static void ConvertLXF(string filename)
        {
            Console.WriteLine("Converting compressed archive '" + filename + "'...");

            DAESession session = new DAESession();

            using (ZipArchive archive = ZipFile.Open(filename, ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.Name.EndsWith(".lxfml", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Console.WriteLine("  Using compressed file '" + entry.Name + "'...");
                        using (Stream xmlStream = entry.Open())
                        {
                            session.PlaceFile(XDocument.Load(xmlStream), Matrix.Identity, session.VisualScene, Path.GetDirectoryName(filename));
                        }
                        break;
                    }
                }
            }

            session.Document.Save(Path.ChangeExtension(filename, ".dae"));
        }

        private class DAESession
        {
            private const string COLLADA_NAMESPACE = "http://www.collada.org/2005/11/COLLADASchema";

            public XDocument Document;
            public XElement ImageLibrary;
            public XElement MaterialLibrary;
            public XElement EffectLibrary;
            public XElement GeometryLibrary;
            public XElement VisualScene;

            private HashSet<string> CreatedPrimitives = new HashSet<string>();
            private HashSet<int> CreatedMaterials = new HashSet<int>();
            private HashSet<string> CreatedDecorations = new HashSet<string>();

            private int NextNodeID = 1;
            private int AcquireNodeID()
            {
                return NextNodeID++;
            }

            public DAESession()
            {
                XNamespace ns = COLLADA_NAMESPACE;

                Document = new XDocument();

                XElement collada = new XElement(ns + "COLLADA", new XAttribute("xmlns", COLLADA_NAMESPACE), new XAttribute("version", "1.4.1"));

                collada.Add(new XElement(ns + "asset",
                    new XElement(ns + "contributor",
                        new XElement(ns + "author"),
                        new XElement(ns + "authoring_tool", "LDDCollada.Shared " + System.Reflection.Assembly.GetAssembly(typeof(DAESession)).GetName().Version.ToString()),
                        new XElement(ns + "comments")),
                    new XElement(ns + "created", DateTime.Now.ToString("o")),
                    new XElement(ns + "keywords"),
                    new XElement(ns + "modified", DateTime.Now.ToString("o")),
                    new XElement(ns + "unit", new XAttribute("meter", "0.01"), new XAttribute("name", "centimeter")),
                    new XElement(ns + "up_axis", "Y_UP")));

                ImageLibrary = new XElement(ns + "library_images");
                MaterialLibrary = new XElement(ns + "library_materials");
                EffectLibrary = new XElement(ns + "library_effects");
                GeometryLibrary = new XElement(ns + "library_geometries");
                VisualScene = new XElement(ns + "visual_scene", new XAttribute("id", "Scene"), new XAttribute("name", "Scene"));

                collada.Add(ImageLibrary);
                collada.Add(MaterialLibrary);
                collada.Add(EffectLibrary);
                collada.Add(GeometryLibrary);
                collada.Add(new XElement(ns + "library_visual_scenes", VisualScene));
                collada.Add(new XElement(ns + "scene",
                    new XElement(ns + "instance_visual_scene", new XAttribute("url", "#Scene"))));

                Document.Add(collada);
            }

            public void PlaceFile(XDocument doc, Matrix transform, XElement parent, string textureDirectory)
            {
                XElement lxfml = doc.Element("LXFML");
                if (Int32.Parse(lxfml.Attribute("versionMajor").Value) < 5)
                {
                    Console.WriteLine("  [ERROR]: LXFML file must be at least version 5.0");
                }

                string name = "file";
                XElement designNameAnnotation = lxfml.Element("Meta").Elements("Annotation").FirstOrDefault(element => element.Attribute("designname") != null);
                if (designNameAnnotation != null) // For builtin assemblies
                    name = designNameAnnotation.Attribute("designname").Value;
                else if (lxfml.Attribute("name") != null) // For user files
                    name = lxfml.Attribute("name").Value;

                name = name.Replace(' ', '_');

                string nodeID = "part" + AcquireNodeID();
                XElement fileElement = new XElement((XNamespace)COLLADA_NAMESPACE + "node", new XAttribute("id", nodeID), new XAttribute("sid", nodeID), new XAttribute("name", name));

                foreach (XElement brick in lxfml.Element("Bricks").Elements("Brick"))
                {
                    foreach (XElement part in brick.Elements("Part"))
                    {
                        XElement bone = part.Element("Bone");
                        Matrix partTransform = LDDDB.DecodeLDDTransform(bone.Attribute("transformation").Value);
                        string[] materials = part.Attribute("materials").Value.Split(',');
                        string[] decorations = new string[0];
                        if (part.Attribute("decoration") != null)
                            decorations = part.Attribute("decoration").Value.Split(',');
                        PlaceInstance(part.Attribute("designID").Value, partTransform, fileElement, materials.Select(str => Int32.Parse(str)), decorations.Select(str => Int32.Parse(str)), textureDirectory);
                    }
                }

                parent.Add(fileElement);
            }

            private static XElement WriteMatrix(Matrix m)
            {
                m.Transpose();
                return new XElement((XNamespace)COLLADA_NAMESPACE + "matrix", String.Join(" ", m.ToArray()));
            }

            public void PlaceInstance(string designID, Matrix transform, XElement parent, IEnumerable<int> materials, IEnumerable<int> decorations, string textureDirectory)
            {
                if (LDDDB.HasPrimitive(designID))
                {
                    Console.WriteLine("  Primitive '" + designID + "'...");

                    string name = designID + "_" + String.Join("_", materials) + "_" + String.Join("_", decorations);

                    if (!CreatedPrimitives.Contains(name))
                    {
                        GeometryLibrary.Add(GFileToGeometry(LDDDB.GetPrimitive(designID), name));
                        CreatedPrimitives.Add(name);
                    }

                    // Prepare material binds
                    int slot = 0;
                    XElement technique = new XElement((XNamespace)COLLADA_NAMESPACE + "technique_common");
                    int? firstMaterialID = null;
                    foreach (int materialID in materials)
                    {
                        if (materialID == 0)
                        {
                            int decorationIndex = decorations.ElementAt(slot - 1);
                            if (decorationIndex == 0)
                            {
                                if (firstMaterialID.HasValue)
                                {
                                    technique.Add(new XElement((XNamespace)COLLADA_NAMESPACE + "instance_material", new XAttribute("symbol", "slot" + slot), new XAttribute("target", "#mat" + firstMaterialID.Value)));
                                }

                            }
                            else if (firstMaterialID.HasValue)
                            {
                                // Use decorationIndex to find the decoration material
                                if (!CreatedDecorations.Contains(decorationIndex + "_on_" + firstMaterialID.Value))
                                {
                                    CreateDecoration(decorationIndex, firstMaterialID.Value, textureDirectory);
                                    CreatedDecorations.Add(decorationIndex + "_on_" + firstMaterialID.Value);
                                }
                                technique.Add(new XElement((XNamespace)COLLADA_NAMESPACE + "instance_material", new XAttribute("symbol", "slot" + slot), new XAttribute("target", "#mat_deco" + decorationIndex + "_on_" + firstMaterialID.Value)));
                            }
                            else
                            {

                            }
                        }
                        else
                        {
                            firstMaterialID = materialID;
                            if (!CreatedMaterials.Contains(materialID))
                            {
                                CreateMaterial(materialID);
                                CreatedMaterials.Add(materialID);
                            }
                            technique.Add(new XElement((XNamespace)COLLADA_NAMESPACE + "instance_material", new XAttribute("symbol", "slot" + slot), new XAttribute("target", "#mat" + materialID)));
                        }
                        slot++;
                    }

                    string nodeID = "part" + AcquireNodeID() + "_" + designID;
                    XElement nodeElement = new XElement((XNamespace)COLLADA_NAMESPACE + "node", new XAttribute("id", nodeID), new XAttribute("sid", nodeID), new XAttribute("name", nodeID),
                        WriteMatrix(transform),
                        new XElement((XNamespace)COLLADA_NAMESPACE + "instance_geometry", new XAttribute("url", "#" + name + "-GEO"),
                            new XElement((XNamespace)COLLADA_NAMESPACE + "bind_material",
                                technique)));

                    parent.Add(nodeElement);

                    if (decorations.Count() > 0 && LDDDB.DuplicateDecalBricks)
                    {
                        // Do it again, but without decorations
                        // Prepare material binds
                        name = name + "_blank";
                        if (!CreatedPrimitives.Contains(name))
                        {
                            GeometryLibrary.Add(GFileToGeometry(LDDDB.GetPrimitive(designID), name));
                            CreatedPrimitives.Add(name);
                        }
                        slot = 0;
                        technique = new XElement((XNamespace)COLLADA_NAMESPACE + "technique_common");
                        firstMaterialID = null;
                        foreach (int materialID in materials)
                        {
                            if (materialID == 0)
                            {
                                if (firstMaterialID.HasValue)
                                {
                                    technique.Add(new XElement((XNamespace)COLLADA_NAMESPACE + "instance_material", new XAttribute("symbol", "slot" + slot), new XAttribute("target", "#mat" + firstMaterialID.Value)));
                                }
                            }
                            else
                            {
                                firstMaterialID = materialID;
                                if (!CreatedMaterials.Contains(materialID))
                                {
                                    CreateMaterial(materialID);
                                    CreatedMaterials.Add(materialID);
                                }
                                technique.Add(new XElement((XNamespace)COLLADA_NAMESPACE + "instance_material", new XAttribute("symbol", "slot" + slot), new XAttribute("target", "#mat" + materialID)));
                            }
                            slot++;
                        }

                        nodeID = nodeID + "_blank";
                        nodeElement = new XElement((XNamespace)COLLADA_NAMESPACE + "node", new XAttribute("id", nodeID), new XAttribute("sid", nodeID), new XAttribute("name", nodeID),
                            WriteMatrix(transform),
                            new XElement((XNamespace)COLLADA_NAMESPACE + "instance_geometry", new XAttribute("url", "#" + name + "-GEO"),
                                new XElement((XNamespace)COLLADA_NAMESPACE + "bind_material",
                                    technique)));

                        parent.Add(nodeElement);
                    }

                }
                else if (LDDDB.HasAssembly(designID))
                {
                    Console.WriteLine("  Assembly '" + designID + "'...");
                    PlaceFile(LDDDB.GetAssembly(designID), transform, parent, textureDirectory);
                }
                else
                {
                    Console.WriteLine("  [WARNING]: Design ID '" + designID + "' is not a primitive or an assembly in this brick set!");
                }
            }

            private XElement GFileToGeometry(Geometry geometry, string name)
            {
                XNamespace ns = COLLADA_NAMESPACE;

                StringBuilder posData = new StringBuilder();
                StringBuilder texcoordData = new StringBuilder();

                uint vertexCount = 0;
                foreach (GFile gFile in geometry.Segments)
                {
                    foreach (Vector3 pos in gFile.Positions)
                    {
                        posData.Append(pos.X);
                        posData.Append(" ");
                        posData.Append(pos.Y);
                        posData.Append(" ");
                        posData.Append(pos.Z);
                        posData.Append(" ");
                    }
                    if (gFile.Flags.HasFlag(GFileFlags.TexCoords))
                    {
                        foreach (Vector2 uv in gFile.TexCoords)
                        {
                            texcoordData.Append(uv.X);
                            texcoordData.Append(" ");
                            texcoordData.Append(LDDDB.FlipTextureCoordinates ? 1.0f - uv.Y : uv.Y); // Flip for Maya
                            texcoordData.Append(" ");
                        }
                    }
                    else
                    {
                        for (int i = 0; i < gFile.VertexCount; i++)
                        {
                            texcoordData.Append("0 0 ");
                        }
                    }
                    vertexCount += gFile.VertexCount;
                }

                XElement positionSource = new XElement(ns + "source", new XAttribute("id", name + "-POS"),
                    new XElement(ns + "float_array", new XAttribute("id", name + "-POS-array"), new XAttribute("count", vertexCount * 3), posData.ToString()),
                    new XElement(ns + "technique_common",
                        new XElement(ns + "accessor", new XAttribute("source", "#" + name + "-POS-array"), new XAttribute("count", vertexCount), new XAttribute("stride", "3"),
                            new XElement(ns + "param", new XAttribute("name", "X"), new XAttribute("type", "float")),
                            new XElement(ns + "param", new XAttribute("name", "Y"), new XAttribute("type", "float")),
                            new XElement(ns + "param", new XAttribute("name", "Z"), new XAttribute("type", "float")))));

                XElement texcoordSource = new XElement(ns + "source", new XAttribute("id", name + "-UV"),
                    new XElement(ns + "float_array", new XAttribute("id", name + "-UV-array"), new XAttribute("count", vertexCount * 2), texcoordData.ToString()),
                    new XElement(ns + "technique_common",
                        new XElement(ns + "accessor", new XAttribute("source", "#" + name + "-UV-array"), new XAttribute("count", vertexCount), new XAttribute("stride", "2"),
                            new XElement(ns + "param", new XAttribute("name", "S"), new XAttribute("type", "float")),
                            new XElement(ns + "param", new XAttribute("name", "T"), new XAttribute("type", "float")))));

                XElement mesh = new XElement(ns + "mesh",
                    positionSource,
                    texcoordSource,
                    new XElement(ns + "vertices", new XAttribute("id", name + "-VTX"),
                        new XElement(ns + "input", new XAttribute("semantic", "POSITION"), new XAttribute("source", "#" + name + "-POS")),
                        new XElement(ns + "input", new XAttribute("semantic", "TEXCOORD"), new XAttribute("source", "#" + name + "-UV"))));


                vertexCount = 0;
                uint indexCount = 0;
                int slot = 0;
                foreach (GFile gFile in geometry.Segments)
                {
                    StringBuilder indexData = new StringBuilder();
                    foreach (uint index in gFile.Indices)
                    {
                        indexData.Append(index + vertexCount);
                        indexData.Append(" ");
                    }

                    mesh.Add(new XElement(ns + "triangles", new XAttribute("count", gFile.IndexCount / 3), new XAttribute("material", "slot" + slot),
                            new XElement(ns + "input", new XAttribute("semantic", "VERTEX"), new XAttribute("offset", "0"), new XAttribute("source", "#" + name + "-VTX")),
                            new XElement(ns + "p", indexData.ToString())));

                    vertexCount += gFile.VertexCount;
                    indexCount += gFile.IndexCount;
                    slot++;
                }

                return new XElement(ns + "geometry", new XAttribute("id", name + "-GEO"), new XAttribute("name", name),
                    mesh);
            }

            private void CreateMaterial(int matID)
            {
                XNamespace ns = COLLADA_NAMESPACE;
                Material material = LDDDB.GetMaterial(matID);
                MaterialLibrary.Add(new XElement(ns + "material", new XAttribute("id", "mat" + matID), new XAttribute("name", "Mat" + matID),
                    new XElement(ns + "instance_effect", new XAttribute("url", "#mat" + matID + "-FX"))));
                EffectLibrary.Add(new XElement(ns + "effect", new XAttribute("id", "mat" + matID + "-FX"), new XAttribute("name", "Mat" + matID),
                    new XElement(ns + "profile_COMMON",
                        new XElement(ns + "technique", new XAttribute("sid", "standard"),
                            new XElement(ns + "phong",
                                new XElement(ns + "emission",
                                    new XElement(ns + "color", new XAttribute("sid", "emission"), "0.0 0.0 0.0 1.0")),
                                new XElement(ns + "ambient",
                                    new XElement(ns + "color", new XAttribute("sid", "ambient"), "0 0 0 1.0")),
                                new XElement(ns + "diffuse",
                                    new XElement(ns + "color", new XAttribute("sid", "diffuse"), UnSRGB(material.Red / 255.0f) + " " + UnSRGB(material.Green / 255.0f) + " " + UnSRGB(material.Blue / 255.0f) + " 1.0")),
                                new XElement(ns + "specular",
                                    new XElement(ns + "color", new XAttribute("sid", "specular"), "0.5 0.5 0.5 1.0")),
                                new XElement(ns + "shininess",
                                    new XElement(ns + "float", new XAttribute("sid", "shininess"), "0.3")),
                                new XElement(ns + "transparency",
                                    new XElement(ns + "float", new XAttribute("sid", "transparency"), material.Alpha / 255.0f)))))));
            }

            private void CreateDecoration(int decorationID, int matID, string outputDirectory)
            {
                string name = decorationID + "_on_" + matID;
                XNamespace ns = COLLADA_NAMESPACE;
                Material material = LDDDB.GetMaterial(matID);
                string dbFilename = Path.Combine(LDDDB.PartDBPath, "Decorations", decorationID + ".png");
                string destFilename;
                if (LDDDB.CopyTextures)
                {
                    if (LDDDB.ColorizeTextures)
                    {
                        destFilename = Path.GetFullPath(Path.Combine(outputDirectory, name + ".png"));
                        ApplyBackgroundColor(dbFilename, destFilename, material.Red, material.Green, material.Blue, material.Alpha);
                    }
                    else
                    {
                        destFilename = Path.Combine(outputDirectory, decorationID + ".png");
                        File.Copy(dbFilename, destFilename);
                    }
                }
                else
                {
                    destFilename = dbFilename;
                }
                ImageLibrary.Add(new XElement(ns + "image", new XAttribute("id", "deco-" + name + "-TEX"), new XAttribute("name", "deco_" + name),
                    new XElement(ns + "init_from", "file://" + destFilename.Replace('\\', '/'))));
                MaterialLibrary.Add(new XElement(ns + "material", new XAttribute("id", "mat_deco" + name), new XAttribute("name", "Mat_Deco" + name),
                    new XElement(ns + "instance_effect", new XAttribute("url", "#mat_deco" + name + "-FX"))));
                EffectLibrary.Add(new XElement(ns + "effect", new XAttribute("id", "mat_deco" + name + "-FX"), new XAttribute("name", "Mat_Deco" + name),
                    new XElement(ns + "profile_COMMON",
                        new XElement(ns + "technique", new XAttribute("sid", "standard"),
                            new XElement(ns + "phong",
                                new XElement(ns + "emission",
                                    new XElement(ns + "color", new XAttribute("sid", "emission"), "0.0 0.0 0.0 1.0")),
                                new XElement(ns + "ambient",
                                    new XElement(ns + "color", new XAttribute("sid", "ambient"), "0 0 0 1.0")),
                                new XElement(ns + "diffuse",
                                    new XElement(ns + "texture", new XAttribute("texture", "deco-" + name + "-TEX"), new XAttribute("texcoord", "CHANNEL0")/*,
                                            new XElement(ns + "extra",
                                                new XElement(ns + "techique", new XAttribute("profile", "MAYA"),
                                                    new XElement(ns + "wrapU", new XAttribute("sid", "wrapU0"), "FALSE"),
                                                    new XElement(ns + "wrapV", new XAttribute("sid", "wrapV0"), "FALSE")))*/)),
                                new XElement(ns + "specular",
                                    new XElement(ns + "color", new XAttribute("sid", "specular"), "0.5 0.5 0.5 1.0")),
                                new XElement(ns + "shininess",
                                    new XElement(ns + "float", new XAttribute("sid", "shininess"), "0.3"))/*,
                                    new XElement(ns + "transparent", new XAttribute("opaque", "RGB_ZERO"),
                                        new XElement(ns + "texture", new XAttribute("texture", "deco-" + name + "-TEX"), new XAttribute("texcoord", "CHANNEL0")))*/)))));
            }

            private static float UnSRGB(float input)
            {
                return (float)Math.Pow(input, 2.2f);
            }

            private static void ApplyBackgroundColor(string inputFilename, string outputFilename, byte r, byte g, byte b, byte a)
            {
                if (File.Exists(outputFilename))
                    return;

                Console.WriteLine("  Blending decal " + Path.GetFileNameWithoutExtension(inputFilename) + "...");
                using (Bitmap bitmap = new Bitmap(inputFilename))
                {
                    BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                    byte[] pixels = new byte[bitmap.Width * bitmap.Height * 4];
                    Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);

                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        for (int y = 0; y < bitmap.Height; y++)
                        {
                            // Order is BRGA
                            int pixelOffset = y * data.Stride + x * 4;
                            pixels[pixelOffset] = Lerp(b, pixels[pixelOffset], pixels[pixelOffset + 3]);
                            pixels[pixelOffset + 1] = Lerp(g, pixels[pixelOffset + 1], pixels[pixelOffset + 3]);
                            pixels[pixelOffset + 2] = Lerp(r, pixels[pixelOffset + 2], pixels[pixelOffset + 3]);
                            pixels[pixelOffset + 3] = Lerp(a, pixels[pixelOffset + 3], pixels[pixelOffset + 3]);
                        }
                    }

                    Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
                    bitmap.UnlockBits(data);
                    bitmap.Save(outputFilename);
                }
            }

            private static byte Lerp(byte a, byte b, byte t)
            {
                float v = t / (float)Byte.MaxValue;
                return (byte)(b * v + a * (1.0f - v));
            }
        }
    }
}
