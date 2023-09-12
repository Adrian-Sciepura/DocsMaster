using Documentation.Common;
using Documentation.Models.CodeElements.Documentation;
using Documentation.Models.CodeElements.TypeKind;
using Documentation.Models.CodeElements.Types;
using System.Collections.Generic;
using System.Text;

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
