using Documentation.Engine.Models.CodeDocs;
using Documentation.Engine.Models.CodeElements.TypeKind;

namespace Documentation.Engine.Models.CodeElements.Types
{
    internal abstract class BaseCodeType : CodeElement
    {
        public BaseCodeType(CodeElementType type, BaseCodeDeclarationKind declaration, CodeNamespace namespaceReference, IParentType? parent = null, string? accessModifier = null, CodeDocumentation? documentation = null) :
            base(type, declaration, namespaceReference, parent, accessModifier, documentation)
        {
        }
    }
}
