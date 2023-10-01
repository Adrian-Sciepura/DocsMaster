using DocsMaster.Engine.Models.CodeElements;
using DocsMaster.Engine.Models.CodeElements.TypeKind;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DocsMaster.Engine.ProjectTree
{
    internal class ReferenceBuilder
    {
        private Queue<CodeRegularDeclaration> DeclarationsToAddReference;
        private Dictionary<string, CodeElement> References;

        public ReferenceBuilder()
        {
            References = new Dictionary<string, CodeElement>();
            DeclarationsToAddReference = new Queue<CodeRegularDeclaration>();
        }

        public string DeclarationFullNameScraper(string fullName, int numberOfGenericParameters)
        {
            for (int i = 0; i < fullName.Length; i++)
            {
                switch (fullName[i])
                {
                    case '<':
                    case '[':
                    case '*':
                        if (numberOfGenericParameters > 0)
                            return $"{fullName.Substring(0, i)}<{numberOfGenericParameters}>";
                        else
                            return fullName.Substring(0, i);
                }
            }

            return fullName;
        }

        public void AddReference(CodeElement codeElementReference)
        {
            string? key = codeElementReference.Declaration.GetFullName();

            if (key == null)
                return;

            if (!References.TryGetValue(key, out _))
                References.Add(key, codeElementReference);
        }

        public void AddReferencesToTreeElements()
        {
            CodeElement reference;

            foreach (var element in DeclarationsToAddReference)
                if (element.FullName != null && References.TryGetValue(element.FullName, out reference))
                    element.TypeReference = reference;

            DeclarationsToAddReference.Clear();
        }

        public void AddDeclarationToQueue(CodeRegularDeclaration declaration) => DeclarationsToAddReference.Enqueue(declaration);

        public void AddDeclarationToQueue(BaseCodeDeclarationKind declaration) => DeclarationsToAddReference.Enqueue(declaration is CodeGenericDeclaration genericDeclaration ? genericDeclaration.MainType : declaration as CodeRegularDeclaration);

        public BaseCodeDeclarationKind GetMethodDeclaration(BaseMethodDeclarationSyntax methodDeclaration, string name, SemanticModel semanticModel, SeparatedSyntaxList<TypeParameterSyntax>? genericParameters)
        {
            var symbol = (IMethodSymbol)semanticModel.GetDeclaredSymbol(methodDeclaration);
            var containingTypeInfo = symbol.ContainingType;

            StringBuilder sb = new StringBuilder();
            sb.Append(DeclarationFullNameScraper(containingTypeInfo.ToDisplayString(), containingTypeInfo.TypeParameters.Count()));
            sb.Append('.').Append(DeclarationFullNameScraper(name, symbol.TypeParameters.Count()));
            sb.Append($"({symbol.Parameters.Count()})");

            if (genericParameters == null || genericParameters?.Count == 0)
                return new CodeRegularDeclaration(symbol.Name, sb.ToString());

            List<BaseCodeDeclarationKind> subTypes = genericParameters?.Select(x => new CodeRegularDeclaration(x.Identifier.ValueText, null)).ToList<BaseCodeDeclarationKind>();
            CodeGenericDeclaration genericMethodDeclaration = new CodeGenericDeclaration(subTypes, new CodeRegularDeclaration(name, sb.ToString()));

            return genericMethodDeclaration;
        }

        public BaseCodeDeclarationKind GetTypeDeclaration(SyntaxNode syntaxNode, SemanticModel semanticModel, string name, SeparatedSyntaxList<TypeParameterSyntax>? genericParameters)
        {
            string? displayString = semanticModel.GetDeclaredSymbol(syntaxNode)?.ToDisplayString();
            string? fullName = displayString == null ? null : DeclarationFullNameScraper(displayString, genericParameters?.Count ?? 0);

            if (genericParameters == null || genericParameters?.Count == 0)
                return new CodeRegularDeclaration(name, fullName);

            List<BaseCodeDeclarationKind> subTypes = genericParameters?.Select(x => new CodeRegularDeclaration(x.Identifier.ValueText, null)).ToList<BaseCodeDeclarationKind>();
            CodeGenericDeclaration genericTypeDeclaration = new CodeGenericDeclaration(subTypes, new CodeRegularDeclaration(name, fullName));

            return genericTypeDeclaration;
        }

        public BaseCodeDeclarationKind GetTypeDeclaration(ISymbol symbol, SemanticModel semanticModel)
        {
            string name = symbol.Name;
            string rawFullName = symbol.ToString();
            string fullName;

            if (symbol is INamedTypeSymbol namedType && namedType.TypeParameters.Length > 0)
            {
                fullName = DeclarationFullNameScraper(rawFullName, namedType.TypeParameters.Length);

                List<BaseCodeDeclarationKind> subTypes = namedType.TypeParameters.Select(x => new CodeRegularDeclaration(x.Name, null)).ToList<BaseCodeDeclarationKind>();
                return new CodeGenericDeclaration(subTypes, new CodeRegularDeclaration(name, fullName));
            }

            return new CodeRegularDeclaration(name, rawFullName);
        }

        public BaseCodeDeclarationKind GetVariableDeclaration(TypeSyntax typeSyntax, SemanticModel semanticModel)
        {
            BaseCodeDeclarationKind result = null;

            if (typeSyntax is QualifiedNameSyntax qualifiedNameSyntax)
                typeSyntax = qualifiedNameSyntax.Right;


            string fullName = semanticModel.GetTypeInfo(typeSyntax).Type?.ToDisplayString();
            if (fullName != null) fullName = DeclarationFullNameScraper(fullName, 0);

            if (typeSyntax is GenericNameSyntax genericTypeSyntax)
            {
                List<BaseCodeDeclarationKind> subTypes =
                    genericTypeSyntax.TypeArgumentList.Arguments.Select(x => GetVariableDeclaration(x, semanticModel)).ToList();

                if (fullName != null) fullName += $"<{subTypes.Count}>";

                var mainType = new CodeRegularDeclaration(genericTypeSyntax.Identifier.ValueText, fullName);
                result = new CodeGenericDeclaration(subTypes, mainType);
                DeclarationsToAddReference.Enqueue(mainType);

            }
            else
            {
                var temp = new CodeRegularDeclaration(typeSyntax.ToString(), fullName);
                result = temp;
                DeclarationsToAddReference.Enqueue(temp);
            }


            return result;
        }
    }
}
