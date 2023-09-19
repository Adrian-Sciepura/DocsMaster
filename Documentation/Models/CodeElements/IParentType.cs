using System.Collections.Generic;
using System.Windows.Documents;

namespace Documentation.Models.CodeElements
{
    internal interface IParentType
    {
        void AddInternalElement(CodeElement element);
        CodeElement GetElement();
        CodeElementType GetElementType();
        IParentType? GetParent();
        bool ContainsElement(CodeElement codeElement);
    }
}
