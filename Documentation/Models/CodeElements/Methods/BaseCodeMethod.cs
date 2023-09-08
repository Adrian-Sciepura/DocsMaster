using Documentation.Models.CodeElements.Documentation;
using Documentation.Models.CodeElements.TypeKind;
using Documentation.Models.CodeElements.Types;

namespace Documentation.Models.CodeElements.Methods
{
    internal abstract class BaseCodeMethod : CodeElement
    {
        protected BaseCodeMethod(IParentType parent, CodeElementType type, BaseCodeDeclarationKind declaration, string? accessModifier, CodeDocumentation? documentation = null) :
            base(parent, type, declaration, accessModifier, documentation)
        {
        }
    }
}
