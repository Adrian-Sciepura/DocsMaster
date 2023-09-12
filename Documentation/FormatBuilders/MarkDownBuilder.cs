using Documentation.Common;
using Documentation.Models;
using Documentation.Models.CodeElements;
using Documentation.Models.CodeElements.Documentation;
using Documentation.Models.CodeElements.Methods;
using Documentation.Models.CodeElements.TypeKind;
using Documentation.Models.CodeElements.Types;
using Documentation.Models.CodeElements.Variables;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static Documentation.Models.CodeElements.Documentation.CodeDocumentationElement;
using FileLayout = Documentation.Models.DocsConfiguration.FileLayoutType;

namespace Documentation.FormatBuilders
{
    internal sealed class MarkDownBuilder : FormatBuilder
    {
        private static string _outputFolder;

        private void GetNamespacesTreeRecursive(ProjectStructureTreeNode node, ref string text, string indent, string namePath, bool last, Action<CodeNamespace> RunForEveryNamespace, Func<CodeNamespace, string> PathBuilder)
        {
            namePath += namePath == string.Empty ? node.Name : $".{node.Name}";
            bool isNotNullReference = node.NamespaceReference != null;

            if (node.Childs.Count != 1 || isNotNullReference)
            {
                string symbol = last ? "╚═" : "╠═";
                if (isNotNullReference)
                {
                    text += $"{indent}{symbol} [{namePath}]({PathBuilder(node.NamespaceReference)})  \n";
                    RunForEveryNamespace(node.NamespaceReference);
                }
                else
                {
                    text += $"{indent}{symbol} {namePath}  \n";
                }

                indent += last ? "&emsp;&emsp;" : "║&emsp;&emsp;";
                namePath = string.Empty;
            }


            for (int i = 0; i < node.Childs.Count; i++)
                GetNamespacesTreeRecursive(node.Childs[i], ref text, indent, namePath, i == node.Childs.Count - 1, RunForEveryNamespace, PathBuilder);

        }

        public MarkDownBuilder(DocsInfo docsInfo, ProjectStructureTree solutionTree) :
            base(docsInfo, solutionTree)
        {
            _outputFolder = Path.Combine(docsInfo.DocsPath, "md");

            if (!Directory.Exists(_outputFolder))
                Directory.CreateDirectory(_outputFolder);
        }


        public override void GenerateAllInOne()
        {
            StringBuilder sb = new StringBuilder();
            Action<CodeNamespace> runForEveryNamespace = namespacereference => sb.Append(NamespaceAllInOne(FileLayout.AllInOne, namespacereference));
            Func<CodeNamespace, string> pathBuilder = namespaceReference => namespaceReference.Declaration.GetHash();

            string menu = "<link rel=\"stylesheet\" href=\"style.css\">\n<pre>\n";
            GetNamespacesTreeRecursive(_solutionTree.root, ref menu, "", "", true, runForEveryNamespace, pathBuilder);
            menu += "</pre>";

            using (StreamWriter allInOneFile = new StreamWriter(Path.Combine(_outputFolder, "README.md")))
            {
                allInOneFile.Write(menu);
                allInOneFile.Write(sb.ToString());
            }
        }

        public override void GenerateSplitByNamespace()
        {
            Action<CodeNamespace> runForEveryNamespace = namespacereference => NamespaceSplitByNamespace(FileLayout.SplitByNamespace, namespacereference, _outputFolder);
            Func<CodeNamespace, string> pathBuilder = namespaceReference => $"{namespaceReference.Declaration.GetName()}.md#{namespaceReference.Declaration.GetHash()}";

            string menu = "<pre>\n";
            GetNamespacesTreeRecursive(_solutionTree.root, ref menu, "", "", true, runForEveryNamespace, pathBuilder);
            menu += "</pre>";

            using (StreamWriter allInOneFile = new StreamWriter(Path.Combine(_outputFolder, "README.md")))
            {
                allInOneFile.Write(menu);
            }
        }

        public override void GenerateSplitByType()
        {
            Action<CodeNamespace> runForEveryNamespace = namespacereference => NamespaceSplitByType(FileLayout.SplitByType, namespacereference, _outputFolder);
            Func<CodeNamespace, string> pathBuilder = namespaceReference => $"{namespaceReference.Declaration.GetName()}.md#{namespaceReference.Declaration.GetHash()}";

            string menu = "";
            GetNamespacesTreeRecursive(_solutionTree.root, ref menu, "", "", true, runForEveryNamespace, pathBuilder);

            using (StreamWriter menuFile = new StreamWriter(Path.Combine(_outputFolder, "README.md")))
                menuFile.Write(menu);
        }


        #region Converters Invoke Management

        private delegate StringBuilder ActionForCodeElement(FileLayout fileLayout, CodeElement codeElement, string indent);

        private static readonly Dictionary<(CodeElementType, FileLayout), ActionForCodeElement> ConvertCodeElementToMarkdown = new Dictionary<(CodeElementType, FileLayout), ActionForCodeElement>()
        {
            //All In One File
            { (CodeElementType.Property, FileLayout.AllInOne), PropertyDefault },
            { (CodeElementType.Variable, FileLayout.AllInOne), VariableDefault },
            { (CodeElementType.Constructor, FileLayout.AllInOne), ConstructorDefault },
            { (CodeElementType.Destructor, FileLayout.AllInOne), DestructorDefault },
            { (CodeElementType.Method, FileLayout.AllInOne), MethodDefault },
            { (CodeElementType.Operator, FileLayout.AllInOne), OperatorDefault },
            { (CodeElementType.Delegate, FileLayout.AllInOne), DelegateDefault },
            { (CodeElementType.Interface, FileLayout.AllInOne), TypeAllInOne },
            { (CodeElementType.Class, FileLayout.AllInOne), TypeAllInOne },
            { (CodeElementType.Struct, FileLayout.AllInOne), TypeAllInOne },
            { (CodeElementType.Record, FileLayout.AllInOne), TypeAllInOne },
            { (CodeElementType.Enum, FileLayout.AllInOne), EnumDefault },


            //Split By Namespace
            { (CodeElementType.Property, FileLayout.SplitByNamespace), PropertyDefault },
            { (CodeElementType.Variable, FileLayout.SplitByNamespace), VariableDefault },
            { (CodeElementType.Constructor, FileLayout.SplitByNamespace), ConstructorDefault },
            { (CodeElementType.Destructor, FileLayout.SplitByNamespace), DestructorDefault },
            { (CodeElementType.Method, FileLayout.SplitByNamespace), MethodDefault },
            { (CodeElementType.Operator, FileLayout.SplitByNamespace), OperatorDefault },
            { (CodeElementType.Delegate, FileLayout.SplitByNamespace), DelegateDefault },
            { (CodeElementType.Interface, FileLayout.SplitByNamespace), TypeSplitByNamespace },
            { (CodeElementType.Class, FileLayout.SplitByNamespace), TypeSplitByNamespace },
            { (CodeElementType.Struct, FileLayout.SplitByNamespace), TypeSplitByNamespace },
            { (CodeElementType.Record, FileLayout.SplitByNamespace), TypeSplitByNamespace },
            { (CodeElementType.Enum, FileLayout.SplitByNamespace), EnumDefault },


            //Split By Type
            { (CodeElementType.Property, FileLayout.SplitByType), PropertyDefault },
            { (CodeElementType.Variable, FileLayout.SplitByType), VariableDefault },
            { (CodeElementType.Constructor, FileLayout.SplitByType), ConstructorDefault },
            { (CodeElementType.Destructor, FileLayout.SplitByType), DestructorDefault },
            { (CodeElementType.Method, FileLayout.SplitByType), MethodDefault },
            { (CodeElementType.Operator, FileLayout.SplitByType), OperatorDefault },
            { (CodeElementType.Delegate, FileLayout.SplitByType), DelegateSplitByType },
            { (CodeElementType.Interface, FileLayout.SplitByType), TypeSplitByType },
            { (CodeElementType.Class, FileLayout.SplitByType), TypeSplitByType },
            { (CodeElementType.Struct, FileLayout.SplitByType), TypeSplitByType },
            { (CodeElementType.Record, FileLayout.SplitByType), TypeSplitByType },
            { (CodeElementType.Enum, FileLayout.SplitByType), EnumSplitByType },


            //Split Everything
            { (CodeElementType.Property, FileLayout.SplitEverything), PropertyDefault },
            { (CodeElementType.Variable, FileLayout.SplitEverything), VariableDefault },
            { (CodeElementType.Constructor, FileLayout.SplitEverything), ConstructorDefault },
            { (CodeElementType.Destructor, FileLayout.SplitEverything), DestructorDefault },
            { (CodeElementType.Method, FileLayout.SplitEverything), MethodDefault },
            { (CodeElementType.Operator, FileLayout.SplitEverything), OperatorDefault },
            { (CodeElementType.Delegate, FileLayout.SplitEverything), DelegateDefault },
            { (CodeElementType.Interface, FileLayout.SplitEverything), NotImplementedConversion },
            { (CodeElementType.Class, FileLayout.SplitEverything), NotImplementedConversion },
            { (CodeElementType.Struct, FileLayout.SplitEverything), NotImplementedConversion },
            { (CodeElementType.Record, FileLayout.SplitEverything), NotImplementedConversion },
            { (CodeElementType.Enum, FileLayout.SplitEverything), EnumDefault },
        };

        #endregion

        #region Documentation Invoke Management

        private delegate StringBuilder ActionForDocumentationElement(FileLayout fileLayout, CodeDocumentationElement documentationElement, string indent);

        private static Dictionary<CodeDocumentationElementType, ActionForDocumentationElement> ConvertDocumentationElementToMarkdown = new Dictionary<CodeDocumentationElementType, ActionForDocumentationElement>()
        {
            { CodeDocumentationElementType.Summary, DocumentationSimpleElement },
            { CodeDocumentationElementType.Returns, DocumentationSimpleElement },
            { CodeDocumentationElementType.Remarks, DocumentationSimpleElement },
            { CodeDocumentationElementType.Param, DocumentationParam },
            { CodeDocumentationElementType.Typeparam, DocumentationParam },
            { CodeDocumentationElementType.Paramref, DocumentationParamref },
            { CodeDocumentationElementType.Typeparamref, DocumentationParamref },
            { CodeDocumentationElementType.Exception, DocumentationException },
            { CodeDocumentationElementType.SeeAlso, DocumentationSeeAlso },
            { CodeDocumentationElementType.Example, DocumentationExample },
            { CodeDocumentationElementType.Para,  DocumentationPara },
            { CodeDocumentationElementType.See, DocumentationSee },
            { CodeDocumentationElementType.C, DocumentationCode },
            { CodeDocumentationElementType.Code, DocumentationCode },
        };

        #endregion



        #region Documentation Converters

        #region Help Functions

        private static StringBuilder[] DocumentationSubElements(FileLayout fileLayout, List<CodeDocumentationElement> subElements, string indent)
        {
            if (subElements.Count == 0)
                return Array.Empty<StringBuilder>();

            StringBuilder[] stringBuilders = new StringBuilder[subElements.Count];
            ActionForDocumentationElement functionToInvoke;

            for (int i = 0; i < subElements.Count; i++)
                if (ConvertDocumentationElementToMarkdown.TryGetValue(subElements[i].Type, out functionToInvoke))
                    stringBuilders[i] = (functionToInvoke(fileLayout, subElements[i], indent));

            return stringBuilders;
        }

        private static StringBuilder DocumentationFormatText(FileLayout fileLayout, CodeDocumentationElement element, string indent)
        {
            StringBuilder sb = new StringBuilder();

            if (element.SubElements.Count > 0)
                sb.Append(string.Format(element.Text, DocumentationSubElements(fileLayout, element.SubElements, indent)));
            else
                sb.Append(element.Text);

            return sb;
        }

        private static StringBuilder DocumentationCrefAttribute(FileLayout fileLayout, CodeDocumentationElement codeDocumentationElement, string defaultContent)
        {
            CodeRegularDeclaration declaration;

            if (!codeDocumentationElement.Attributes.TryGetValue("cref", out declaration))
                return new StringBuilder();

            var typeReference = declaration.TypeReference;
            var text = defaultContent;

            string content = text != null ? text : (typeReference != null ? typeReference.Declaration.GetName() : "NULL");

            return new StringBuilder(typeReference == null ? content : $"[{content}]({ReferencePathDefault(fileLayout, typeReference, true, true)})");//<a href=\"{ReferencePathDefault(typeReference)}\">{content}</a>");
        }

        #endregion

        private static StringBuilder? Documentation(FileLayout fileLayout, CodeDocumentation? documentation, string indent)
        {
            if (documentation == null)
                return null;

            StringBuilder sb = new StringBuilder();
            ActionForDocumentationElement invokeAction;

            sb.Append($"\n{indent}\n{indent}| Name | Description |\n{indent}| --- | --- |\n");
            BuildSingleElement(documentation.Summary, CodeDocumentationElementType.Summary, "Description");
            BuildSingleElement(documentation.Returns, CodeDocumentationElementType.Returns, "Returns");
            BuildSingleElement(documentation.Remarks, CodeDocumentationElementType.Remarks, "Remarks");

            BuildListElement(documentation.Parameters, CodeDocumentationElementType.Param, "Parameters");
            BuildListElement(documentation.Exceptions, CodeDocumentationElementType.Exception, "Exceptions");
            BuildListElement(documentation.Examples, CodeDocumentationElementType.Example, "Examples");
            BuildListElement(documentation.SeeAlsos, CodeDocumentationElementType.SeeAlso, "See Also");


            void BuildSingleElement(CodeDocumentationElement? singleDocumentationElement, CodeDocumentationElementType elementType, string name)
            {
                if (singleDocumentationElement == null)
                    return;

                if (!ConvertDocumentationElementToMarkdown.TryGetValue(elementType, out invokeAction))
                    return;

                sb.Append($"{indent}| __{name}__ |");
                sb.Append(invokeAction(fileLayout, singleDocumentationElement, indent));
                //sb.Append(indent).Append();
                sb.Append(" |\n");
            }

            void BuildListElement(List<CodeDocumentationElement> listDocumentationElement, CodeDocumentationElementType elementChildType, string header)
            {
                if (listDocumentationElement.Count == 0)
                    return;

                if (!ConvertDocumentationElementToMarkdown.TryGetValue(elementChildType, out invokeAction))
                    return;


                sb.Append($"{indent}| __{header}__ | <ul>");

                foreach (var listElement in listDocumentationElement)
                    sb.Append(invokeAction(fileLayout, listElement, indent));

                sb.Append($"</ul> |\n");
            }

            return sb;
        }

        private static StringBuilder DocumentationListElement(StringBuilder content, string? headerContent = null, string? footerContent = null)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<li>");
            sb.TryAppend(headerContent);
            sb.Append(content);
            sb.TryAppend(footerContent);
            sb.Append("</li>");
            return sb;
        }

        private static StringBuilder DocumentationSimpleElement(FileLayout fileLayout, CodeDocumentationElement codeDocumentationElement, string indent)
        {
            return DocumentationFormatText(fileLayout, codeDocumentationElement, indent);
        }

        private static StringBuilder DocumentationParam(FileLayout fileLayout, CodeDocumentationElement codeDocumentationElement, string indent)
        {
            return DocumentationListElement(DocumentationFormatText(fileLayout, codeDocumentationElement, indent), $"{codeDocumentationElement.Attributes["name"].Name} - ", null);
        }

        private static StringBuilder DocumentationException(FileLayout fileLayout, CodeDocumentationElement codeDocumentationElement, string indent)
        {
            return DocumentationListElement(DocumentationFormatText(fileLayout, codeDocumentationElement, indent), DocumentationCrefAttribute(fileLayout, codeDocumentationElement, codeDocumentationElement.Text).ToString(), null);
        }

        private static StringBuilder DocumentationExample(FileLayout fileLayout, CodeDocumentationElement codeDocumentationElement, string indent)
        {
            return DocumentationListElement(DocumentationFormatText(fileLayout, codeDocumentationElement, indent), null, null);
        }

        private static StringBuilder DocumentationSeeAlso(FileLayout fileLayout, CodeDocumentationElement codeDocumentationElement, string indent)
        {
            CodeRegularDeclaration content;

            if (codeDocumentationElement.Attributes.TryGetValue("cref", out content))
                return DocumentationListElement(DocumentationCrefAttribute(fileLayout, codeDocumentationElement, codeDocumentationElement.Text), null, null);
            else if (codeDocumentationElement.Attributes.TryGetValue("href", out content))
                return DocumentationListElement(new StringBuilder("xxxx"), $"<a href=\"\">", "</a>");

            return DocumentationListElement(new StringBuilder(codeDocumentationElement.Text), null, null);
        }

        private static StringBuilder DocumentationSee(FileLayout fileLayout, CodeDocumentationElement codeDocumentationElement, string indent)
        {
            StringBuilder result;

            CodeRegularDeclaration content;
            string text = codeDocumentationElement.Text != null ? DocumentationFormatText(fileLayout, codeDocumentationElement, indent).ToString() : codeDocumentationElement.Text;

            if (codeDocumentationElement.Attributes.TryGetValue("cref", out content))
                result = DocumentationCrefAttribute(fileLayout, codeDocumentationElement, text);
            else
                result = new StringBuilder(text);

            return result;
        }

        private static StringBuilder DocumentationParamref(FileLayout fileLayout, CodeDocumentationElement codeDocumentationElement, string indent)
        {
            return new StringBuilder($"<b><i>{codeDocumentationElement.Attributes["name"]}</i></b>");
        }

        private static StringBuilder DocumentationPara(FileLayout fileLayout, CodeDocumentationElement codeDocumentationElement, string indent)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<p>");
            sb.Append(codeDocumentationElement.Text);
            sb.Append("</p>");
            return sb;
        }

        private static StringBuilder DocumentationCode(FileLayout fileLayout, CodeDocumentationElement codeDocumentationElement, string indent)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<code>");
            sb.Append(DocumentationFormatText(fileLayout, codeDocumentationElement, indent));
            sb.Append("</code>");
            return sb;
        }

        #endregion


        #region Const Values

        private const string CSS_STYLE_DECLARATION = "<link rel=\"stylesheet\" href=\"style.css\">";

        private const string TYPE_NO_CONTENT = "This type has no content";

        private const string NAMESPACE_DECLARATION_SELECTOR = "NamespaceDeclaration";

        private const string TYPE_DECLARATION_SELECTOR = "TypeDeclaration";
        private const string CLASS_DECLARATION_SELECTOR = "ClassDeclaration";
        private const string RECORD_DECLARATION_SELECTOR = "ClassDeclaration";
        private const string STRUCT_DECLARATION_SELECTOR = "StructDeclaration";
        private const string INTERFACE_DECLARATION_SELECTOR = "InterfaceDeclaration";
        private const string DELEGATE_DECLARATION_SELECTOR = "DelegateDeclaration";
        private const string ENUM_DECLARATION_SELECTOR = "EnumDeclaration";

        private const string METHOD_DECLARATION_SELECTOR = "MethodDeclaration";

        private const string METHOD_PARAMETER_NAME_SELECTOR = "MethodParameterName";
        private const string VARIABLE_NAME_SELECTOR = "VariableName";
        private const string KEYWORD_SELECTOR = "Keyword";
        private const string TYPE_PARAMETER_SELECTOR = "TypeParameter";


        private const string LIST_ELEMENT_DECLARATION_SELECTOR = "ListElement";
        private const string LIST_HEADER_DECLARATION_SELECTOR = "ListHeader";


        #endregion

        #region Default Converters

        #region Help functions

        private static readonly CodeElementType[] NamespaceTarget = new[] { CodeElementType.Namespace };
        private static readonly CodeElementType[] TypeTarget = new[] { CodeElementType.Namespace, CodeElementType.Interface, CodeElementType.Class, CodeElementType.Struct, CodeElementType.Record, CodeElementType.Enum };


        private static string BuildPath(CodeElement codeElement, CodeElementType[]? targetType)
        {
            Stack<string> elements = new Stack<string>();

            IParentType? parentType = codeElement.Parent;

            if (targetType == null || targetType.Contains(codeElement.Type))
                elements.Push(codeElement.Declaration.GetName());
            else
                while (parentType != null && !targetType.Contains(parentType.GetElement().Type))
                    parentType = parentType.GetParent();


            while (parentType != null)
            {
                elements.Push(parentType.GetElement().Declaration.GetName());
                parentType = parentType.GetParent();
            }

            if (elements.Count > 0)
                return string.Join(".", elements.ToArray());
            else
                return string.Empty;
        }

        private static string ReferencePathDefault(FileLayout fileLayout, CodeElement codeElement, bool includePath, bool includeHash)
        {
            StringBuilder sb = new StringBuilder();

            if (includePath)
            {
                switch (fileLayout)
                {
                    case FileLayout.AllInOne:
                        break;
                    case FileLayout.SplitByNamespace:
                        sb.Append(BuildPath(codeElement, NamespaceTarget)).Append(".md");
                        break;
                    case FileLayout.SplitByType:
                        sb.Append(BuildPath(codeElement, TypeTarget)).Append(".md");
                        break;
                    case FileLayout.SplitEverything:
                        sb.Append(BuildPath(codeElement, null)).Append(".md");
                        break;
                    default:
                        throw new ArgumentException($"Unknown file layout: {fileLayout}");
                }
            }


            if (includeHash)
            {
                sb.Append('#');
                sb.Append(codeElement.Declaration.GetHash());
            }

            return sb.ToString();
        }

        private static string GetSubType(CodeElementType elementType)
        {
            switch (elementType)
            {
                case CodeElementType.Class: return CLASS_DECLARATION_SELECTOR;
                case CodeElementType.Record: return RECORD_DECLARATION_SELECTOR;
                case CodeElementType.Struct: return STRUCT_DECLARATION_SELECTOR;
                case CodeElementType.Interface: return INTERFACE_DECLARATION_SELECTOR;
                case CodeElementType.Delegate: return DELEGATE_DECLARATION_SELECTOR;
                case CodeElementType.Enum: return ENUM_DECLARATION_SELECTOR;
                default: return string.Empty;
            }
        }

        private static StringBuilder DeclarationDefault(FileLayout fileLayout, BaseCodeDeclarationKind codeDeclaration, string mainTypeSelector, string defaultTypeSelector)
        {
            StringBuilder Analyze(BaseCodeDeclarationKind element, string selector)
            {
                StringBuilder stringBuilder = new StringBuilder();

                switch (element)
                {
                    case CodeRegularDeclaration regularDeclaration:
                        string content;
                        string typeSelector = selector;

                        if (regularDeclaration.TypeReference != null)
                        {
                            content = $"[{regularDeclaration.Name}]({ReferencePathDefault(fileLayout, regularDeclaration.TypeReference, true, true)})";
                            typeSelector += $" {GetSubType(regularDeclaration.TypeReference.Type)}";
                        }
                        else
                        {
                            content = regularDeclaration.Name;
                        }

                        AddElementWithSelector(content, typeSelector, stringBuilder);
                        break;

                    case CodeGenericDeclaration genericDeclaration:
                        stringBuilder.Append(Analyze(genericDeclaration.MainType, mainTypeSelector));

                        string temp = string.Join(", ", genericDeclaration.SubTypes.Select(x => Analyze(x, defaultTypeSelector)));
                        stringBuilder.Append($"&lt;{temp}&gt;");
                        break;

                    default:
                        throw new ArgumentException("Unknown Declaration Kind");
                }

                return stringBuilder;
            }

            return Analyze(codeDeclaration, mainTypeSelector);
        }

        private static StringBuilder TypeHeaderDefault(BaseCodeDeclarationKind declaration, string content, string indent)
        {
            StringBuilder sb = new StringBuilder();
            string hash = declaration.GetHash();
            string header = $"<strong><a name=\"{hash}\" id=\"{hash}\">{content}</a></strong>";

            if (indent == string.Empty)
                sb.Append($"## {header}");
            else
                sb.Append(header);

            sb.Append("\n\n");
            return sb;
        }

        private static StringBuilder MethodParametersDefault(FileLayout fileLayout, List<CodeVariable> parameters)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append('(');
            int numberOfAddedCommas = 0;

            foreach (CodeVariable parameter in parameters)
            {
                AddElementWithSelectorAndSpace(parameter.AccessModifier, KEYWORD_SELECTOR, sb);
                sb.AppendWithSpace(DeclarationDefault(fileLayout, parameter.Declaration, TYPE_DECLARATION_SELECTOR, TYPE_DECLARATION_SELECTOR));
                AddElementWithSelector(parameter.FieldName, METHOD_PARAMETER_NAME_SELECTOR, sb);

                if (numberOfAddedCommas < parameters.Count - 1)
                {
                    sb.Append(", ");
                    numberOfAddedCommas++;
                }
            }

            sb.Append(')');
            return sb;
        }

        private static void AddElementWithSelector(string? element, string selector, StringBuilder stringBuilder)
        {
            if (element == null)
                return;

            stringBuilder.Append($"<span class=\"{selector}\">");
            stringBuilder.Append(element);
            stringBuilder.Append("</span>");
        }

        private static void AddElementWithSelectorAndSpace(string? element, string selector, StringBuilder stringBuilder)
        {
            if (element == null)
                return;

            stringBuilder.Append($"<span class=\"{selector}\">");
            stringBuilder.Append(element);
            stringBuilder.Append("</span> ");
        }

        #endregion

        private static StringBuilder NotImplementedConversion(FileLayout fileLayout, CodeElement codeElement, string indent)
        {
            throw new NotImplementedException($"Conversion of element {codeElement.Type} has not yet been implemented");
        }

        private static StringBuilder PropertyDefault(FileLayout fileLayout, CodeElement codeElement, string indent)
        {
            CodeProperty codeProperty = codeElement as CodeProperty;
            StringBuilder sb = new StringBuilder();

            AddElementWithSelectorAndSpace(codeProperty.AccessModifier, KEYWORD_SELECTOR, sb);
            sb.AppendWithSpace(DeclarationDefault(fileLayout, codeProperty.Declaration, TYPE_DECLARATION_SELECTOR, TYPE_DECLARATION_SELECTOR));
            AddElementWithSelectorAndSpace(codeProperty.FieldName, VARIABLE_NAME_SELECTOR, sb);
            sb.Append($"{{ {string.Join(" ", codeProperty.Accessors.Select(x => $"<span class=\"{KEYWORD_SELECTOR}\">{x}</span>;") ?? Enumerable.Empty<string>())} }}");

            sb.Append(Documentation(fileLayout, codeElement.Documentation, indent + "  "));
            return sb;
        }

        private static StringBuilder VariableDefault(FileLayout fileLayout, CodeElement codeElement, string indent)
        {
            CodeVariable codeVariable = codeElement as CodeVariable;
            StringBuilder sb = new StringBuilder();

            AddElementWithSelectorAndSpace(codeVariable.AccessModifier, KEYWORD_SELECTOR, sb);
            sb.AppendWithSpace(DeclarationDefault(fileLayout, codeVariable.Declaration, TYPE_DECLARATION_SELECTOR, TYPE_DECLARATION_SELECTOR));
            AddElementWithSelector(codeVariable.FieldName, VARIABLE_NAME_SELECTOR, sb);

            sb.Append(Documentation(fileLayout, codeElement.Documentation, indent + "  "));
            return sb;
        }

        private static StringBuilder ConstructorDefault(FileLayout fileLayout, CodeElement codeElement, string indent)
        {
            CodeConstructor codeConstructor = codeElement as CodeConstructor;
            StringBuilder sb = new StringBuilder();

            AddElementWithSelectorAndSpace(codeConstructor.AccessModifier, KEYWORD_SELECTOR, sb);
            sb.Append(DeclarationDefault(fileLayout, codeConstructor.Declaration, METHOD_DECLARATION_SELECTOR, TYPE_PARAMETER_SELECTOR));
            sb.Append(MethodParametersDefault(fileLayout, codeConstructor.Parameters));

            sb.Append(Documentation(fileLayout, codeElement.Documentation, indent + "  "));

            return sb;
        }

        private static StringBuilder DestructorDefault(FileLayout fileLayout, CodeElement codeElement, string indent)
        {
            CodeDestructor codeDestructor = codeElement as CodeDestructor;
            StringBuilder sb = new StringBuilder();

            //sb.Append("<pre>").Append(CODE_BLOCK_OPEN);

            sb.Append('~');
            sb.Append(DeclarationDefault(fileLayout, codeDestructor.Declaration, METHOD_DECLARATION_SELECTOR, TYPE_PARAMETER_SELECTOR));
            sb.Append("()");

            //sb.Append(CODE_BLOCK_CLOSE);
            sb.Append(Documentation(fileLayout, codeElement.Documentation, indent + "  "));

            return sb;
        }

        private static StringBuilder MethodDefault(FileLayout fileLayout, CodeElement codeElement, string indent)
        {
            CodeMethod codeMethod = codeElement as CodeMethod;
            StringBuilder sb = new StringBuilder();

            AddElementWithSelectorAndSpace(codeMethod.AccessModifier, KEYWORD_SELECTOR, sb);
            sb.AppendWithSpace(DeclarationDefault(fileLayout, codeMethod.ReturnType, TYPE_DECLARATION_SELECTOR, TYPE_DECLARATION_SELECTOR));
            sb.Append(DeclarationDefault(fileLayout, codeMethod.Declaration, METHOD_DECLARATION_SELECTOR, TYPE_PARAMETER_SELECTOR));
            sb.Append(MethodParametersDefault(fileLayout, codeMethod.Parameters));

            sb.Append(Documentation(fileLayout, codeElement.Documentation, indent + "  "));

            return sb;
        }

        private static StringBuilder OperatorDefault(FileLayout fileLayout, CodeElement codeElement, string indent)
        {
            CodeOperator codeOperator = codeElement as CodeOperator;
            StringBuilder sb = new StringBuilder();

            AddElementWithSelectorAndSpace(codeOperator.AccessModifier, KEYWORD_SELECTOR, sb);
            sb.TryAppend(DeclarationDefault(fileLayout, codeOperator.ReturnType, TYPE_DECLARATION_SELECTOR, TYPE_DECLARATION_SELECTOR));
            sb.AppendWithSpace("operator");
            sb.Append(DeclarationDefault(fileLayout, codeOperator.Declaration, METHOD_DECLARATION_SELECTOR, TYPE_PARAMETER_SELECTOR));
            sb.Append(MethodParametersDefault(fileLayout, codeOperator.Parameters));

            sb.Append(Documentation(fileLayout, codeElement.Documentation, indent + "  "));

            return sb;
        }

        private static StringBuilder DelegateDefault(FileLayout fileLayout, CodeElement codeElement, string indent)
        {
            CodeDelegate codeDelegate = codeElement as CodeDelegate;

            StringBuilder headerBuilder = new StringBuilder();
            AddElementWithSelector(codeDelegate.AccessModifier, KEYWORD_SELECTOR, headerBuilder);
            headerBuilder.AppendWithSpace("delegate");
            headerBuilder.AppendWithSpace(codeDelegate.ReturnType);
            headerBuilder.Append(DeclarationDefault(fileLayout, codeDelegate.Declaration, TYPE_DECLARATION_SELECTOR, TYPE_PARAMETER_SELECTOR));
            headerBuilder.Append(MethodParametersDefault(fileLayout, codeDelegate.Parameters));

            StringBuilder sb = TypeHeaderDefault(codeElement.Declaration, headerBuilder.ToString(), indent);

            if (codeElement.Documentation != null)
            {
                //sb.Append("<pre>");
                sb.Append(Documentation(fileLayout, codeElement.Documentation, indent + "  "));
            }

            return sb;
        }

        private static StringBuilder EnumDefault(FileLayout fileLayout, CodeElement codeElement, string indent)
        {
            CodeEnum codeEnum = codeElement as CodeEnum;
            StringBuilder sb = TypeHeaderDefault(codeEnum.Declaration, DeclarationDefault(fileLayout, codeElement.Declaration, TYPE_DECLARATION_SELECTOR, TYPE_PARAMETER_SELECTOR).ToString(), indent);

            sb.Append($"{indent}Elements:\n{indent}");

            if (codeEnum.Elements.Count > 0)
                sb.Append(string.Join(", ", codeEnum.Elements));
            else
                sb.Append($"- {TYPE_NO_CONTENT}\n");

            sb.Append(Documentation(fileLayout, codeElement.Documentation, indent + "  "));
            return sb;
        }

        #endregion


        #region All In One Converters

        private static StringBuilder NamespaceAllInOne(FileLayout fileLayout, CodeNamespace codeNamespace)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"***\n\n<h1><strong><span class=\"{NAMESPACE_DECLARATION_SELECTOR}\"><a name=\"{codeNamespace.Declaration.GetName()}\" id=\"{codeNamespace.Declaration.GetHash()}\">{codeNamespace.Declaration.GetName()}</a></span></strong></h1>\n\n");


            CodeElementType currentCodeElementType = CodeElementType.None;
            foreach (var internalType in codeNamespace.InternalTypes)
            {
                if (internalType.Type != currentCodeElementType)
                {
                    sb.Append($"# {ConvertCodeElementType.ConvertToPluralForm[internalType.Type]}\n\n");
                    currentCodeElementType = internalType.Type;
                }

                sb.Append(ConvertCodeElementToMarkdown[(internalType.Type, fileLayout)](fileLayout, internalType, ""));
                sb.Append("\n\n");
            }

            if (codeNamespace.InternalTypes.Count == 0) sb.Append(TYPE_NO_CONTENT);
            return sb;
        }

        private static StringBuilder TypeAllInOne(FileLayout fileLayout, CodeElement codeElement, string indent)
        {
            CodeType codeType = codeElement as CodeType;
            StringBuilder sb = TypeHeaderDefault(codeType.Declaration, DeclarationDefault(fileLayout, codeElement.Declaration, TYPE_DECLARATION_SELECTOR, TYPE_PARAMETER_SELECTOR).ToString(), indent);

            string headerIndent = indent == string.Empty ? indent : indent + '\t';
            string newIndent = headerIndent + '\t';
            CodeElementType currentElementType = CodeElementType.None;

            if (codeElement.Documentation != null)
            {
                sb.Append($"{headerIndent}- __Documentation__\n{headerIndent}");
                sb.Append(Documentation(fileLayout, codeElement.Documentation, headerIndent + "  ")).Append('\n');
            }


            foreach (var member in codeType.Members)
            {
                if (member.Type != currentElementType)
                {
                    sb.Append($"{headerIndent}- <span class=\"{LIST_HEADER_DECLARATION_SELECTOR}\"> __{ConvertCodeElementType.ConvertToPluralForm[member.Type]}__ </span>\n");
                    currentElementType = member.Type;
                }

                sb.Append(newIndent);
                sb.AppendWithSpace($"- <span class=\"{LIST_ELEMENT_DECLARATION_SELECTOR}\">");
                sb.Append(ConvertCodeElementToMarkdown[(member.Type, fileLayout)](fileLayout, member, newIndent));
                sb.Append($"{newIndent}  </span>\n");
            }

            if (codeType.Members.Count == 0) sb.Append($"{headerIndent}- {TYPE_NO_CONTENT}\n");
            return sb;
        }

        #endregion


        #region Split By Namespace Converters

        private static void NamespaceSplitByNamespace(FileLayout fileLayout, CodeNamespace codeNamespace, string baseOutputPath)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<link rel=\"stylesheet\" href=\"style.css\">\n\n");
            sb.Append($"### [&#x21E6; Back](README.md)\n");
            sb.Append($"***\n\n<h1><strong><a name=\"{codeNamespace.Declaration.GetName()}\" id=\"{codeNamespace.Declaration.GetHash()}\">{codeNamespace.Declaration.GetName()}</a></strong></h1>\n\n");


            CodeElementType currentCodeElementType = CodeElementType.None;
            foreach (var internalType in codeNamespace.InternalTypes)
            {
                if (internalType.Type != currentCodeElementType)
                {
                    sb.Append($"# {ConvertCodeElementType.ConvertToPluralForm[internalType.Type]}\n\n");
                    currentCodeElementType = internalType.Type;
                }

                sb.Append(ConvertCodeElementToMarkdown[(internalType.Type, fileLayout)](fileLayout, internalType, ""));
                sb.Append("\n\n");
            }

            if (codeNamespace.InternalTypes.Count == 0) sb.Append(TYPE_NO_CONTENT);

            using (StreamWriter splitIntoNamespaces = new StreamWriter(Path.Combine(baseOutputPath, $"{codeNamespace.Declaration.GetName()}.md")))
            {
                splitIntoNamespaces.Write(sb.ToString());
            }
        }

        private static StringBuilder TypeSplitByNamespace(FileLayout fileLayout, CodeElement codeElement, string indent)
        {
            CodeType codeType = codeElement as CodeType;
            StringBuilder sb = TypeHeaderDefault(codeType.Declaration, DeclarationDefault(fileLayout, codeElement.Declaration, TYPE_DECLARATION_SELECTOR, TYPE_PARAMETER_SELECTOR).ToString(), indent);

            string headerIndent = indent == string.Empty ? indent : indent + '\t';
            string newIndent = headerIndent + '\t';
            CodeElementType currentElementType = CodeElementType.None;

            if (codeElement.Documentation != null)
            {
                sb.Append($"{headerIndent}- __Documentation__\n{headerIndent}");
                sb.Append(Documentation(fileLayout, codeElement.Documentation, headerIndent + "  ")).Append('\n');
            }


            foreach (var member in codeType.Members)
            {
                if (member.Type != currentElementType)
                {
                    sb.Append($"{headerIndent}- <span class=\"{LIST_HEADER_DECLARATION_SELECTOR}\"> __{ConvertCodeElementType.ConvertToPluralForm[member.Type]}__ </span>\n");
                    currentElementType = member.Type;
                }

                sb.Append(newIndent);
                sb.AppendWithSpace($"- <span class=\"{LIST_ELEMENT_DECLARATION_SELECTOR}\">");
                sb.Append(ConvertCodeElementToMarkdown[(member.Type, FileLayout.SplitByNamespace)](fileLayout, member, newIndent));
                sb.Append($"{newIndent}  </span>\n");
            }

            if (codeType.Members.Count == 0) sb.Append($"{headerIndent}- {TYPE_NO_CONTENT}\n");
            return sb;
        }

        #endregion


        #region Split By Type Converters

        #region Help Functions

        private static StringBuilder TypeTemplateSplitByType(StringBuilder content, CodeElement codeElement)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(CSS_STYLE_DECLARATION).Append("\n\n");
            sb.Append($"### [&#x21E6; Go Back]({ReferencePathDefault(FileLayout.SplitByType, codeElement.Parent.GetElement(), true, true)})\n");
            sb.Append(content);

            using (StreamWriter typeFile = new StreamWriter(Path.Combine(_outputFolder, ReferencePathDefault(FileLayout.SplitByType, codeElement, true, false))))
                typeFile.Write(sb.ToString());

            return sb;
        }

        #endregion

        private static void NamespaceSplitByType(FileLayout fileLayout, CodeNamespace codeNamespace, string baseOutputPath)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<link rel=\"stylesheet\" href=\"style.css\">\n\n");
            sb.Append($"### [&#x21E6; Go Back](README.md)\n");
            sb.Append($"***\n\n<h1><strong><a name=\"{codeNamespace.Declaration.GetHash()}\" id=\"{codeNamespace.Declaration.GetHash()}\">{codeNamespace.Declaration.GetName()}</a></strong></h1>\n\n");


            CodeElementType currentCodeElementType = CodeElementType.None;
            foreach (var internalType in codeNamespace.InternalTypes)
            {
                if (internalType.Type != currentCodeElementType)
                {
                    sb.Append("\n\n\n");
                    sb.Append($"# {ConvertCodeElementType.ConvertToPluralForm[internalType.Type]}\n");
                    sb.Append("| Name | Description |\n| --- | --- |\n");
                    currentCodeElementType = internalType.Type;
                }



                var declaration = ConvertCodeElementToMarkdown[(internalType.Type, fileLayout)](fileLayout, internalType, "");

                var docsDescription = internalType.Documentation?.Summary != null ?
                    ConvertDocumentationElementToMarkdown.GetValue(CodeDocumentationElementType.Summary)?.Invoke(fileLayout, internalType.Documentation.Summary, string.Empty).ToString() : string.Empty;

                sb.Append($"| {declaration} | {docsDescription} |\n");
            }

            if (codeNamespace.InternalTypes.Count == 0) sb.Append(TYPE_NO_CONTENT);

            using (StreamWriter namespaceFile = new StreamWriter(Path.Combine(baseOutputPath, $"{codeNamespace.Declaration.GetName()}.md")))
                namespaceFile.Write(sb.ToString());
        }

        private static StringBuilder TypeSplitByType(FileLayout fileLayout, CodeElement codeElement, string indent)
        {
            CodeType codeType = codeElement as CodeType;
            StringBuilder declaration = DeclarationDefault(fileLayout, codeElement.Declaration, TYPE_DECLARATION_SELECTOR, TYPE_PARAMETER_SELECTOR);
            StringBuilder sb = new StringBuilder();

            sb.Append(TypeHeaderDefault(codeType.Declaration, declaration.ToString(), indent));

            string newIndent = indent + '\t';
            CodeElementType currentElementType = CodeElementType.None;

            if (codeElement.Documentation != null)
            {
                sb.Append($"__Documentation__\n{newIndent}");
                sb.Append(Documentation(fileLayout, codeElement.Documentation, "  ")).Append('\n');
            }


            foreach (var member in codeType.Members)
            {
                if (member.Type != currentElementType)
                {
                    sb.Append($"<span class=\"{LIST_HEADER_DECLARATION_SELECTOR}\"> __{ConvertCodeElementType.ConvertToPluralForm[member.Type]}__ </span>\n");
                    currentElementType = member.Type;
                }

                sb.AppendWithSpace($"- <span class=\"{LIST_ELEMENT_DECLARATION_SELECTOR}\">");
                sb.Append(ConvertCodeElementToMarkdown[(member.Type, fileLayout)](fileLayout, member, ""));
                sb.Append($"</span>\n\n");
            }

            if (codeType.Members.Count == 0) sb.Append($"{newIndent}- {TYPE_NO_CONTENT}\n");

            TypeTemplateSplitByType(sb, codeType);
            return declaration;
        }

        private static StringBuilder DelegateSplitByType(FileLayout fileLayout, CodeElement codeElement, string indent)
        {
            CodeDelegate codeDelegate = codeElement as CodeDelegate;

            StringBuilder headerBuilder = new StringBuilder();
            AddElementWithSelector(codeDelegate.AccessModifier, KEYWORD_SELECTOR, headerBuilder);
            headerBuilder.AppendWithSpace($" <span class=\"{KEYWORD_SELECTOR}\">delegate</span>");
            headerBuilder.AppendWithSpace(codeDelegate.ReturnType);
            headerBuilder.Append(DeclarationDefault(fileLayout, codeDelegate.Declaration, TYPE_DECLARATION_SELECTOR, TYPE_PARAMETER_SELECTOR));
            headerBuilder.Append(MethodParametersDefault(fileLayout, codeDelegate.Parameters));

            StringBuilder sb = TypeHeaderDefault(codeElement.Declaration, headerBuilder.ToString(), indent);

            if (codeElement.Documentation != null)
                sb.Append(Documentation(fileLayout, codeElement.Documentation, indent + "  "));

            TypeTemplateSplitByType(sb, codeDelegate);
            return headerBuilder;
        }

        private static StringBuilder EnumSplitByType(FileLayout fileLayout, CodeElement codeElement, string indent)
        {
            CodeEnum codeEnum = codeElement as CodeEnum;
            StringBuilder declaration = DeclarationDefault(fileLayout, codeElement.Declaration, TYPE_DECLARATION_SELECTOR, TYPE_PARAMETER_SELECTOR);
            StringBuilder sb = TypeHeaderDefault(codeEnum.Declaration, declaration.ToString(), indent);

            sb.Append($"Elements:\n");

            if (codeEnum.Elements.Count > 0)
                codeEnum.Elements.ForEach(x => sb.Append($"- {x}\n"));
            else
                sb.Append($"- {TYPE_NO_CONTENT}\n");

            sb.Append(Documentation(fileLayout, codeElement.Documentation, indent + "  "));

            TypeTemplateSplitByType(sb, codeEnum);
            return declaration;
        }

        #endregion


        #region Split Everything Converters
        #endregion
    }
}
