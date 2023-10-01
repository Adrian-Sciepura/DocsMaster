using DocsMaster.Engine.Models.CodeDocs;
using DocsMaster.Engine.Models.CodeElements.TypeKind;
using System.Collections.Generic;

namespace DocsMaster.Engine.Models.CodeElements.Variables
{
    internal class CodeProperty : CodeField
    {
        public List<string> Accessors { get; set; }
        public CodeProperty(BaseCodeDeclarationKind declaration, string name, CodeNamespace namespaceReference, IParentType? parent = null, string? accessModifier = null, List<string>? accessors = null, CodeDocumentation? documentation = null) :
            base(CodeElementType.Property, declaration, new List<string>() { name }, namespaceReference, parent, accessModifier, documentation)
        {
            Accessors = accessors ?? new List<string>();
        }
    }
}
