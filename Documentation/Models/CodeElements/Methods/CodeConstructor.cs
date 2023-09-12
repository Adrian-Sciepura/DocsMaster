using Documentation.Common;
using Documentation.Models.CodeElements.Documentation;
using Documentation.Models.CodeElements.TypeKind;
using Documentation.Models.CodeElements.Types;
using Documentation.Models.CodeElements.Variables;
using Microsoft.VisualStudio.Package;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Documentation.Models.CodeElements.Methods
{
    internal class CodeConstructor : BaseCodeMethod
    {
        public List<CodeVariable> Parameters { get; set; }

        public CodeConstructor(CodeRegularDeclaration declaration, CodeNamespace namespaceReference, IParentType parent, string accessModifier, List<CodeVariable>? parameters = null, CodeDocumentation documentation = null) :
            base(CodeElementType.Constructor, declaration, namespaceReference, parent, accessModifier, documentation)
        {
            Parameters = parameters ?? new List<CodeVariable>();
        }
    }
}
