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
using System.Reflection;
using System.Threading.Tasks;

namespace Documentation.Services
{
    internal static class ProjectTreeBuilder
    {
        private static readonly Dictionary<SyntaxKind, Action<DocsInfo, ProjectStructureTree, ProjectStructureTreeNode, SyntaxNode, IParentType>> syntaxElementFunctions =
            new Dictionary<SyntaxKind, Action<DocsInfo, ProjectStructureTree, ProjectStructureTreeNode, SyntaxNode, IParentType>>()
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

                //------Type Members------
                //Type Method Members
                { SyntaxKind.MethodDeclaration, AnalyzeMethodDeclaration },
                { SyntaxKind.ConstructorDeclaration, AnalyzeContructorDeclaration },
                { SyntaxKind.DestructorDeclaration, AnalyzeDestructorDeclaration },
                { SyntaxKind.OperatorDeclaration, AnalyzeOperatorDeclaration },
                { SyntaxKind.ConversionOperatorDeclaration, AnalyzeConversionOperatorDeclaration },

                //Type Variable and Property Members
                { SyntaxKind.FieldDeclaration, AnalyzeFieldDeclaration },
                { SyntaxKind.PropertyDeclaration, AnalyzePropertyDeclaration }
            };

        private static readonly Dictionary<string, Tuple<CodeNamespace, BaseCodeType>> declaredTypesInfo = new Dictionary<string, Tuple<CodeNamespace, BaseCodeType>>();
        private static ProjectTreeReferences references;
        private static SemanticModel csharpSemanticModel;


        #region Main Functions
        private static CSharpCompilation GetCompilation(List<SyntaxTree> trees)
        {
            /*string outputPath = await project.GetAttributeAsync("OutputPath");
            string assemblyName = await project.GetAttributeAsync("AssemblyName");
            string folderPath = Path.GetDirectoryName(project.FullPath);
            string fullAssemblyPath = Path.Combine(folderPath, outputPath, Path.GetExtension(assemblyName) == ".dll" ? assemblyName : $"{assemblyName}.dll");
            */
            //var assembly = Assembly.LoadFrom(fullAssemblyPath);

            var compilation = CSharpCompilation.Create("TestCompilation").AddSyntaxTrees(trees);
            return compilation;
        }
        
        private static List<SyntaxTree> SetupSyntaxTrees(DocsInfo docsInfo)
        {
            List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();
            foreach (var proj in docsInfo.SolutionProjects)
                foreach (var projChild in proj.Children)
                    GetChilds(projChild);


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

        private static async Task AnalyzeSyntaxAsync(DocsInfo docsInfo, ProjectStructureTree tree, SyntaxTree syntaxTree, CSharpCompilation compilation)
        {
            var syntax = (await syntaxTree.GetRootAsync()).DescendantNodes();
            csharpSemanticModel = compilation.GetSemanticModel(syntaxTree);
            foreach (var codeElement in syntax)
            {
                var kind = codeElement.Kind();
                if (kind == SyntaxKind.FileScopedNamespaceDeclaration || kind == SyntaxKind.NamespaceDeclaration)
                    AnalyzeNamespaceDeclaration(docsInfo, tree, null, codeElement, null);
            }
        }


        #endregion

        #region Help Functions


        private static void Cleanup()
        {
            declaredTypesInfo.Clear();
            //elementsToAddReference.Clear();
        }


        #endregion



        #region Analyze Namespace

        private static void AnalyzeMembers(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, IParentType parentElement, SyntaxList<MemberDeclarationSyntax> members)
        {
            Action<DocsInfo, ProjectStructureTree, ProjectStructureTreeNode, SyntaxNode, IParentType> action;

            foreach (var member in members)
                if (syntaxElementFunctions.TryGetValue(member.Kind(), out action))
                    action.Invoke(docsInfo, tree, currentNode, member, parentElement);
        }

        private static void AnalyzeNamespaceDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentElement)
        {
            BaseNamespaceDeclarationSyntax baseNamespaceDeclaration = codeElement as BaseNamespaceDeclarationSyntax;

            string namespaceName = baseNamespaceDeclaration.Name.ToString();
            ProjectStructureTreeNode changeNode = GetCurrentNode(docsInfo, tree, namespaceName);

            if (changeNode.NamespaceReference == null)
                changeNode.NamespaceReference = new CodeNamespace(namespaceName);

            AnalyzeMembers(docsInfo, tree, changeNode, changeNode.NamespaceReference, baseNamespaceDeclaration.Members);
            changeNode.NamespaceReference.InternalTypes.Sort();
        }

        #endregion

        #region Analyze Types

        private static void AnalyzeDelegateDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentElement)
        {
            DelegateDeclarationSyntax delegateDeclaration = codeElement as DelegateDeclarationSyntax;

            CodeDelegate codeDelegate = new CodeDelegate(
                parent: parentElement,
                declaration: references.GetTypeDeclaration(codeElement, csharpSemanticModel, delegateDeclaration.Identifier.ValueText, delegateDeclaration.TypeParameterList?.Parameters),
                accessModifier: delegateDeclaration.Modifiers.ToString(),
                returnType: delegateDeclaration.ReturnType.ToString(),
                parameters: delegateDeclaration.ParameterList.Parameters.Select(x => new CodeVariable(null, references.GetVariableDeclaration(x.Type, csharpSemanticModel), x.Identifier.ValueText)).ToList(),
                documentation: AnalyzeDocumentation(codeElement));

            codeDelegate.Declaration.AddReference(codeDelegate);
            parentElement.AddInternalType(codeDelegate);
            references.AddReference(codeDelegate);
            //AddTypeReference(currentNode, codeDelegate);
        }

        private static void AnalyzeClassDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentElement)
        {
            ClassDeclarationSyntax classDeclaration = codeElement as ClassDeclarationSyntax;

            CodeType codeClass = new CodeType(
                parent: parentElement,
                type: CodeElementType.Class,
                declaration: references.GetTypeDeclaration(codeElement, csharpSemanticModel, classDeclaration.Identifier.ValueText, classDeclaration.TypeParameterList?.Parameters),
                //name: classDeclaration.Identifier.ValueText,
                accessModifier: classDeclaration.Modifiers.ToString(),
                documentation: AnalyzeDocumentation(codeElement));

            codeClass.Declaration.AddReference(codeClass);
            parentElement.AddInternalType(codeClass);
            AnalyzeMembers(docsInfo, tree, currentNode, codeClass, classDeclaration.Members);
            codeClass.Members.Sort();
            references.AddReference(codeClass);
            //AddTypeReference(currentNode, codeClass);
        }

        private static void AnalyzeRecordDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentElement)
        {
            RecordDeclarationSyntax recordDeclaration = codeElement as RecordDeclarationSyntax;

            CodeType codeRecord = new CodeType(
                parent: parentElement,
                type: CodeElementType.Record,
                declaration: references.GetTypeDeclaration(codeElement, csharpSemanticModel, recordDeclaration.Identifier.ValueText, recordDeclaration.TypeParameterList?.Parameters),
                //name: recordDeclaration.Identifier.ValueText,
                accessModifier: recordDeclaration.Modifiers.ToString(),
                documentation: AnalyzeDocumentation(codeElement));


            if (recordDeclaration.ParameterList != null)
                foreach (var variable in recordDeclaration.ParameterList.Parameters)
                    codeRecord.Members.Add(new CodeVariable(codeRecord, references.GetVariableDeclaration(variable.Type, csharpSemanticModel), variable.Identifier.ValueText));


            codeRecord.Declaration.AddReference(codeRecord);
            parentElement.AddInternalType(codeRecord);
            AnalyzeMembers(docsInfo, tree, currentNode, codeRecord, recordDeclaration.Members);
            codeRecord.Members.Sort();
            references.AddReference(codeRecord);
            //AddTypeReference(currentNode, codeRecord);
        }

        private static void AnalyzeInterfaceDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentElement)
        {
            InterfaceDeclarationSyntax interfaceDeclaration = codeElement as InterfaceDeclarationSyntax;

            CodeType codeInterface = new CodeType(
                parent: parentElement,
                type: CodeElementType.Interface,
                declaration: references.GetTypeDeclaration(codeElement, csharpSemanticModel, interfaceDeclaration.Identifier.ValueText, interfaceDeclaration.TypeParameterList?.Parameters),
                //name: interfaceDeclaration.Identifier.ValueText,
                accessModifier: interfaceDeclaration.Modifiers.ToString(),
                documentation: AnalyzeDocumentation(codeElement));

            
            codeInterface.Declaration.AddReference(codeInterface);
            parentElement.AddInternalType(codeInterface);
            AnalyzeMembers(docsInfo, tree, currentNode, codeInterface, interfaceDeclaration.Members);
            codeInterface.Members.Sort();
            references.AddReference(codeInterface);
            //AddTypeReference(currentNode, codeInterface);
        }

        private static void AnalyzeStructDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentElement)
        {
            StructDeclarationSyntax structDeclaration = codeElement as StructDeclarationSyntax;

            CodeType codeStruct = new CodeType(
                parent: parentElement,
                type: CodeElementType.Struct,
                declaration: references.GetTypeDeclaration(codeElement, csharpSemanticModel, structDeclaration.Identifier.ValueText, structDeclaration.TypeParameterList?.Parameters),
                //name: structDeclaration.Identifier.ToString(),
                accessModifier: structDeclaration.Modifiers.ToString(),
                documentation: AnalyzeDocumentation(codeElement));


            codeStruct.Declaration.AddReference(codeStruct);
            parentElement.AddInternalType(codeStruct);
            AnalyzeMembers(docsInfo, tree, currentNode, codeStruct, structDeclaration.Members);
            codeStruct.Members.Sort();
            references.AddReference(codeStruct);
            //AddTypeReference(currentNode, codeStruct);
        }

        private static void AnalyzeEnumDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentElement)
        {
            EnumDeclarationSyntax enumDeclaration = codeElement as EnumDeclarationSyntax;

            CodeEnum codeEnum = new CodeEnum(
                parent: parentElement,
                declaration: (CodeRegularDeclaration)references.GetTypeDeclaration(codeElement, csharpSemanticModel, enumDeclaration.Identifier.ValueText, null),
                //name: enumDeclaration.Identifier.ValueText,
                accessModifier: enumDeclaration.Modifiers.ToString(),
                elements: new List<string>(enumDeclaration.Members.Select(x => x.Identifier.ValueText)),
                documentation: AnalyzeDocumentation(codeElement));


            codeEnum.Declaration.AddReference(codeEnum);
            parentElement.AddInternalType(codeEnum);
            references.AddReference(codeEnum);
            //AddTypeReference(currentNode, codeEnum);
        }

        #endregion

        #region Analyze Type Members

        #region Documentation Comments

        private static CodeDocumentation? AnalyzeDocumentation(SyntaxNode node)
        {
            DocumentationCommentTriviaSyntax documentationComment = node.GetLeadingTrivia()
                .Select(trivia => trivia.GetStructure())
                .OfType<DocumentationCommentTriviaSyntax>()
                .FirstOrDefault();

            if (documentationComment == null)
                return null;

            var childNodes = documentationComment.ChildNodes().OfType<XmlElementSyntax>();

            return childNodes.Count() > 0 ? CodeDocumentationBuilder.BuildDocumentation(childNodes, csharpSemanticModel, references) : null;
        }

        #endregion

        #region Method members

        private static void AnalyzeMethodDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentElement)
        {
            MethodDeclarationSyntax methodDeclaration = codeElement as MethodDeclarationSyntax;

            CodeMethod codeMethod = new CodeMethod(
                parent: parentElement,
                declaration: references.GetMethodDeclaration(methodDeclaration, methodDeclaration.Identifier.ValueText, csharpSemanticModel, methodDeclaration.TypeParameterList?.Parameters),
                accessModifier: methodDeclaration.Modifiers.ToString(),
                returnType: references.GetVariableDeclaration(methodDeclaration.ReturnType, csharpSemanticModel),
                parameters: methodDeclaration.ParameterList.Parameters.Select(x => new CodeVariable(null, references.GetVariableDeclaration(x.Type, csharpSemanticModel), x.Identifier.ValueText)).ToList(),
                documentation: AnalyzeDocumentation(codeElement));

            (parentElement as CodeType).Members.Add(codeMethod);
        }

        private static void AnalyzeContructorDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentElement)
        {
            ConstructorDeclarationSyntax constructorDeclaration = codeElement as ConstructorDeclarationSyntax;

            CodeConstructor constructorMethod = new CodeConstructor(
                parent: parentElement,
                declaration: (CodeRegularDeclaration)references.GetMethodDeclaration(constructorDeclaration, constructorDeclaration.Identifier.ValueText, csharpSemanticModel, null),
                //name: constructorDeclaration.Identifier.ValueText,
                accessModifier: constructorDeclaration.Modifiers.ToString(),
                parameters: constructorDeclaration.ParameterList.Parameters.Select(x => new CodeVariable(null, references.GetVariableDeclaration(x.Type, csharpSemanticModel), x.Identifier.ValueText)).ToList(),
                documentation: AnalyzeDocumentation(codeElement));

            (parentElement as CodeType).Members.Add(constructorMethod);
        }

        private static void AnalyzeDestructorDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentElement)
        {
            DestructorDeclarationSyntax destructorDeclaration = codeElement as DestructorDeclarationSyntax;

            CodeDestructor destructorMethod = new CodeDestructor(
                parent: parentElement,
                declaration: (CodeRegularDeclaration)references.GetMethodDeclaration(destructorDeclaration, destructorDeclaration.Identifier.ValueText, csharpSemanticModel, null),
                //name: destructorDeclaration.Identifier.ValueText,
                documentation: AnalyzeDocumentation(codeElement));

            (parentElement as CodeType).Members.Add(destructorMethod);
        }

        private static void AnalyzeOperatorDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentElement)
        {
            OperatorDeclarationSyntax operatorDeclaration = codeElement as OperatorDeclarationSyntax;

            CodeOperator operatorMethod = new CodeOperator(
                parent: parentElement,
                declaration: (CodeRegularDeclaration)references.GetMethodDeclaration(operatorDeclaration, operatorDeclaration.OperatorToken.ValueText, csharpSemanticModel, null),
                //name: operatorDeclaration.OperatorToken.ValueText,
                accessModifier: operatorDeclaration.Modifiers.ToString(),
                returnType: references.GetVariableDeclaration(operatorDeclaration.ReturnType, csharpSemanticModel),
                parameters: operatorDeclaration.ParameterList.Parameters.Select(x => new CodeVariable(null, references.GetVariableDeclaration(x.Type, csharpSemanticModel), x.Identifier.ValueText)).ToList(),
                documentation: AnalyzeDocumentation(codeElement));

            (parentElement as CodeType).Members.Add(operatorMethod);
        }

        private static void AnalyzeConversionOperatorDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentElement)
        {
            ConversionOperatorDeclarationSyntax conversionOperatorDeclaration = codeElement as ConversionOperatorDeclarationSyntax;

            CodeOperator conversionOperatorMethod = new CodeOperator(
                parent: parentElement,
                declaration: (CodeRegularDeclaration)references.GetMethodDeclaration(conversionOperatorDeclaration, conversionOperatorDeclaration.Type.ToString(), csharpSemanticModel, null),
                //name: conversionOperatorDeclaration.Type.ToString(),
                accessModifier: conversionOperatorDeclaration.Modifiers.ToString(),
                returnType: new CodeRegularDeclaration(string.Empty, null),
                parameters: conversionOperatorDeclaration.ParameterList.Parameters.Select(x => new CodeVariable(null, references.GetVariableDeclaration(x.Type, csharpSemanticModel), x.Identifier.ValueText)).ToList(),
                documentation: AnalyzeDocumentation(codeElement));

            (parentElement as CodeType).Members.Add(conversionOperatorMethod);
        }

        #endregion

        #region Variable members
        private static void AnalyzeFieldDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentElement)
        {
            FieldDeclarationSyntax fieldDeclaration = codeElement as FieldDeclarationSyntax;

            CodeVariable codeVariable = new CodeVariable(
                parent: parentElement,
                declaration: references.GetVariableDeclaration(fieldDeclaration.Declaration.Type, csharpSemanticModel),
                accessModifier: fieldDeclaration.Modifiers.ToString(),
                fieldName: string.Join(", ", fieldDeclaration.Declaration.Variables.Select(x => x.Identifier)),
                documentation: AnalyzeDocumentation(codeElement));
           
            (parentElement as CodeType).Members.Add(codeVariable);
        }

        private static void AnalyzePropertyDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentElement)
        {
            PropertyDeclarationSyntax propertyDeclaration = codeElement as PropertyDeclarationSyntax;

            CodeProperty codeProperty = new CodeProperty(
                parent: parentElement,
                declaration: references.GetVariableDeclaration(propertyDeclaration.Type, csharpSemanticModel),
                accessModifier: propertyDeclaration.Modifiers.ToString(),
                name: propertyDeclaration.Identifier.ValueText,
                accessors: propertyDeclaration.AccessorList?.Accessors.Select(x => x.Keyword.ValueText).ToList(),
                documentation: AnalyzeDocumentation(codeElement));

            (parentElement as CodeType).Members.Add(codeProperty);
        }

        #endregion

        #endregion

        public static async Task<ProjectStructureTree> BuildProjectTreeAsync(DocsInfo docsInfo)
        {
            references = new ProjectTreeReferences();
            ProjectStructureTree projectStructureTree = new ProjectStructureTree();
            List<SyntaxTree> syntaxTrees = SetupSyntaxTrees(docsInfo);
            var compilation = GetCompilation(syntaxTrees);


            foreach (var tree in syntaxTrees)
                await AnalyzeSyntaxAsync(docsInfo, projectStructureTree, tree, compilation);

            //AddReferencesToVariableDeclarations();
            references.AddReferencesToElements();
            Cleanup();

            return projectStructureTree;
        }
    }
}
