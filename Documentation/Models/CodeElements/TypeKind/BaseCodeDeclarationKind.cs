using Documentation.FormatBuilders;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Text;
using System.Xml.Linq;

namespace Documentation.Models.CodeElements.TypeKind
{
    internal abstract class BaseCodeDeclarationKind
    {
        protected BaseCodeDeclarationKind() 
        {
        }

        public abstract string GetName();
        public abstract string? GetFullName();
        public abstract string? GetHash();
        public abstract void AddReference(CodeElement codeElement);
    }
}
