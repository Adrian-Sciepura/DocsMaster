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
        private delegate void SyntaxElementAction(DocsInfo docsInfo, ProjectStructureTree projectStructureTree, ProjectStructureTreeNode projectStructureTreeNode, SyntaxNode codeElement, IParentType parentReference);

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

        private static void AnalyzeMembers(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, IParentType parentReference, SyntaxList<MemberDeclarationSyntax> members)
        {
            SyntaxElementAction action;

            foreach (var member in members)
                if (syntaxElementFunctions.TryGetValue(member.Kind(), out action))
                    action.Invoke(docsInfo, tree, currentNode, member, parentReference);
        }

        #endregion



        #region Analyze Namespace

        private static void AnalyzeNamespaceDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentReference)
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

        private static void AnalyzeDelegateDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentReference)
        {
            DelegateDeclarationSyntax delegateDeclaration = codeElement as DelegateDeclarationSyntax;

            CodeDelegate codeDelegate = new CodeDelegate(
                parent: parentReference,
                namespaceReference: currentNode.NamespaceReference,
                declaration: references.GetTypeDeclaration(codeElement, csharpSemanticModel, delegateDeclaration.Identifier.ValueText, delegateDeclaration.TypeParameterList?.Parameters),
                accessModifier: delegateDeclaration.Modifiers.ToString(),
                returnType: delegateDeclaration.ReturnType.ToString(),
                parameters: delegateDeclaration.ParameterList.Parameters.Select(x => new CodeVariable(references.GetVariableDeclaration(x.Type, csharpSemanticModel), currentNode.NamespaceReference, null, x.Identifier.ValueText)).ToList(),
                documentation: AnalyzeDocumentation(codeElement));

            codeDelegate.Declaration.AddReference(codeDelegate);
            parentReference.AddInternalElement(codeDelegate);
            references.AddReference(codeDelegate);
            //AddTypeReference(currentNode, codeDelegate);
        }

        private static void AnalyzeClassDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentReference)
        {
            ClassDeclarationSyntax classDeclaration = codeElement as ClassDeclarationSyntax;

            CodeType codeClass = new CodeType(
                parent: parentReference,
                namespaceReference: currentNode.NamespaceReference,
                type: CodeElementType.Class,
                declaration: references.GetTypeDeclaration(codeElement, csharpSemanticModel, classDeclaration.Identifier.ValueText, classDeclaration.TypeParameterList?.Parameters),
                //name: classDeclaration.Identifier.ValueText,
                accessModifier: classDeclaration.Modifiers.ToString(),
                documentation: AnalyzeDocumentation(codeElement));

            codeClass.Declaration.AddReference(codeClass);
            parentReference.AddInternalElement(codeClass);
            AnalyzeMembers(docsInfo, tree, currentNode, codeClass, classDeclaration.Members);
            codeClass.Members.Sort();
            references.AddReference(codeClass);
            //AddTypeReference(currentNode, codeClass);
        }

        private static void AnalyzeRecordDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentReference)
        {
            RecordDeclarationSyntax recordDeclaration = codeElement as RecordDeclarationSyntax;

            CodeType codeRecord = new CodeType(
                parent: parentReference,
                namespaceReference: currentNode.NamespaceReference,
                type: CodeElementType.Record,
                declaration: references.GetTypeDeclaration(codeElement, csharpSemanticModel, recordDeclaration.Identifier.ValueText, recordDeclaration.TypeParameterList?.Parameters),
                //name: recordDeclaration.Identifier.ValueText,
                accessModifier: recordDeclaration.Modifiers.ToString(),
                documentation: AnalyzeDocumentation(codeElement));


            if (recordDeclaration.ParameterList != null)
                foreach (var variable in recordDeclaration.ParameterList.Parameters)
                    codeRecord.Members.Add(new CodeVariable(references.GetVariableDeclaration(variable.Type, csharpSemanticModel), currentNode.NamespaceReference, null, variable.Identifier.ValueText));


            codeRecord.Declaration.AddReference(codeRecord);
            parentReference.AddInternalElement(codeRecord);
            AnalyzeMembers(docsInfo, tree, currentNode, codeRecord, recordDeclaration.Members);
            codeRecord.Members.Sort();
            references.AddReference(codeRecord);
            //AddTypeReference(currentNode, codeRecord);
        }

        private static void AnalyzeInterfaceDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentReference)
        {
            InterfaceDeclarationSyntax interfaceDeclaration = codeElement as InterfaceDeclarationSyntax;

            CodeType codeInterface = new CodeType(
                parent: parentReference,
                namespaceReference: currentNode.NamespaceReference,
                type: CodeElementType.Interface,
                declaration: references.GetTypeDeclaration(codeElement, csharpSemanticModel, interfaceDeclaration.Identifier.ValueText, interfaceDeclaration.TypeParameterList?.Parameters),
                accessModifier: interfaceDeclaration.Modifiers.ToString(),
                documentation: AnalyzeDocumentation(codeElement));

            
            codeInterface.Declaration.AddReference(codeInterface);
            parentReference.AddInternalElement(codeInterface);
            AnalyzeMembers(docsInfo, tree, currentNode, codeInterface, interfaceDeclaration.Members);
            codeInterface.Members.Sort();
            references.AddReference(codeInterface);
        }

        private static void AnalyzeStructDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentReference)
        {
            StructDeclarationSyntax structDeclaration = codeElement as StructDeclarationSyntax;

            CodeType codeStruct = new CodeType(
                parent: parentReference,
                namespaceReference: currentNode.NamespaceReference,
                type: CodeElementType.Struct,
                declaration: references.GetTypeDeclaration(codeElement, csharpSemanticModel, structDeclaration.Identifier.ValueText, structDeclaration.TypeParameterList?.Parameters),
                accessModifier: structDeclaration.Modifiers.ToString(),
                documentation: AnalyzeDocumentation(codeElement));


            codeStruct.Declaration.AddReference(codeStruct);
            parentReference.AddInternalElement(codeStruct);
            AnalyzeMembers(docsInfo, tree, currentNode, codeStruct, structDeclaration.Members);
            codeStruct.Members.Sort();
            references.AddReference(codeStruct);
        }

        private static void AnalyzeEnumDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentReference)
        {
            EnumDeclarationSyntax enumDeclaration = codeElement as EnumDeclarationSyntax;

            CodeEnum codeEnum = new CodeEnum(
                parent: parentReference,
                namespaceReference: currentNode.NamespaceReference,
                declaration: (CodeRegularDeclaration)references.GetTypeDeclaration(codeElement, csharpSemanticModel, enumDeclaration.Identifier.ValueText, null),
                accessModifier: enumDeclaration.Modifiers.ToString(),
                elements: new List<string>(enumDeclaration.Members.Select(x => x.Identifier.ValueText)),
                documentation: AnalyzeDocumentation(codeElement));


            codeEnum.Declaration.AddReference(codeEnum);
            parentReference.AddInternalElement(codeEnum);
            references.AddReference(codeEnum);
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

        private static void AnalyzeMethodDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentReference)
        {
            MethodDeclarationSyntax methodDeclaration = codeElement as MethodDeclarationSyntax;

            CodeMethod codeMethod = new CodeMethod(
                parent: parentReference,
                namespaceReference: currentNode.NamespaceReference,
                declaration: references.GetMethodDeclaration(methodDeclaration, methodDeclaration.Identifier.ValueText, csharpSemanticModel, methodDeclaration.TypeParameterList?.Parameters),
                accessModifier: methodDeclaration.Modifiers.ToString(),
                returnType: references.GetVariableDeclaration(methodDeclaration.ReturnType, csharpSemanticModel),
                parameters: methodDeclaration.ParameterList.Parameters.Select(x => new CodeVariable(references.GetVariableDeclaration(x.Type, csharpSemanticModel), currentNode.NamespaceReference, null, x.Identifier.ValueText)).ToList(),
                documentation: AnalyzeDocumentation(codeElement));

            parentReference.AddInternalElement(codeMethod);
        }

        private static void AnalyzeContructorDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentReference)
        {
            ConstructorDeclarationSyntax constructorDeclaration = codeElement as ConstructorDeclarationSyntax;

            CodeConstructor constructorMethod = new CodeConstructor(
                parent: parentReference,
                namespaceReference: currentNode.NamespaceReference,
                declaration: (CodeRegularDeclaration)references.GetMethodDeclaration(constructorDeclaration, constructorDeclaration.Identifier.ValueText, csharpSemanticModel, null),
                accessModifier: constructorDeclaration.Modifiers.ToString(),
                parameters: constructorDeclaration.ParameterList.Parameters.Select(x => new CodeVariable(references.GetVariableDeclaration(x.Type, csharpSemanticModel), currentNode.NamespaceReference, null, x.Identifier.ValueText)).ToList(),
                documentation: AnalyzeDocumentation(codeElement));

            parentReference.AddInternalElement(constructorMethod);
        }

        private static void AnalyzeDestructorDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentReference)
        {
            DestructorDeclarationSyntax destructorDeclaration = codeElement as DestructorDeclarationSyntax;

            CodeDestructor destructorMethod = new CodeDestructor(
                parent: parentReference,
                namespaceReference: currentNode.NamespaceReference,
                declaration: (CodeRegularDeclaration)references.GetMethodDeclaration(destructorDeclaration, destructorDeclaration.Identifier.ValueText, csharpSemanticModel, null),
                documentation: AnalyzeDocumentation(codeElement));

            parentReference.AddInternalElement(destructorMethod);
        }

        private static void AnalyzeOperatorDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentReference)
        {
            OperatorDeclarationSyntax operatorDeclaration = codeElement as OperatorDeclarationSyntax;

            CodeOperator operatorMethod = new CodeOperator(
                parent: parentReference,
                namespaceReference: currentNode.NamespaceReference,
                declaration: (CodeRegularDeclaration)references.GetMethodDeclaration(operatorDeclaration, operatorDeclaration.OperatorToken.ValueText, csharpSemanticModel, null),
                //name: operatorDeclaration.OperatorToken.ValueText,
                accessModifier: operatorDeclaration.Modifiers.ToString(),
                returnType: references.GetVariableDeclaration(operatorDeclaration.ReturnType, csharpSemanticModel),
                parameters: operatorDeclaration.ParameterList.Parameters.Select(x => new CodeVariable(references.GetVariableDeclaration(x.Type, csharpSemanticModel), currentNode.NamespaceReference, null, x.Identifier.ValueText)).ToList(),
                documentation: AnalyzeDocumentation(codeElement));

            parentReference.AddInternalElement(operatorMethod);
        }

        private static void AnalyzeConversionOperatorDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentReference)
        {
            ConversionOperatorDeclarationSyntax conversionOperatorDeclaration = codeElement as ConversionOperatorDeclarationSyntax;

            CodeOperator conversionOperatorMethod = new CodeOperator(
                parent: parentReference,
                namespaceReference: currentNode.NamespaceReference,
                declaration: (CodeRegularDeclaration)references.GetMethodDeclaration(conversionOperatorDeclaration, conversionOperatorDeclaration.Type.ToString(), csharpSemanticModel, null),
                accessModifier: conversionOperatorDeclaration.Modifiers.ToString(),
                returnType: new CodeRegularDeclaration(string.Empty, null),
                parameters: conversionOperatorDeclaration.ParameterList.Parameters.Select(x => new CodeVariable(references.GetVariableDeclaration(x.Type, csharpSemanticModel), currentNode.NamespaceReference, null, x.Identifier.ValueText)).ToList(),
                documentation: AnalyzeDocumentation(codeElement));

            parentReference.AddInternalElement(conversionOperatorMethod);
        }

        #endregion

        #region Variable members
        private static void AnalyzeFieldDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentReference)
        {
            FieldDeclarationSyntax fieldDeclaration = codeElement as FieldDeclarationSyntax;

            CodeVariable codeVariable = new CodeVariable(
                parent: parentReference,
                namespaceReference: currentNode.NamespaceReference,
                declaration: references.GetVariableDeclaration(fieldDeclaration.Declaration.Type, csharpSemanticModel),
                accessModifier: fieldDeclaration.Modifiers.ToString(),
                fieldName: string.Join(", ", fieldDeclaration.Declaration.Variables.Select(x => x.Identifier)),
                documentation: AnalyzeDocumentation(codeElement));
           
            parentReference.AddInternalElement(codeVariable);
        }

        private static void AnalyzePropertyDeclaration(DocsInfo docsInfo, ProjectStructureTree tree, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentReference)
        {
            PropertyDeclarationSyntax propertyDeclaration = codeElement as PropertyDeclarationSyntax;

            CodeProperty codeProperty = new CodeProperty(
                parent: parentReference,
                namespaceReference: currentNode.NamespaceReference,
                declaration: references.GetVariableDeclaration(propertyDeclaration.Type, csharpSemanticModel),
                accessModifier: propertyDeclaration.Modifiers.ToString(),
                name: propertyDeclaration.Identifier.ValueText,
                accessors: propertyDeclaration.AccessorList?.Accessors.Select(x => x.Keyword.ValueText).ToList(),
                documentation: AnalyzeDocumentation(codeElement));

            parentReference.AddInternalElement(codeProperty);
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
