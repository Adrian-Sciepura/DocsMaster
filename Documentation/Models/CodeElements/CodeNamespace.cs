using Documentation.Models.CodeElements.TypeKind;
using Documentation.Models.CodeElements.Types;
using System.Collections.Generic;
using System.Linq;

namespace Documentation.Models.CodeElements
{
    internal sealed class CodeNamespace : CodeElement, IParentType
    {
        public List<BaseCodeType> InternalTypes { get; set; }

        public CodeNamespace(string name, List<BaseCodeType>? internalTypes = null) :
            base(CodeElementType.Namespace, new CodeRegularDeclaration(name, name))
        {
            InternalTypes = internalTypes ?? new List<BaseCodeType>();
        }

        public void AddInternalElement(CodeElement element)
        {
            if (element is BaseCodeType internalType)
                InternalTypes.Add(internalType);
        }

        public CodeElement GetElement()
        {
            return this;
        }

        public IParentType GetParent()
        {
            return null;
        }

        public bool ContainsElement(CodeElement codeElement) => (codeElement is BaseCodeType internalType && InternalTypes.Contains(internalType));

        public CodeElementType GetElementType() => CodeElementType.Namespace;

        public CodeElement? GetChild(CodeElementType elementType, string name) => InternalTypes.Where(x => x.Type == elementType && x.Declaration.GetName() == name).FirstOrDefault();
    }
}
