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
    internal class CodeOperator : BaseCodeMethod
    {
        public List<CodeVariable> Parameters { get; set; }
        public BaseCodeDeclarationKind ReturnType { get; set; }

        public CodeOperator(CodeRegularDeclaration declaration, CodeNamespace namespaceReference, IParentType parent, string accessModifier, BaseCodeDeclarationKind returnType, List<CodeVariable>? parameters = null, CodeDocumentation documentation = null) :
            base(CodeElementType.Operator, declaration, namespaceReference, parent, accessModifier, documentation)
        {
            ReturnType = returnType;
            Parameters = parameters ?? new List<CodeVariable>();
        }
    }
}
