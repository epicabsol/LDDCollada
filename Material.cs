using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LDDCollada
{
    public class Material
    {
        public const string MATERIAL_TYPE_SHINY_PLASTIC = "shinyPlastic";
        public const string MATERIAL_TYPE_SHINY_STEEL = "shinySteel";

        public int MatID { get; }
        public byte Red { get; }
        public byte Green { get; }
        public byte Blue { get; }
        public byte Alpha { get; }
        public string MaterialType { get; }

        public Material(XElement element)
        {
            MatID = Int32.Parse(element.Attribute("MatID").Value);
            Red = Byte.Parse(element.Attribute("Red").Value);
            Green = Byte.Parse(element.Attribute("Green").Value);
            Blue = Byte.Parse(element.Attribute("Blue").Value);
            Alpha = Byte.Parse(element.Attribute("Alpha").Value);
            MaterialType = element.Attribute("MaterialType").Value;
        }
    }
}
