using Documentation.Models.CodeElements.Documentation;
using Documentation.Models.CodeElements.TypeKind;
using Microsoft.VisualStudio.Package;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Documentation.Models.CodeElements.Types
{
    internal class CodeEnum : BaseCodeType
    {
        public List<string> Elements { get; set; }

        public CodeEnum(IParentType parent, CodeRegularDeclaration declaration, string? accessModifier, CodeDocumentation? documentation = null, List<string>? elements = null) :
            base(parent, CodeElementType.Enum, declaration, accessModifier, documentation)
        {
            Elements = elements ?? new List<string>();
        }

        /*public override XElement ConvertToXml()
        {
            XElement xmlEnum = new XElement("enum");
            xmlEnum.Add(new XAttribute("name", Declaration.ConvertToXml()));

            if (AccessModifier != null)
                xmlEnum.Add(new XAttribute("access", AccessModifier));

            if (Elements.Count > 0)
                xmlEnum.Add(Elements.Select(x => new XElement("element", x)));

            return xmlEnum;
        }*/
    }
}
