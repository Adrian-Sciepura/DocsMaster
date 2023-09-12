using Documentation.Models.CodeElements.Documentation;
using Documentation.Models.CodeElements.TypeKind;
using Documentation.Models.CodeElements.Types;

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
