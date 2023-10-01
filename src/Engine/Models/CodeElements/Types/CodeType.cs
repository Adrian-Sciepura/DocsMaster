using DocsMaster.Engine.Models.CodeDocs;
using DocsMaster.Engine.Models.CodeElements.TypeKind;
using System.Collections.Generic;
using System.Linq;

namespace DocsMaster.Engine.Models.CodeElements.Types
{
    internal class CodeType : BaseCodeType, IParentType
    {
        public List<CodeElement> Members { get; set; }

        public CodeType(CodeElementType type, BaseCodeDeclarationKind declaration, CodeNamespace namespaceReference, IParentType? parent = null, string? accessModifier = null, CodeDocumentation? documentation = null, List<CodeElement>? members = null) :
            base(type, declaration, namespaceReference, parent, accessModifier, documentation)
        {
            Members = members ?? new List<CodeElement>();
        }

        public void AddInternalElement(CodeElement element)
        {
            Members.Add(element);
        }

        public CodeElement GetElement()
        {
            return this;
        }

        public IParentType? GetParent()
        {
            return Parent;
        }

        public bool ContainsElement(CodeElement codeElement) => Members.Contains(codeElement);

        public CodeElementType GetElementType() => Type;

        public CodeElement? GetChild(CodeElementType elementType, string name) => Members.Where(x => x.Type == elementType && x.Declaration.GetName() == name).FirstOrDefault();
    }
}
