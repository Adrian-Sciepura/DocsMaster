using Documentation.Common;
using Documentation.Models.CodeElements.Documentation;
using Documentation.Models.CodeElements.TypeKind;
using System.Collections.Generic;
using System.Text;

namespace Documentation.Models.CodeElements.Variables
{
    internal class CodeProperty : CodeVariable
    {
        public List<string> Accessors { get; set; }
        public CodeProperty(IParentType parent, BaseCodeDeclarationKind declaration, string name, string? accessModifier = null, List<string>? accessors = null, CodeDocumentation? documentation = null) :
            base(parent, CodeElementType.Property, declaration, name, accessModifier, documentation)
        {
            Accessors = accessors ?? new List<string>();
        }
    }
}
