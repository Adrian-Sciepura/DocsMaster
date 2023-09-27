using Documentation.Engine.Models.CodeDocs;
using Documentation.Engine.Models.CodeElements.TypeKind;
using Documentation.Engine.Models.CodeElements.Variables;
using System.Collections.Generic;

namespace Documentation.Engine.Models.CodeElements.Methods
{
    internal class CodeOperator : BaseCodeMethod
    {
        public List<CodeField> Parameters { get; set; }
        public BaseCodeDeclarationKind ReturnType { get; set; }

        public CodeOperator(CodeRegularDeclaration declaration, CodeNamespace namespaceReference, IParentType parent, string accessModifier, BaseCodeDeclarationKind returnType, List<CodeField>? parameters = null, CodeDocumentation documentation = null) :
            base(CodeElementType.Operator, declaration, namespaceReference, parent, accessModifier, documentation)
        {
            ReturnType = returnType;
            Parameters = parameters ?? new List<CodeField>();
        }
    }
}
