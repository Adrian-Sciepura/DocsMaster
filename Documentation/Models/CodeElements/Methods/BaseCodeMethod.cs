using Documentation.Models.CodeElements.Documentation;
using Documentation.Models.CodeElements.TypeKind;
using Documentation.Models.CodeElements.Types;

namespace Documentation.Models.CodeElements.Methods
{
    internal abstract class BaseCodeMethod : CodeElement
    {
        protected BaseCodeMethod(CodeElementType type, BaseCodeDeclarationKind declaration, CodeNamespace namespaceReference, IParentType parent, string? accessModifier, CodeDocumentation? documentation = null) :
            base(type, declaration, namespaceReference, parent, accessModifier, documentation)
        {
        }
    }
}
