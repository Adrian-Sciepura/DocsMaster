using Documentation.Engine.Configuration;
using Documentation.Engine.Models.CodeDocs;
using Documentation.Engine.Models.CodeElements;
using Documentation.Engine.Models.CodeElements.Methods;
using Documentation.Engine.Models.CodeElements.TypeKind;
using Documentation.Engine.Models.CodeElements.Types;
using Documentation.Engine.Models.CodeElements.Variables;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Documentation.Engine.ProjectTree
{
    internal static class ProjectTreeBuilder
    {
        #region Models

        private record SyntaxActionConstParams(DocsInfo docsInfo, ProjectStructureTree projetStructureTree, ReferenceBuilder references);
        private record SyntaxActionParams(SemanticModel semanticModel, ProjectStructureTreeNode currentNode, SyntaxNode codeElement, IParentType parentReference, CodeDocumentation documentation);

        #endregion



        #region Invoke Management

        private delegate void SyntaxElementAction(SyntaxActionConstParams cp, SyntaxActionParams p);

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

        #endregion



        #region Help Functions

        private static CSharpCompilation GetCompilation(List<SyntaxTree> trees)
        {
            return CSharpCompilation.Create("TestCompilation").AddSyntaxTrees(trees);
        }

        private static List<SyntaxTree> SetupSyntaxTrees(DocsInfo docsInfo)
        {
            List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();
            foreach (var proj in docsInfo.SolutionProjects)
                foreach (var projTree in proj.SyntaxTrees)
                    syntaxTrees.Add(projTree);

            return syntaxTrees;
        }

        private static async Task AnalyzeSyntaxAsync(SyntaxActionConstParams cp, SyntaxTree syntaxTree, CSharpCompilation compilation)
        {
            var syntax = (await syntaxTree.GetRootAsync()).DescendantNodes();
            SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
            foreach (var codeElement in syntax)
            {
                var kind = codeElement.Kind();
                if (kind == SyntaxKind.FileScopedNamespaceDeclaration || kind == SyntaxKind.NamespaceDeclaration)
                    AnalyzeNamespaceDeclaration(cp, new SyntaxActionParams(semanticModel, null, codeElement, null, null));
            }
        }

        private static void AnalyzeMembers(SyntaxActionConstParams cp, SemanticModel semanticModel, ProjectStructureTreeNode currentNode, IParentType parentReference, SyntaxList<MemberDeclarationSyntax> members)
        {
            SyntaxElementAction action;

            foreach (var member in members)
            {
                if (syntaxElementFunctions.TryGetValue(member.Kind(), out action))
                {
                    CodeDocumentation? codeDocs = AnalyzeDocumentation(cp.references, semanticModel, member);

                    if (codeDocs != null && codeDocs.Skip)
                        continue;

                    action.Invoke(cp, new SyntaxActionParams(semanticModel, currentNode, member, parentReference, codeDocs));
                }
            }

        }

        #endregion



        #region Analyze Namespace

        private static void AnalyzeNamespaceDeclaration(SyntaxActionConstParams cp, SyntaxActionParams p)
        {
            BaseNamespaceDeclarationSyntax baseNamespaceDeclaration = p.codeElement as BaseNamespaceDeclarationSyntax;

            string namespaceName = baseNamespaceDeclaration.Name.ToString();
            ProjectStructureTreeNode changeNode = cp.projetStructureTree.GetNodeByFullName(namespaceName);//GetCurrentNode(cp.docsInfo, cp.projetStructureTree, namespaceName);

            if (changeNode.NamespaceReference == null)
                changeNode.NamespaceReference = new CodeNamespace(namespaceName);

            AnalyzeMembers(cp, p.semanticModel, changeNode, changeNode.NamespaceReference, baseNamespaceDeclaration.Members);
            changeNode.NamespaceReference.InternalTypes.Sort();
        }

        #endregion



        #region Analyze Types

        private static void AnalyzeDelegateDeclaration(SyntaxActionConstParams cp, SyntaxActionParams p)
        {
            DelegateDeclarationSyntax delegateDeclaration = p.codeElement as DelegateDeclarationSyntax;

            CodeDelegate codeDelegate = new CodeDelegate(
                parent: p.parentReference,
                namespaceReference: p.currentNode.NamespaceReference,
                declaration: cp.references.GetTypeDeclaration(p.codeElement, p.semanticModel, delegateDeclaration.Identifier.ValueText, delegateDeclaration.TypeParameterList?.Parameters),
                accessModifier: delegateDeclaration.Modifiers.ToString(),
                returnType: cp.references.GetVariableDeclaration(delegateDeclaration.ReturnType, p.semanticModel),
                parameters: delegateDeclaration.ParameterList.Parameters.Select(x => new CodeField(cp.references.GetVariableDeclaration(x.Type, p.semanticModel), p.currentNode.NamespaceReference, null, x.Identifier.ValueText)).ToList(),
                documentation: p.documentation);

            codeDelegate.Declaration.AddReference(codeDelegate);
            p.parentReference.AddInternalElement(codeDelegate);
            cp.references.AddReference(codeDelegate);
        }

        private static void AnalyzeClassDeclaration(SyntaxActionConstParams cp, SyntaxActionParams p)
        {
            ClassDeclarationSyntax classDeclaration = p.codeElement as ClassDeclarationSyntax;
            BaseCodeDeclarationKind declaration = cp.references.GetTypeDeclaration(p.codeElement, p.semanticModel, classDeclaration.Identifier.ValueText, classDeclaration.TypeParameterList?.Parameters);

            if (classDeclaration.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)) && p.parentReference != null)
            {
                CodeType partialClass = (CodeType)p.parentReference.GetChild(CodeElementType.Class, declaration.GetName());

                if (partialClass != null)
                {
                    AnalyzeMembers(cp, p.semanticModel, p.currentNode, partialClass, classDeclaration.Members);
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
            AnalyzeMembers(cp, p.semanticModel, p.currentNode, codeClass, classDeclaration.Members);
            codeClass.Members.Sort();
            cp.references.AddReference(codeClass);
        }

        private static void AnalyzeRecordDeclaration(SyntaxActionConstParams cp, SyntaxActionParams p)
        {
            RecordDeclarationSyntax recordDeclaration = p.codeElement as RecordDeclarationSyntax;

            CodeType codeRecord = new CodeType(
                parent: p.parentReference,
                namespaceReference: p.currentNode.NamespaceReference,
                type: CodeElementType.Record,
                declaration: cp.references.GetTypeDeclaration(p.codeElement, p.semanticModel, recordDeclaration.Identifier.ValueText, recordDeclaration.TypeParameterList?.Parameters),
                accessModifier: recordDeclaration.Modifiers.ToString(),
                documentation: p.documentation);


            if (recordDeclaration.ParameterList != null)
                foreach (var variable in recordDeclaration.ParameterList.Parameters)
                    codeRecord.Members.Add(new CodeField(cp.references.GetVariableDeclaration(variable.Type, p.semanticModel), p.currentNode.NamespaceReference, null, variable.Identifier.ValueText));


            codeRecord.Declaration.AddReference(codeRecord);
            p.parentReference.AddInternalElement(codeRecord);
            AnalyzeMembers(cp, p.semanticModel, p.currentNode, codeRecord, recordDeclaration.Members);
            codeRecord.Members.Sort();
            cp.references.AddReference(codeRecord);
        }

        private static void AnalyzeInterfaceDeclaration(SyntaxActionConstParams cp, SyntaxActionParams p)
        {
            InterfaceDeclarationSyntax interfaceDeclaration = p.codeElement as InterfaceDeclarationSyntax;

            CodeType codeInterface = new CodeType(
                parent: p.parentReference,
                namespaceReference: p.currentNode.NamespaceReference,
                type: CodeElementType.Interface,
                declaration: cp.references.GetTypeDeclaration(p.codeElement, p.semanticModel, interfaceDeclaration.Identifier.ValueText, interfaceDeclaration.TypeParameterList?.Parameters),
                accessModifier: interfaceDeclaration.Modifiers.ToString(),
                documentation: p.documentation);


            codeInterface.Declaration.AddReference(codeInterface);
            p.parentReference.AddInternalElement(codeInterface);
            AnalyzeMembers(cp, p.semanticModel, p.currentNode, codeInterface, interfaceDeclaration.Members);
            codeInterface.Members.Sort();
            cp.references.AddReference(codeInterface);
        }

        private static void AnalyzeStructDeclaration(SyntaxActionConstParams cp, SyntaxActionParams p)
        {
            StructDeclarationSyntax structDeclaration = p.codeElement as StructDeclarationSyntax;

            CodeType codeStruct = new CodeType(
                parent: p.parentReference,
                namespaceReference: p.currentNode.NamespaceReference,
                type: CodeElementType.Struct,
                declaration: cp.references.GetTypeDeclaration(p.codeElement, p.semanticModel, structDeclaration.Identifier.ValueText, structDeclaration.TypeParameterList?.Parameters),
                accessModifier: structDeclaration.Modifiers.ToString(),
                documentation: p.documentation);


            codeStruct.Declaration.AddReference(codeStruct);
            p.parentReference.AddInternalElement(codeStruct);
            AnalyzeMembers(cp, p.semanticModel, p.currentNode, codeStruct, structDeclaration.Members);
            codeStruct.Members.Sort();
            cp.references.AddReference(codeStruct);
        }

        private static void AnalyzeEnumDeclaration(SyntaxActionConstParams cp, SyntaxActionParams p)
        {
            EnumDeclarationSyntax enumDeclaration = p.codeElement as EnumDeclarationSyntax;

            CodeEnum codeEnum = new CodeEnum(
                parent: p.parentReference,
                namespaceReference: p.currentNode.NamespaceReference,
                declaration: (CodeRegularDeclaration)cp.references.GetTypeDeclaration(p.codeElement, p.semanticModel, enumDeclaration.Identifier.ValueText, null),
                accessModifier: enumDeclaration.Modifiers.ToString(),
                elements: new List<string>(enumDeclaration.Members.Select(x => x.Identifier.ValueText)),
                documentation: p.documentation);


            codeEnum.Declaration.AddReference(codeEnum);
            p.parentReference.AddInternalElement(codeEnum);
            cp.references.AddReference(codeEnum);
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

        private static void AnalyzeMethodDeclaration(SyntaxActionConstParams cp, SyntaxActionParams p)
        {
            MethodDeclarationSyntax methodDeclaration = p.codeElement as MethodDeclarationSyntax;

            CodeMethod codeMethod = new CodeMethod(
                parent: p.parentReference,
                namespaceReference: p.currentNode.NamespaceReference,
                declaration: cp.references.GetMethodDeclaration(methodDeclaration, methodDeclaration.Identifier.ValueText, p.semanticModel, methodDeclaration.TypeParameterList?.Parameters),
                accessModifier: methodDeclaration.Modifiers.ToString(),
                returnType: cp.references.GetVariableDeclaration(methodDeclaration.ReturnType, p.semanticModel),
                parameters: methodDeclaration.ParameterList.Parameters.Select(x => new CodeField(cp.references.GetVariableDeclaration(x.Type, p.semanticModel), p.currentNode.NamespaceReference, null, x.Identifier.ValueText)).ToList(),
                documentation: p.documentation);

            p.parentReference.AddInternalElement(codeMethod);
        }

        private static void AnalyzeContructorDeclaration(SyntaxActionConstParams cp, SyntaxActionParams p)
        {
            ConstructorDeclarationSyntax constructorDeclaration = p.codeElement as ConstructorDeclarationSyntax;

            CodeConstructor constructorMethod = new CodeConstructor(
                parent: p.parentReference,
                namespaceReference: p.currentNode.NamespaceReference,
                declaration: (CodeRegularDeclaration)cp.references.GetMethodDeclaration(constructorDeclaration, constructorDeclaration.Identifier.ValueText, p.semanticModel, null),
                accessModifier: constructorDeclaration.Modifiers.ToString(),
                parameters: constructorDeclaration.ParameterList.Parameters.Select(x => new CodeField(cp.references.GetVariableDeclaration(x.Type, p.semanticModel), p.currentNode.NamespaceReference, null, x.Identifier.ValueText)).ToList(),
                documentation: p.documentation);

            p.parentReference.AddInternalElement(constructorMethod);
        }

        private static void AnalyzeDestructorDeclaration(SyntaxActionConstParams cp, SyntaxActionParams p)
        {
            DestructorDeclarationSyntax destructorDeclaration = p.codeElement as DestructorDeclarationSyntax;

            CodeDestructor destructorMethod = new CodeDestructor(
                parent: p.parentReference,
                namespaceReference: p.currentNode.NamespaceReference,
                declaration: (CodeRegularDeclaration)cp.references.GetMethodDeclaration(destructorDeclaration, destructorDeclaration.Identifier.ValueText, p.semanticModel, null),
                documentation: p.documentation);

            p.parentReference.AddInternalElement(destructorMethod);
        }

        private static void AnalyzeOperatorDeclaration(SyntaxActionConstParams cp, SyntaxActionParams p)
        {
            OperatorDeclarationSyntax operatorDeclaration = p.codeElement as OperatorDeclarationSyntax;

            CodeOperator operatorMethod = new CodeOperator(
                parent: p.parentReference,
                namespaceReference: p.currentNode.NamespaceReference,
                declaration: (CodeRegularDeclaration)cp.references.GetMethodDeclaration(operatorDeclaration, operatorDeclaration.OperatorToken.ValueText, p.semanticModel, null),
                accessModifier: operatorDeclaration.Modifiers.ToString(),
                returnType: cp.references.GetVariableDeclaration(operatorDeclaration.ReturnType, p.semanticModel),
                parameters: operatorDeclaration.ParameterList.Parameters.Select(x => new CodeField(cp.references.GetVariableDeclaration(x.Type, p.semanticModel), p.currentNode.NamespaceReference, null, x.Identifier.ValueText)).ToList(),
                documentation: p.documentation);

            p.parentReference.AddInternalElement(operatorMethod);
        }

        private static void AnalyzeConversionOperatorDeclaration(SyntaxActionConstParams cp, SyntaxActionParams p)
        {
            ConversionOperatorDeclarationSyntax conversionOperatorDeclaration = p.codeElement as ConversionOperatorDeclarationSyntax;

            CodeOperator conversionOperatorMethod = new CodeOperator(
                parent: p.parentReference,
                namespaceReference: p.currentNode.NamespaceReference,
                declaration: (CodeRegularDeclaration)cp.references.GetMethodDeclaration(conversionOperatorDeclaration, conversionOperatorDeclaration.Type.ToString(), p.semanticModel, null),
                accessModifier: conversionOperatorDeclaration.Modifiers.ToString(),
                returnType: new CodeRegularDeclaration(string.Empty, null),
                parameters: conversionOperatorDeclaration.ParameterList.Parameters.Select(x => new CodeField(cp.references.GetVariableDeclaration(x.Type, p.semanticModel), p.currentNode.NamespaceReference, null, x.Identifier.ValueText)).ToList(),
                documentation: p.documentation);

            p.parentReference.AddInternalElement(conversionOperatorMethod);
        }

        #endregion



        #region Variable members
        private static void AnalyzeFieldDeclaration(SyntaxActionConstParams cp, SyntaxActionParams p)
        {
            FieldDeclarationSyntax fieldDeclaration = p.codeElement as FieldDeclarationSyntax;

            CodeField codeVariable = new CodeField(
                parent: p.parentReference,
                namespaceReference: p.currentNode.NamespaceReference,
                declaration: cp.references.GetVariableDeclaration(fieldDeclaration.Declaration.Type, p.semanticModel),
                accessModifier: fieldDeclaration.Modifiers.ToString(),
                variableNames: fieldDeclaration.Declaration.Variables.Select(x => x.Identifier.ToString()).ToList(),
                documentation: p.documentation);

            p.parentReference.AddInternalElement(codeVariable);
        }

        private static void AnalyzePropertyDeclaration(SyntaxActionConstParams cp, SyntaxActionParams p)
        {
            PropertyDeclarationSyntax propertyDeclaration = p.codeElement as PropertyDeclarationSyntax;

            CodeProperty codeProperty = new CodeProperty(
                parent: p.parentReference,
                namespaceReference: p.currentNode.NamespaceReference,
                declaration: cp.references.GetVariableDeclaration(propertyDeclaration.Type, p.semanticModel),
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
                await AnalyzeSyntaxAsync(new SyntaxActionConstParams(docsInfo, projectStructureTree, projectTreeReference), tree, compilation);


            projectTreeReference.AddReferencesToTreeElements();
            return projectStructureTree;
        }
    }
}
