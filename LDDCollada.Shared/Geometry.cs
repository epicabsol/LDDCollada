using System;
using System.Collections.Generic;
using System.IO;

namespace LDDCollada
{
    public class Geometry
    {
        public List<GFile> Segments { get; } = new List<GFile>();

        public void PutGFile(string filename, GFile geometry)
        {
            string extension = Path.GetExtension(filename).Substring(2);
            int index = extension.Length > 0 ? Int32.Parse(extension) : 0;
            while (Segments.Count < index)
            {
                Segments.Add(null);
            }
            Segments.Insert(index, geometry);
        }
    }
}
