
using Documentation.Engine.Models.CodeDocs;
using Documentation.Engine.Models.CodeElements.TypeKind;

namespace Documentation.Engine.Models.CodeElements.Methods
{
    internal class CodeDestructor : BaseCodeMethod
    {
        public CodeDestructor(CodeRegularDeclaration declaration, CodeNamespace namespaceReference, IParentType parent, CodeDocumentation documentation = null) :
            base(CodeElementType.Destructor, declaration, namespaceReference, parent, null, documentation)
        {
        }
    }
}
