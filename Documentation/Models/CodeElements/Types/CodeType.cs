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

        public CodeType(CodeElementType type, BaseCodeDeclarationKind declaration, CodeNamespace namespaceReference, IParentType? parent = null, string? accessModifier = null, CodeDocumentation? documentation = null, List<CodeElement>? members = null) :
            base(type, declaration, namespaceReference, parent, accessModifier, documentation)
        {
            Members = members ?? new List<CodeElement>();
        }

        public void AddInternalElement(CodeElement element)
        {
            Members.Add(element);
        }

        public CodeElement GetElement()
        {
            return this;
        }

        public IParentType GetParent()
        {
            return Parent;
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
