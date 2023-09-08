using Documentation.Models.CodeElements.Documentation;
using Documentation.Models.CodeElements.TypeKind;
using Documentation.Models.CodeElements.Types;
using Documentation.Models.CodeElements.Variables;
using System.Collections.Generic;

namespace Documentation.Models.CodeElements.Methods
{
    internal class CodeMethod : BaseCodeMethod
    {
        public List<CodeVariable> Parameters { get; set; }
        public BaseCodeDeclarationKind ReturnType { get; set; }

        public CodeMethod(IParentType parent, BaseCodeDeclarationKind declaration, string accessModifier, BaseCodeDeclarationKind returnType, List<CodeVariable>? parameters = null, CodeDocumentation documentation = null) :
            base(parent, CodeElementType.Method, declaration, accessModifier, documentation)
        {
            ReturnType = returnType;
            Parameters = parameters ?? new List<CodeVariable>();
        }
    }
}
