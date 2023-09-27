using Documentation.Engine.Models.CodeDocs;
using Documentation.Engine.Models.CodeElements.TypeKind;
using System.Collections.Generic;

namespace Documentation.Engine.Models.CodeElements.Variables
{
    internal class CodeField : CodeElement
    {
        public List<string> VariableNames { get; set; }

        protected CodeField(CodeElementType type, BaseCodeDeclarationKind declaration, List<string> variableNames, CodeNamespace namespaceReference, IParentType? parent = null, string? accessModifier = null, CodeDocumentation? documentation = null) :
            base(type, declaration, namespaceReference, parent, accessModifier, documentation)
        {
            VariableNames = variableNames;
        }

        public CodeField(BaseCodeDeclarationKind declaration, CodeNamespace namespaceReference, IParentType? parent, List<string> variableNames, string? accessModifier = null, CodeDocumentation? documentation = null) :
            base(CodeElementType.Variable, declaration, namespaceReference, parent, accessModifier, documentation)
        {
            VariableNames = variableNames;
        }

        // Only for fields with single variable declaration
        public CodeField(BaseCodeDeclarationKind declaration, CodeNamespace namespaceReference, IParentType? parent, string variableName, string? accessModifier = null, CodeDocumentation? documentation = null) :
            base(CodeElementType.Variable, declaration, namespaceReference, parent, accessModifier, documentation)
        {
            VariableNames = new List<string>() { variableName };
        }
    }
}
