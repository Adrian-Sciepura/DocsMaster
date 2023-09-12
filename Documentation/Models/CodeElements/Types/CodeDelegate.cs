using Documentation.Models.CodeElements.Documentation;
using Documentation.Models.CodeElements.TypeKind;
using Documentation.Models.CodeElements.Variables;
using System.Collections.Generic;

namespace Documentation.Models.CodeElements.Types
{
    internal class CodeDelegate : BaseCodeType
    {
        public List<CodeVariable> Parameters { get; set; }
        public string ReturnType { get; set; }

        public CodeDelegate(BaseCodeDeclarationKind declaration, string returnType, CodeNamespace namespaceReference, List<CodeVariable>? parameters = null, IParentType? parent = null, string? accessModifier = null, CodeDocumentation documentation = null) :
            base(CodeElementType.Delegate, declaration, namespaceReference, parent, accessModifier, documentation)
        {
            ReturnType = returnType;
            Parameters = parameters ?? new List<CodeVariable>();
        }
    }
}
