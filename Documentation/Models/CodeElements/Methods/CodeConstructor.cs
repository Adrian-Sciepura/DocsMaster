using Documentation.Models.CodeElements.Documentation;
using Documentation.Models.CodeElements.TypeKind;
using Documentation.Models.CodeElements.Variables;
using System.Collections.Generic;

namespace Documentation.Models.CodeElements.Methods
{
    internal class CodeConstructor : BaseCodeMethod
    {
        public List<CodeVariable> Parameters { get; set; }

        public CodeConstructor(CodeRegularDeclaration declaration, CodeNamespace namespaceReference, IParentType parent, string accessModifier, List<CodeVariable>? parameters = null, CodeDocumentation documentation = null) :
            base(CodeElementType.Constructor, declaration, namespaceReference, parent, accessModifier, documentation)
        {
            Parameters = parameters ?? new List<CodeVariable>();
        }
    }
}
