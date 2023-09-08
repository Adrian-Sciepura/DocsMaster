using Documentation.Common;
using Documentation.Models.CodeElements.Documentation;
using Documentation.Models.CodeElements.TypeKind;
using Documentation.Models.CodeElements.Types;
using Microsoft.VisualStudio.Package;
using System.Text;
using System.Xml.Linq;

namespace Documentation.Models.CodeElements.Variables
{
    internal class CodeVariable : CodeElement
    {
        public string FieldName { get; set; }

        protected CodeVariable(IParentType parent, CodeElementType type, BaseCodeDeclarationKind declaration, string fieldName, string? accessModifier = null, CodeDocumentation? documentation = null) :
            base(parent, type, declaration, accessModifier, documentation)
        {
            FieldName = fieldName;
        }

        public CodeVariable(IParentType parent, BaseCodeDeclarationKind declaration, string fieldName, string? accessModifier = null, CodeDocumentation? documentation = null) :
            base(parent, CodeElementType.Variable, declaration, accessModifier, documentation)
        {
            FieldName = fieldName;
        }

        /*public override XElement ConvertToXml()
        {
            XElement xmlVariable = new XElement("variable");
            xmlVariable.Add(new XAttribute("name", FieldName));

            if (AccessModifier != null)
                xmlVariable.Add(new XAttribute("access", AccessModifier));

            xmlVariable.Add(new XAttribute("valueType", Declaration.ConvertToXml()));

            return xmlVariable;
        }*/
    }
}
