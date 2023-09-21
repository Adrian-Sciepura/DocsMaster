using Documentation.Models.CodeElements.TypeKind;
using System.Collections.Generic;

namespace Documentation.Models.CodeElements.Documentation
{
    internal class CodeDocumentationElement : IComparable<CodeDocumentationElement>
    {
        public CodeDocumentationElementType Type { get; set; }
        public string Text { get; set; }
        public List<CodeDocumentationElement> SubElements { get; set; }
        public Dictionary<string, BaseCodeDeclarationKind> Attributes { get; set; }


        public CodeDocumentationElement(CodeDocumentationElementType type)
        {
            Type = type;
            Text = string.Empty;
            SubElements = new List<CodeDocumentationElement>();
            Attributes = new Dictionary<string, BaseCodeDeclarationKind>();
        }


        public enum CodeDocumentationElementType
        {
            Summary,
            Remarks,
            Returns,
            Param,
            Paramref,
            Exception,
            Value,
            Para,

            List,
            ListHeader,
            Item,
            Term,
            Description,

            C,
            Code,
            Example,
            Inheritdoc,
            Include,
            See,
            SeeAlso,
            Typeparam,
            Typeparamref
        }

        public int CompareTo(CodeDocumentationElement other)
        {
            return Type.CompareTo(other.Type);
        }
    }
}
