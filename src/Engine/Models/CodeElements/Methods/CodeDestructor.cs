
using DocsMaster.Engine.Models.CodeDocs;
using DocsMaster.Engine.Models.CodeElements.TypeKind;

namespace DocsMaster.Engine.Models.CodeElements.Methods
{
    internal class CodeDestructor : BaseCodeMethod
    {
        public CodeDestructor(CodeRegularDeclaration declaration, CodeNamespace namespaceReference, IParentType parent, CodeDocumentation documentation = null) :
            base(CodeElementType.Destructor, declaration, namespaceReference, parent, null, documentation)
        {
        }
    }
}
