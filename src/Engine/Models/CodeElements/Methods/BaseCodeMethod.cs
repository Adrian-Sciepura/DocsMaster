using DocsMaster.Engine.Models.CodeDocs;
using DocsMaster.Engine.Models.CodeElements.TypeKind;

namespace DocsMaster.Engine.Models.CodeElements.Methods
{
    internal abstract class BaseCodeMethod : CodeElement
    {
        protected BaseCodeMethod(CodeElementType type, BaseCodeDeclarationKind declaration, CodeNamespace namespaceReference, IParentType parent, string? accessModifier, CodeDocumentation? documentation = null) :
            base(type, declaration, namespaceReference, parent, accessModifier, documentation)
        {
        }
    }
}
