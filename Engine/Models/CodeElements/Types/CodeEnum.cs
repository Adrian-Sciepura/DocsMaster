using Documentation.Engine.Models.CodeDocs;
using Documentation.Engine.Models.CodeElements.TypeKind;
using System.Collections.Generic;

namespace Documentation.Engine.Models.CodeElements.Types
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
