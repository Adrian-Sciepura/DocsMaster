using Documentation.Models.CodeElements.Documentation;
using Documentation.Models.CodeElements.TypeKind;
using Documentation.Models.CodeElements.Types;

namespace Documentation.Models.CodeElements
{
    internal abstract class CodeElement : IComparable<CodeElement>
    {
        public CodeElementType Type { get; set; }
        public BaseCodeDeclarationKind Declaration { get; set; }
        public string? AccessModifier { get; set; }
        public CodeDocumentation? Documentation { get; set; }
        public IParentType? Parent { get; set; }

        public CodeElement(IParentType parent, CodeElementType type, BaseCodeDeclarationKind declaration, string? accessModifier, CodeDocumentation? documentation = null)
        {
            Type = type;
            Declaration = declaration;
            AccessModifier = accessModifier;
            Documentation = documentation;
            Parent = parent;
        }

        public int CompareTo(CodeElement other)
        {
            return Type.CompareTo(other.Type);
        }
    }
}
