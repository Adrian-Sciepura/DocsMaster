using Documentation.Models.CodeElements.Documentation;
using Documentation.Models.CodeElements.TypeKind;
using Documentation.Models.CodeElements.Variables;
using System.Collections.Generic;

namespace Documentation.Models.CodeElements.Types
{
    internal class CodeDelegate : BaseCodeType
    {
        public List<CodeField> Parameters { get; set; }
        public BaseCodeDeclarationKind ReturnType { get; set; }

        public CodeDelegate(BaseCodeDeclarationKind declaration, BaseCodeDeclarationKind returnType, CodeNamespace namespaceReference, List<CodeField>? parameters = null, IParentType? parent = null, string? accessModifier = null, CodeDocumentation documentation = null) :
            base(CodeElementType.Delegate, declaration, namespaceReference, parent, accessModifier, documentation)
        {
            ReturnType = returnType;
            Parameters = parameters ?? new List<CodeField>();
        }
    }
}
