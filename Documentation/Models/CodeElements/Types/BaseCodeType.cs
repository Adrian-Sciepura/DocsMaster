using Documentation.Models.CodeElements.Documentation;
using Documentation.Models.CodeElements.TypeKind;

namespace Documentation.Models.CodeElements.Types
{
    internal abstract class BaseCodeType : CodeElement
    {
        public BaseCodeType(IParentType parent, CodeElementType type, BaseCodeDeclarationKind declaration, string accessModifier, CodeDocumentation documentation = null) :
            base(parent, type, declaration, accessModifier, documentation)
        {
        }
    }
}
