using Documentation.FormatBuilders;
using Documentation.Models.CodeElements.Types;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Documentation.Models.CodeElements
{
    internal sealed class CodeNamespace : IParentType
    {
        public string Name { get; set; }
        public string Hash { get; init; }
        public List<BaseCodeType> InternalTypes { get; set; }

        public CodeNamespace(string name, List<BaseCodeType>? internalTypes = null)
        {
            Name = name;
            InternalTypes = internalTypes ?? new List<BaseCodeType>();
            Hash = $"{Name.GetHashCode():X8}";
        }

        public void AddInternalType(BaseCodeType internalType)
        {
            InternalTypes.Add(internalType);
        }

        public string GetName()
        {
            return Name;
        }

        public IParentType? GetParent()
        {
            return null;
        }

        public CodeElementType GetElementType()
        {
            return CodeElementType.Namespace;
        }

        /*public XElement ConvertToXml()
{
   *//*XElement xmlNamespace = new XElement("namespace");

   if (InternalTypes.Count > 0)
       xmlNamespace.Add(InternalTypes.Select(x => x.ConvertToXml()));

   return xmlNamespace;*//*
}*/
    }
}
