using Documentation.Models.CodeElements.Documentation;
using Documentation.Models.CodeElements.TypeKind;
using Documentation.Models.CodeElements.Variables;
using System.Collections.Generic;

namespace Documentation.Models.CodeElements.Methods
{
    internal class CodeMethod : BaseCodeMethod
    {
        public List<CodeField> Parameters { get; set; }
        public BaseCodeDeclarationKind ReturnType { get; set; }

        public CodeMethod(BaseCodeDeclarationKind declaration, BaseCodeDeclarationKind returnType, CodeNamespace namespaceReference, IParentType parent, string accessModifier, List<CodeField>? parameters = null, CodeDocumentation documentation = null) :
            base(CodeElementType.Method, declaration, namespaceReference, parent, accessModifier, documentation)
        {
            ReturnType = returnType;
            Parameters = parameters ?? new List<CodeField>();
        }
    }
}
