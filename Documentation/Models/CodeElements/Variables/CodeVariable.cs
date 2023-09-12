using Documentation.Models.CodeElements.Documentation;
using Documentation.Models.CodeElements.TypeKind;

namespace Documentation.Models.CodeElements.Variables
{
    internal class CodeVariable : CodeElement
    {
        public string FieldName { get; set; }

        protected CodeVariable(CodeElementType type, BaseCodeDeclarationKind declaration, string fieldName, CodeNamespace namespaceReference, IParentType? parent = null, string? accessModifier = null, CodeDocumentation? documentation = null) :
            base(type, declaration, namespaceReference, parent, accessModifier, documentation)
        {
            FieldName = fieldName;
        }

        public CodeVariable(BaseCodeDeclarationKind declaration, CodeNamespace namespaceReference, IParentType? parent, string fieldName, string? accessModifier = null, CodeDocumentation? documentation = null) :
            base(CodeElementType.Variable, declaration, namespaceReference, parent, accessModifier, documentation)
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
