using Documentation.Models.CodeElements.Documentation;
using Documentation.Models.CodeElements.TypeKind;
using Documentation.Models.CodeElements.Variables;
using Microsoft.VisualStudio.Package;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Documentation.Models.CodeElements.Types
{
    internal class CodeDelegate : BaseCodeType
    {
        public List<CodeVariable> Parameters { get; set; }
        public string ReturnType { get; set; }

        public CodeDelegate(IParentType parent, BaseCodeDeclarationKind declaration, string accessModifier, string returnType, List<CodeVariable>? parameters = null, CodeDocumentation documentation = null) :
            base(parent, CodeElementType.Delegate, declaration, accessModifier, documentation)
        {
            ReturnType = returnType;
            Parameters = parameters ?? new List<CodeVariable>();
        }
    }
}
