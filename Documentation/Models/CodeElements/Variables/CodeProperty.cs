using Documentation.Models.CodeElements.Documentation;
using Documentation.Models.CodeElements.TypeKind;
using System.Collections.Generic;

namespace Documentation.Models.CodeElements.Variables
{
    internal class CodeProperty : CodeVariable
    {
        public List<string> Accessors { get; set; }
        public CodeProperty(BaseCodeDeclarationKind declaration, string name, CodeNamespace namespaceReference, IParentType? parent = null, string? accessModifier = null, List<string>? accessors = null, CodeDocumentation? documentation = null) :
            base(CodeElementType.Property, declaration, name, namespaceReference, parent, accessModifier, documentation)
        {
            Accessors = accessors ?? new List<string>();
        }
    }
}
