using Documentation.Engine.Models.CodeDocs;
using Documentation.Engine.Models.CodeElements.TypeKind;

namespace Documentation.Engine.Models.CodeElements.Methods
{
    internal abstract class BaseCodeMethod : CodeElement
    {
        protected BaseCodeMethod(CodeElementType type, BaseCodeDeclarationKind declaration, CodeNamespace namespaceReference, IParentType parent, string? accessModifier, CodeDocumentation? documentation = null) :
            base(type, declaration, namespaceReference, parent, accessModifier, documentation)
        {
        }
    }
}
