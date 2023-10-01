

using DocsMaster.Engine.Models.CodeDocs;
using DocsMaster.Engine.Models.CodeElements.TypeKind;
using System;

namespace DocsMaster.Engine.Models.CodeElements
{
    internal abstract class CodeElement : IComparable<CodeElement>
    {
        public CodeElementType Type { get; set; }
        public BaseCodeDeclarationKind Declaration { get; set; }
        public string? AccessModifier { get; set; }
        public CodeDocumentation? Documentation { get; set; }
        public CodeNamespace? Namespace { get; set; }
        public IParentType? Parent { get; set; }

        public CodeElement(CodeElementType type, BaseCodeDeclarationKind declaration, CodeNamespace? namespaceReference = null, IParentType? parent = null, string? accessModifier = null, CodeDocumentation? documentation = null)
        {
            Type = type;
            Declaration = declaration;
            Namespace = namespaceReference;
            Parent = parent;
            AccessModifier = accessModifier;
            Documentation = documentation;
        }

        public int CompareTo(CodeElement other) => Type.CompareTo(other.Type);
    }
}
