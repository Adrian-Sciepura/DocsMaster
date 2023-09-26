using Documentation.Models.CodeElements.Documentation;
using Documentation.Models.CodeElements.TypeKind;
using System.Collections.Generic;

namespace Documentation.Models.CodeElements.Types
{
    internal class CodeEnum : BaseCodeType
    {
        public List<string> Elements { get; set; }

        public CodeEnum(CodeRegularDeclaration declaration, CodeNamespace namespaceReference, List<string>? elements = null, IParentType? parent = null, string? accessModifier = null, CodeDocumentation? documentation = null) :
            base(CodeElementType.Enum, declaration, namespaceReference, parent, accessModifier, documentation)
        {
            Elements = elements ?? new List<string>();
        }
    }
}
