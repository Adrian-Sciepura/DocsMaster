using Documentation.Models.CodeElements.Types;
using System.Text;
using System.Xml.Linq;

namespace Documentation.Models.CodeElements.TypeKind
{
    internal class CodeRegularDeclaration : BaseCodeDeclarationKind
    {
        public string Name { get; set; }
        public string? FullName { get; set; }
        public string? FullNameHash { get; private set; }
        public CodeElement TypeReference { get; set; }
        
        public CodeRegularDeclaration(string name, string? fullName)
        {
            Name = name;
            FullName = fullName;
            FullNameHash = FullName != null ? $"{FullName.GetHashCode():X8}" : null;
        }

        public override string GetName()
        {
            return Name;
        }

        public override string? GetFullName()
        {
            return FullName;
        }

        public override string? GetHash()
        {
            return FullNameHash;
        }

        public override void AddReference(CodeElement codeElement)
        {
            TypeReference = codeElement;
        }
    }
}
