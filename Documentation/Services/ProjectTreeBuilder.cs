using Documentation.Models;
using Documentation.Models.CodeElements;
using Documentation.Models.CodeElements.Documentation;
using Documentation.Models.CodeElements.Methods;
using Documentation.Models.CodeElements.TypeKind;
using Documentation.Models.CodeElements.Types;
using Documentation.Models.CodeElements.Variables;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Documentation.Services
{
    internal static class ProjectTreeBuilder
    {
        private record SyntaxActionStaticParams(DocsInfo docsInfo, ProjectStructureTree projetStructureTree, ReferenceBuilder references);
        private record SyntaxActionParams(SemanticModel semanticModel, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentReference, CodeDocumentation documentation);
        private delegate void SyntaxElementAction(SyntaxActionStaticParams sp, SyntaxActionParams p);

        private static readonly Dictionary<SyntaxKind, SyntaxElementAction> syntaxElementFunctions = new Dictionary<SyntaxKind, SyntaxElementAction>()
            {
                // Namespaces
                { SyntaxKind.NamespaceDeclaration, AnalyzeNamespaceDeclaration },
                { SyntaxKind.FileScopedNamespaceDeclaration, AnalyzeNamespaceDeclaration },

                // Types
                { SyntaxKind.DelegateDeclaration, AnalyzeDelegateDeclaration },
                { SyntaxKind.ClassDeclaration, AnalyzeClassDeclaration },
                { SyntaxKind.StructDeclaration, AnalyzeStructDeclaration },
                { SyntaxKind.InterfaceDeclaration, AnalyzeInterfaceDeclaration },
                { SyntaxKind.RecordDeclaration, AnalyzeRecordDeclaration },
                { SyntaxKind.EnumDeclaration, AnalyzeEnumDeclaration },

                // ------Type Members------
                // Type Method Members
                { SyntaxKind.MethodDeclaration, AnalyzeMethodDeclaration },
                { SyntaxKind.ConstructorDeclaration, AnalyzeContructorDeclaration },
                { SyntaxKind.DestructorDeclaration, AnalyzeDestructorDeclaration },
                { SyntaxKind.OperatorDeclaration, AnalyzeOperatorDeclaration },
                { SyntaxKind.ConversionOperatorDeclaration, AnalyzeConversionOperatorDeclaration },

                // Type Variable and Property Members
                { SyntaxKind.FieldDeclaration, AnalyzeFieldDeclaration },
                { SyntaxKind.PropertyDeclaration, AnalyzePropertyDeclaration }
            };


        #region Help Functions
        private static CSharpCompilation GetCompilation(List<SyntaxTree> trees)
        {
            return CSharpCompilation.Create("TestCompilation").AddSyntaxTrees(trees);
        }

        private static List<SyntaxTree> SetupSyntaxTrees(DocsInfo docsInfo)
        {
            List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();
            foreach (var proj in docsInfo.SolutionProjects)
            {
                if (docsInfo.Configuration.ExcludedProjects.Contains(proj.Name))
                    continue;

                foreach (var projChild in proj.Children)
                    GetChilds(projChild);
            }




            void GetChilds(SolutionItem item)
            {
                switch (item.Type)
                {
                    case SolutionItemType.PhysicalFile:
                        if (Path.GetExtension(item.Name) == ".cs")
                            syntaxTrees.Add(CSharpSyntaxTree.ParseText(File.ReadAllText(item.Name)));
                        break;
                    case SolutionItemType.PhysicalFolder:
                    case SolutionItemType.VirtualFolder:
                        foreach (var child in item.Children)
                            GetChilds(child);
                        break;
                }
            }

            return syntaxTrees;
        }

        private static ProjectStructureTreeNode GetCurrentNode(DocsInfo docsInfo, ProjectStructureTree tree, string name)
        {
            ProjectStructureTreeNode result;
            switch (docsInfo.Configuration.CreateDocsBy)
            {
                case DocsConfiguration.DocumentationType.Namespaces:
                    result = tree.GetNodeByFullName(name);
                    break;
                case DocsConfiguration.DocumentationType.Projects:
                    result = tree.GetNodeByFullName(name);
                    break;
                default:
                    throw new ArgumentException("Unknown type");
            }

            return result;
        }

        private static async Task AnalyzeSyntaxAsync(SyntaxActionStaticParams sp, SyntaxTree syntaxTree, CSharpCompilation compilation)
        {
            var syntax = (await syntaxTree.GetRootAsync()).DescendantNodes();
            SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
            foreach (var codeElement in syntax)
            {
                var kind = codeElement.Kind();
                if (kind == SyntaxKind.FileScopedNamespaceDeclaration || kind == SyntaxKind.NamespaceDeclaration)
                    AnalyzeNamespaceDeclaration(sp, new SyntaxActionParams(semanticModel, null, codeElement, null, null));
            }
        }

        private static void AnalyzeMembers(SyntaxActionStaticParams sp, SemanticModel semanticModel, ProjectStructureTreeNode currentNode, IParentType parentReference, SyntaxList<MemberDeclarationSyntax> members)
        {
            SyntaxElementAction action;

            foreach (var member in members)
            {
                if (syntaxElementFunctions.TryGetValue(member.Kind(), out action))
                {
                    CodeDocumentation? codeDocs = AnalyzeDocumentation(sp.references, semanticModel, member);

                    if (codeDocs != null && codeDocs.Skip)
                        continue;

                    action.Invoke(sp, new SyntaxActionParams(semanticModel, currentNode, member, parentReference, codeDocs));
                }
            }

        }

        #endregion


        #region Analyze Namespace

        private static void AnalyzeNamespaceDeclaration(SyntaxActionStaticParams sp, SyntaxActionParams p)
        {
            BaseNamespaceDeclarationSyntax baseNamespaceDeclaration = p.codeElement as BaseNamespaceDeclarationSyntax;

            string namespaceName = baseNamespaceDeclaration.Name.ToString();
            ProjectStructureTreeNode changeNode = GetCurrentNode(sp.docsInfo, sp.projetStructureTree, namespaceName);

            if (changeNode.NamespaceReference == null)
                changeNode.NamespaceReference = new CodeNamespace(namespaceName);

            AnalyzeMembers(sp, p.semanticModel, changeNode, changeNode.NamespaceReference, baseNamespaceDeclaration.Members);
            changeNode.NamespaceReference.InternalTypes.Sort();
        }

        #endregion

        #region Analyze Types

        private static void AnalyzeDelegateDeclaration(SyntaxActionStaticParams sp, SyntaxActionParams p)
        {
            DelegateDeclarationSyntax delegateDeclaration = p.codeElement as DelegateDeclarationSyntax;

            CodeDelegate codeDelegate = new CodeDelegate(
                parent: p.parentReference,
                namespaceReference: p.currentNode.NamespaceReference,
                declaration: sp.references.GetTypeDeclaration(p.codeElement, p.semanticModel, delegateDeclaration.Identifier.ValueText, delegateDeclaration.TypeParameterList?.Parameters),
                accessModifier: delegateDeclaration.Modifiers.ToString(),
                returnType: sp.references.GetVariableDeclaration(delegateDeclaration.ReturnType, p.semanticModel),
                parameters: delegateDeclaration.ParameterList.Parameters.Select(x => new CodeVariable(sp.references.GetVariableDeclaration(x.Type, p.semanticModel), p.currentNode.NamespaceReference, null, x.Identifier.ValueText)).ToList(),
                documentation: p.documentation);

            codeDelegate.Declaration.AddReference(codeDelegate);
            p.parentReference.AddInternalElement(codeDelegate);
            sp.references.AddReference(codeDelegate);
        }

        private static void AnalyzeClassDeclaration(SyntaxActionStaticParams sp, SyntaxActionParams p)
        {
            ClassDeclarationSyntax classDeclaration = p.codeElement as ClassDeclarationSyntax;
            BaseCodeDeclarationKind declaration = sp.references.GetTypeDeclaration(p.codeElement, p.semanticModel, classDeclaration.Identifier.ValueText, classDeclaration.TypeParameterList?.Parameters);

            if (classDeclaration.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)) && p.parentReference != null)
            {
                CodeType partialClass = (CodeType)p.parentReference.GetChild(CodeElementType.Class, declaration.GetName());
                
                if(partialClass != null)
                {
                    AnalyzeMembers(sp, p.semanticModel, p.currentNode, partialClass, classDeclaration.Members);
                    partialClass.Members.Sort();
                    return;
                }
            }

            CodeType codeClass = new CodeType(
                parent: p.parentReference,
                namespaceReference: p.currentNode.NamespaceReference,
                type: CodeElementType.Class,
                declaration: declaration,
                accessModifier: classDeclaration.Modifiers.ToString(),
                documentation: p.documentation);


            codeClass.Declaration.AddReference(codeClass);
            p.parentReference.AddInternalElement(codeClass);
            AnalyzeMembers(sp, p.semanticModel, p.currentNode, codeClass, classDeclaration.Members);
            codeClass.Members.Sort();
            sp.references.AddReference(codeClass);
        }

        private static void AnalyzeRecordDeclaration(SyntaxActionStaticParams sp, SyntaxActionParams p)
        {
            RecordDeclarationSyntax recordDeclaration = p.codeElement as RecordDeclarationSyntax;

            CodeType codeRecord = new CodeType(
                parent: p.parentReference,
                namespaceReference: p.currentNode.NamespaceReference,
                type: CodeElementType.Record,
                declaration: sp.references.GetTypeDeclaration(p.codeElement, p.semanticModel, recordDeclaration.Identifier.ValueText, recordDeclaration.TypeParameterList?.Parameters),
                accessModifier: recordDeclaration.Modifiers.ToString(),
                documentation: p.documentation);


            if (recordDeclaration.ParameterList != null)
                foreach (var variable in recordDeclaration.ParameterList.Parameters)
                    codeRecord.Members.Add(new CodeVariable(sp.references.GetVariableDeclaration(variable.Type, p.semanticModel), p.currentNode.NamespaceReference, null, variable.Identifier.ValueText));


            codeRecord.Declaration.AddReference(codeRecord);
            p.parentReference.AddInternalElement(codeRecord);
            AnalyzeMembers(sp, p.semanticModel, p.currentNode, codeRecord, recordDeclaration.Members);
            codeRecord.Members.Sort();
            sp.references.AddReference(codeRecord);
        }

        private static void AnalyzeInterfaceDeclaration(SyntaxActionStaticParams sp, SyntaxActionParams p)
        {
            InterfaceDeclarationSyntax interfaceDeclaration = p.codeElement as InterfaceDeclarationSyntax;

            CodeType codeInterface = new CodeType(
                parent: p.parentReference,
                namespaceReference: p.currentNode.NamespaceReference,
                type: CodeElementType.Interface,
                declaration: sp.references.GetTypeDeclaration(p.codeElement, p.semanticModel, interfaceDeclaration.Identifier.ValueText, interfaceDeclaration.TypeParameterList?.Parameters),
                accessModifier: interfaceDeclaration.Modifiers.ToString(),
                documentation: p.documentation);


            codeInterface.Declaration.AddReference(codeInterface);
            p.parentReference.AddInternalElement(codeInterface);
            AnalyzeMembers(sp, p.semanticModel, p.currentNode, codeInterface, interfaceDeclaration.Members);
            codeInterface.Members.Sort();
            sp.references.AddReference(codeInterface);
        }

        private static void AnalyzeStructDeclaration(SyntaxActionStaticParams sp, SyntaxActionParams p)
        {
            StructDeclarationSyntax structDeclaration = p.codeElement as StructDeclarationSyntax;

            CodeType codeStruct = new CodeType(
                parent: p.parentReference,
                namespaceReference: p.currentNode.NamespaceReference,
                type: CodeElementType.Struct,
                declaration: sp.references.GetTypeDeclaration(p.codeElement, p.semanticModel, structDeclaration.Identifier.ValueText, structDeclaration.TypeParameterList?.Parameters),
                accessModifier: structDeclaration.Modifiers.ToString(),
                documentation: p.documentation);


            codeStruct.Declaration.AddReference(codeStruct);
            p.parentReference.AddInternalElement(codeStruct);
            AnalyzeMembers(sp, p.semanticModel, p.currentNode, codeStruct, structDeclaration.Members);
            codeStruct.Members.Sort();
            sp.references.AddReference(codeStruct);
        }

        private static void AnalyzeEnumDeclaration(SyntaxActionStaticParams sp, SyntaxActionParams p)
        {
            EnumDeclarationSyntax enumDeclaration = p.codeElement as EnumDeclarationSyntax;

            CodeEnum codeEnum = new CodeEnum(
                parent: p.parentReference,
                namespaceReference: p.currentNode.NamespaceReference,
                declaration: (CodeRegularDeclaration)sp.references.GetTypeDeclaration(p.codeElement, p.semanticModel, enumDeclaration.Identifier.ValueText, null),
                accessModifier: enumDeclaration.Modifiers.ToString(),
                elements: new List<string>(enumDeclaration.Members.Select(x => x.Identifier.ValueText)),
                documentation: p.documentation);


            codeEnum.Declaration.AddReference(codeEnum);
            p.parentReference.AddInternalElement(codeEnum);
            sp.references.AddReference(codeEnum);
        }

        #endregion

        #region Analyze Type Members

        #region Documentation Comments

        private static CodeDocumentation? AnalyzeDocumentation(ReferenceBuilder references, SemanticModel semanticModel, SyntaxNode node)
        {
            DocumentationCommentTriviaSyntax documentationComment = node.GetLeadingTrivia()
                .Select(trivia => trivia.GetStructure())
                .OfType<DocumentationCommentTriviaSyntax>()
                .FirstOrDefault();

            if (documentationComment == null)
                return null;


            var allChildNodes = documentationComment.ChildNodes();


            if (allChildNodes.OfType<XmlEmptyElementSyntax>().Where(x => x.Name.ToString() == "skip").Count() != 0)
                return new CodeDocumentation() { Skip = true };


            var childNodes = allChildNodes.OfType<XmlElementSyntax>();
            return childNodes.Count() > 0 ? CodeDocumentationBuilder.BuildDocumentation(childNodes, semanticModel, references) : null;
        }

        #endregion

        #region Method members

        private static void AnalyzeMethodDeclaration(SyntaxActionStaticParams sp, SyntaxActionParams p)
        {
            MethodDeclarationSyntax methodDeclaration = p.codeElement as MethodDeclarationSyntax;

            CodeMethod codeMethod = new CodeMethod(
                parent: p.parentReference,
                namespaceReference: p.currentNode.NamespaceReference,
                declaration: sp.references.GetMethodDeclaration(methodDeclaration, methodDeclaration.Identifier.ValueText, p.semanticModel, methodDeclaration.TypeParameterList?.Parameters),
                accessModifier: methodDeclaration.Modifiers.ToString(),
                returnType: sp.references.GetVariableDeclaration(methodDeclaration.ReturnType, p.semanticModel),
                parameters: methodDeclaration.ParameterList.Parameters.Select(x => new CodeVariable(sp.references.GetVariableDeclaration(x.Type, p.semanticModel), p.currentNode.NamespaceReference, null, x.Identifier.ValueText)).ToList(),
                documentation: p.documentation);

            p.parentReference.AddInternalElement(codeMethod);
        }

        private static void AnalyzeContructorDeclaration(SyntaxActionStaticParams sp, SyntaxActionParams p)
        {
            ConstructorDeclarationSyntax constructorDeclaration = p.codeElement as ConstructorDeclarationSyntax;

            CodeConstructor constructorMethod = new CodeConstructor(
                parent: p.parentReference,
                namespaceReference: p.currentNode.NamespaceReference,
                declaration: (CodeRegularDeclaration)sp.references.GetMethodDeclaration(constructorDeclaration, constructorDeclaration.Identifier.ValueText, p.semanticModel, null),
                accessModifier: constructorDeclaration.Modifiers.ToString(),
                parameters: constructorDeclaration.ParameterList.Parameters.Select(x => new CodeVariable(sp.references.GetVariableDeclaration(x.Type, p.semanticModel), p.currentNode.NamespaceReference, null, x.Identifier.ValueText)).ToList(),
                documentation: p.documentation);

            p.parentReference.AddInternalElement(constructorMethod);
        }

        private static void AnalyzeDestructorDeclaration(SyntaxActionStaticParams sp, SyntaxActionParams p)
        {
            DestructorDeclarationSyntax destructorDeclaration = p.codeElement as DestructorDeclarationSyntax;

            CodeDestructor destructorMethod = new CodeDestructor(
                parent: p.parentReference,
                namespaceReference: p.currentNode.NamespaceReference,
                declaration: (CodeRegularDeclaration)sp.references.GetMethodDeclaration(destructorDeclaration, destructorDeclaration.Identifier.ValueText, p.semanticModel, null),
                documentation: p.documentation);

            p.parentReference.AddInternalElement(destructorMethod);
        }

        private static void AnalyzeOperatorDeclaration(SyntaxActionStaticParams sp, SyntaxActionParams p)
        {
            OperatorDeclarationSyntax operatorDeclaration = p.codeElement as OperatorDeclarationSyntax;

            CodeOperator operatorMethod = new CodeOperator(
                parent: p.parentReference,
                namespaceReference: p.currentNode.NamespaceReference,
                declaration: (CodeRegularDeclaration)sp.references.GetMethodDeclaration(operatorDeclaration, operatorDeclaration.OperatorToken.ValueText, p.semanticModel, null),
                accessModifier: operatorDeclaration.Modifiers.ToString(),
                returnType: sp.references.GetVariableDeclaration(operatorDeclaration.ReturnType, p.semanticModel),
                parameters: operatorDeclaration.ParameterList.Parameters.Select(x => new CodeVariable(sp.references.GetVariableDeclaration(x.Type, p.semanticModel), p.currentNode.NamespaceReference, null, x.Identifier.ValueText)).ToList(),
                documentation: p.documentation);

            p.parentReference.AddInternalElement(operatorMethod);
        }

        private static void AnalyzeConversionOperatorDeclaration(SyntaxActionStaticParams sp, SyntaxActionParams p)
        {
            ConversionOperatorDeclarationSyntax conversionOperatorDeclaration = p.codeElement as ConversionOperatorDeclarationSyntax;

            CodeOperator conversionOperatorMethod = new CodeOperator(
                parent: p.parentReference,
                namespaceReference: p.currentNode.NamespaceReference,
                declaration: (CodeRegularDeclaration)sp.references.GetMethodDeclaration(conversionOperatorDeclaration, conversionOperatorDeclaration.Type.ToString(), p.semanticModel, null),
                accessModifier: conversionOperatorDeclaration.Modifiers.ToString(),
                returnType: new CodeRegularDeclaration(string.Empty, null),
                parameters: conversionOperatorDeclaration.ParameterList.Parameters.Select(x => new CodeVariable(sp.references.GetVariableDeclaration(x.Type, p.semanticModel), p.currentNode.NamespaceReference, null, x.Identifier.ValueText)).ToList(),
                documentation: p.documentation);

            p.parentReference.AddInternalElement(conversionOperatorMethod);
        }

        #endregion

        #region Variable members
        private static void AnalyzeFieldDeclaration(SyntaxActionStaticParams sp, SyntaxActionParams p)
        {
            FieldDeclarationSyntax fieldDeclaration = p.codeElement as FieldDeclarationSyntax;

            CodeVariable codeVariable = new CodeVariable(
                parent: p.parentReference,
                namespaceReference: p.currentNode.NamespaceReference,
                declaration: sp.references.GetVariableDeclaration(fieldDeclaration.Declaration.Type, p.semanticModel),
                accessModifier: fieldDeclaration.Modifiers.ToString(),
                fieldName: string.Join(", ", fieldDeclaration.Declaration.Variables.Select(x => x.Identifier)),
                documentation: p.documentation);

            p.parentReference.AddInternalElement(codeVariable);
        }

        private static void AnalyzePropertyDeclaration(SyntaxActionStaticParams sp, SyntaxActionParams p)
        {
            PropertyDeclarationSyntax propertyDeclaration = p.codeElement as PropertyDeclarationSyntax;

            CodeProperty codeProperty = new CodeProperty(
                parent: p.parentReference,
                namespaceReference: p.currentNode.NamespaceReference,
                declaration: sp.references.GetVariableDeclaration(propertyDeclaration.Type, p.semanticModel),
                accessModifier: propertyDeclaration.Modifiers.ToString(),
                name: propertyDeclaration.Identifier.ValueText,
                accessors: propertyDeclaration.AccessorList?.Accessors.Select(x => x.Keyword.ValueText).ToList(),
                documentation: p.documentation);

            p.parentReference.AddInternalElement(codeProperty);
        }

        #endregion

        #endregion

        public static async Task<ProjectStructureTree> BuildProjectTreeAsync(DocsInfo docsInfo)
        {
            ReferenceBuilder projectTreeReference = new ReferenceBuilder();
            ProjectStructureTree projectStructureTree = new ProjectStructureTree();
            List<SyntaxTree> syntaxTrees = SetupSyntaxTrees(docsInfo);
            var compilation = GetCompilation(syntaxTrees);


            foreach (var tree in syntaxTrees)
                await AnalyzeSyntaxAsync(new SyntaxActionStaticParams(docsInfo, projectStructureTree, projectTreeReference), tree, compilation);


            projectTreeReference.AddReferencesToTreeElements();
            return projectStructureTree;
        }
    }
}
