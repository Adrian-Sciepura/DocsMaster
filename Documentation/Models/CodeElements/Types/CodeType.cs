using Documentation.Models.CodeElements.Documentation;
using Documentation.Models.CodeElements.TypeKind;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Documentation.Models.CodeElements.Types
{
    internal class CodeType : BaseCodeType, IParentType
    {
        public List<CodeElement> Members { get; set; }

        public CodeType(IParentType parent, CodeElementType type, BaseCodeDeclarationKind declaration, string? accessModifier, CodeDocumentation? documentation = null, List<CodeElement>? members = null) :
            base(parent, type, declaration, accessModifier, documentation)
        {
            Members = members ?? new List<CodeElement>();
        }

        public void AddInternalType(BaseCodeType internalType)
        {
            Members.Add(internalType);
        }

        public string GetName()
        {
            return Declaration.GetName();
        }

        public IParentType? GetParent()
        {
            return Parent;
        }

        public CodeElementType GetElementType()
        {
            return Type;
        }

        /*public override XElement ConvertToXml()
        {
            *//*XElement xmlType = new XElement(Type.ToString());
            xmlType.Add(new XAttribute("name", Name));

            if (AccessModifier != null)
                xmlType.Add(new XAttribute("access", AccessModifier));

            if (InternalTypes.Count > 0)
                xmlType.Add(new XElement("internalTypes", InternalTypes.Select(x => x.ConvertToXml())));

            if (Members.Count > 0)
                xmlType.Add(new XElement("members", Members.Select(x => x.ConvertToXml())));

            return xmlType;*//*
            throw new NotImplementedException();
        }*/
    }
}
