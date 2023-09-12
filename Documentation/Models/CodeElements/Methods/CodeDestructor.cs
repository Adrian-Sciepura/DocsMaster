using Documentation.Models.CodeElements.Documentation;
using Documentation.Models.CodeElements.TypeKind;

namespace Documentation.Models.CodeElements.Methods
{
    internal class CodeDestructor : BaseCodeMethod
    {
        public CodeDestructor(CodeRegularDeclaration declaration, CodeNamespace namespaceReference, IParentType parent, CodeDocumentation documentation = null) :
            base(CodeElementType.Destructor, declaration, namespaceReference, parent, null, documentation)
        {
        }
    }
}
