using Documentation.Engine.Models.CodeDocs;
using Documentation.Engine.Models.CodeElements.TypeKind;
using Documentation.Engine.Models.CodeElements.Variables;
using System.Collections.Generic;

namespace Documentation.Engine.Models.CodeElements.Methods
{
    internal class CodeConstructor : BaseCodeMethod
    {
        public List<CodeField> Parameters { get; set; }

        public CodeConstructor(CodeRegularDeclaration declaration, CodeNamespace namespaceReference, IParentType parent, string accessModifier, List<CodeField>? parameters = null, CodeDocumentation documentation = null) :
            base(CodeElementType.Constructor, declaration, namespaceReference, parent, accessModifier, documentation)
        {
            Parameters = parameters ?? new List<CodeField>();
        }
    }
}
