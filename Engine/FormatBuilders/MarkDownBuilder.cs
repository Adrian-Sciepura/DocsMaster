using Documentation.Engine.Common;
using Documentation.Engine.Configuration;
using Documentation.Engine.Models.CodeDocs;
using Documentation.Engine.Models.CodeElements;
using Documentation.Engine.Models.CodeElements.Methods;
using Documentation.Engine.Models.CodeElements.TypeKind;
using Documentation.Engine.Models.CodeElements.Types;
using Documentation.Engine.Models.CodeElements.Variables;
using Documentation.Engine.ProjectTree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Documentation.Engine.Models.CodeDocs.CodeDocumentationElement;
using TypeSetup = System.Collections.Generic.Dictionary<Documentation.Engine.Models.CodeElements.CodeElementType, bool>;

namespace Documentation.Engine.FormatBuilders
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

        public override async Task Generate()
        {
            StringBuilder sb = new StringBuilder();

            ConverterConstParams converterStaticParams = new ConverterConstParams()
            {
                TypeSetup = _docsInfo.CodeElementsToGenerateInSeparateFile,
                Tasks = new List<Task>()
            };

            Action<CodeNamespace> runForEveryNamespace = namespacereference => sb.Append(ConvertToMarkdown(converterStaticParams, namespacereference, namespacereference, ""));
            Func<CodeNamespace, string> pathBuilder = namespaceReference => converterStaticParams.TypeSetup[CodeElementType.Namespace] ? $"{namespaceReference.Declaration.GetName()}{namespaceReference.Declaration.GetHash()}.md#{namespaceReference.Declaration.GetHash()}" : $"#{namespaceReference.Declaration.GetHash()}";

            string menu = $"{CSS_STYLE_DECLARATION}\n\n";
            GetNamespacesTreeRecursive(_solutionTree.root, ref menu, "", "", true, runForEveryNamespace, pathBuilder);

            using (StreamWriter allInOneFile = new StreamWriter(Path.Combine(_outputFolder, "README.md")))
            {
                allInOneFile.Write(menu);
                allInOneFile.Write(sb.ToString());
            }

            await Task.WhenAll(converterStaticParams.Tasks.ToArray());
            CopyFileFromResources("style.css", Path.Combine(_outputFolder, "style.css"));
        }



        #region Models

        private class MarkdownCodeElement
        {
            public StringBuilder Header { get; set; }
            public StringBuilder Content { get; set; }
            public StringBuilder Documentation { get; set; }
            public StringBuilder? DisplayHeader { get; set; }

            public MarkdownCodeElement(StringBuilder header, StringBuilder content, StringBuilder documentation, StringBuilder? displayHeader = null)
            {
                Header = header;
                Content = content;
                Documentation = documentation;
                DisplayHeader = displayHeader;
            }

            public MarkdownCodeElement()
            {

            }
        }

        private class ConverterConstParams
        {
            public TypeSetup TypeSetup { get; set; }
            public List<Task> Tasks { get; set; }
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



        #region Converters Invoke Management

        private delegate MarkdownCodeElement ActionForCodeElement(ConverterConstParams constParams, CodeElement codeElement, IParentType currentParent, string indent);

        private static readonly Dictionary<(CodeElementType elementType, bool generateInExternalFile), ActionForCodeElement> MarkdownConverters = new Dictionary<(CodeElementType elementType, bool generateInExternalFile), ActionForCodeElement>()
        {
            { (CodeElementType.Namespace, false), NamespaceDefaultConvert },
            { (CodeElementType.Property, false), PropertyDefaultConvert },
            { (CodeElementType.Variable, false), VariableDefaultConvert },
            { (CodeElementType.Constructor, false), ConstructorDefaultConvert },
            { (CodeElementType.Destructor, false), DestructorDefaultConvert },
            { (CodeElementType.Method, false), MethodDefaultConvert },
            { (CodeElementType.Operator, false), OperatorDefaultConvert },
            { (CodeElementType.Delegate, false), DelegateDefaultConvert },
            { (CodeElementType.Interface, false), TypeDefaultConvert },
            { (CodeElementType.Class, false), TypeDefaultConvert },
            { (CodeElementType.Struct, false), TypeDefaultConvert },
            { (CodeElementType.Record, false), TypeDefaultConvert },
            { (CodeElementType.Enum, false), EnumDefaultConvert },


            { (CodeElementType.Namespace, true), NamespaceDefaultConvert },
            { (CodeElementType.Property, true), PropertyDefaultConvert },
            { (CodeElementType.Variable, true), VariableDefaultConvert },
            { (CodeElementType.Constructor, true), ConstructorDefaultConvert },
            { (CodeElementType.Destructor, true), DestructorDefaultConvert },
            { (CodeElementType.Method, true), MethodDefaultConvert },
            { (CodeElementType.Operator, true), OperatorDefaultConvert },
            { (CodeElementType.Delegate, true), DelegateSpecificConvert },
            { (CodeElementType.Interface, true), TypeSpecificConvert },
            { (CodeElementType.Class, true), TypeSpecificConvert },
            { (CodeElementType.Struct, true), TypeSpecificConvert },
            { (CodeElementType.Record, true), TypeSpecificConvert },
            { (CodeElementType.Enum, true), EnumSpecificConvert },


        };

        #endregion



        #region Documentation Invoke Management

        private delegate StringBuilder ActionForDocumentationElement(TypeSetup typeSetup, IParentType currentParent, CodeDocumentationElement documentationElement, string indent);

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

        private static StringBuilder[] DocumentationSubElements(TypeSetup typeSetup, IParentType currentParent, List<CodeDocumentationElement> subElements, string indent)
        {
            if (subElements.Count == 0)
                return Array.Empty<StringBuilder>();

            StringBuilder[] stringBuilders = new StringBuilder[subElements.Count];
            ActionForDocumentationElement functionToInvoke;

            for (int i = 0; i < subElements.Count; i++)
                if (ConvertDocumentationElementToMarkdown.TryGetValue(subElements[i].Type, out functionToInvoke))
                    stringBuilders[i] = (functionToInvoke(typeSetup, currentParent, subElements[i], indent));

            return stringBuilders;
        }

        private static StringBuilder DocumentationFormatText(TypeSetup typeSetup, IParentType currentParent, CodeDocumentationElement element, string indent)
        {
            StringBuilder sb = new StringBuilder();

            if (element.SubElements.Count > 0)
                sb.Append(string.Format(element.Text, DocumentationSubElements(typeSetup, currentParent, element.SubElements, indent)));
            else
                sb.Append(element.Text);

            return sb;
        }

        private static StringBuilder DocumentationCrefAttribute(TypeSetup typeSetup, IParentType currentParent, CodeDocumentationElement codeDocumentationElement, string defaultContent)
        {
            BaseCodeDeclarationKind declaration;

            if (!codeDocumentationElement.Attributes.TryGetValue("cref", out declaration))
                return new StringBuilder();

            var typeReference = declaration.GetTypeReference();

            if (defaultContent != null)
                return new StringBuilder($"<span class=\"{TYPE_DECLARATION_SELECTOR}\"> [{defaultContent}]({ReferencePathDefault(typeSetup, typeReference, currentParent, true, true)})</a>");

            return new StringBuilder(" ").Append(DeclarationDefaultConvert(typeSetup, declaration, currentParent, TYPE_DECLARATION_SELECTOR, TYPE_PARAMETER_SELECTOR));
        }

        #endregion


        private static StringBuilder? Documentation(ConverterConstParams constParams, IParentType currentParent, CodeDocumentation? documentation, string indent)
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
                sb.Append(invokeAction(constParams.TypeSetup, currentParent, singleDocumentationElement, indent));
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
                    sb.Append(invokeAction(constParams.TypeSetup, currentParent, listElement, indent));

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

        private static StringBuilder DocumentationSimpleElement(TypeSetup typeSetup, IParentType currentParent, CodeDocumentationElement codeDocumentationElement, string indent)
        {
            return DocumentationFormatText(typeSetup, currentParent, codeDocumentationElement, indent);
        }

        private static StringBuilder DocumentationParam(TypeSetup typeSetup, IParentType currentParent, CodeDocumentationElement codeDocumentationElement, string indent)
        {
            return DocumentationListElement(DocumentationFormatText(typeSetup, currentParent, codeDocumentationElement, indent), $"{codeDocumentationElement.Attributes["name"].GetName()} - ", null);
        }

        private static StringBuilder DocumentationException(TypeSetup typeSetup, IParentType currentParent, CodeDocumentationElement codeDocumentationElement, string indent)
        {
            return DocumentationListElement(DocumentationFormatText(typeSetup, currentParent, codeDocumentationElement, indent), DocumentationCrefAttribute(typeSetup, currentParent, codeDocumentationElement, null).ToString(), null);
        }

        private static StringBuilder DocumentationExample(TypeSetup typeSetup, IParentType currentParent, CodeDocumentationElement codeDocumentationElement, string indent)
        {
            return DocumentationListElement(DocumentationFormatText(typeSetup, currentParent, codeDocumentationElement, indent), null, null);
        }

        private static StringBuilder DocumentationSeeAlso(TypeSetup typeSetup, IParentType currentParent, CodeDocumentationElement codeDocumentationElement, string indent)
        {
            BaseCodeDeclarationKind content;

            if (codeDocumentationElement.Attributes.TryGetValue("cref", out content))
                return DocumentationListElement(DocumentationFormatText(typeSetup, currentParent, codeDocumentationElement, indent), DocumentationCrefAttribute(typeSetup, currentParent, codeDocumentationElement, null).ToString(), null);
            else if (codeDocumentationElement.Attributes.TryGetValue("href", out content))
                return DocumentationListElement(new StringBuilder($"{codeDocumentationElement.Text}"), $"<a href=\"{content}\">", "</a>");

            return DocumentationListElement(new StringBuilder(codeDocumentationElement.Text), null, null);
        }

        private static StringBuilder DocumentationSee(TypeSetup typeSetup, IParentType currentParent, CodeDocumentationElement codeDocumentationElement, string indent)
        {
            BaseCodeDeclarationKind content;

            if (codeDocumentationElement.Attributes.TryGetValue("cref", out content))
                return DocumentationCrefAttribute(typeSetup, currentParent, codeDocumentationElement, null);

            return new StringBuilder(codeDocumentationElement.Text ?? "NULL");
        }

        private static StringBuilder DocumentationParamref(TypeSetup typeSetup, IParentType currentParent, CodeDocumentationElement codeDocumentationElement, string indent)
        {
            return new StringBuilder($"<b><i>{codeDocumentationElement.Attributes["name"]}</i></b>");
        }

        private static StringBuilder DocumentationPara(TypeSetup typeSetup, IParentType currentParent, CodeDocumentationElement codeDocumentationElement, string indent)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<p>");
            sb.Append(codeDocumentationElement.Text);
            sb.Append("</p>");
            return sb;
        }

        private static StringBuilder DocumentationCode(TypeSetup typeSetup, IParentType currentParent, CodeDocumentationElement codeDocumentationElement, string indent)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<code>");
            sb.Append(DocumentationFormatText(typeSetup, currentParent, codeDocumentationElement, indent));
            sb.Append("</code>");
            return sb;
        }

        #endregion



        #region Default Converters


        #region Help functions

        private static Regex clearFileNames = new Regex(@"[^\w.]", RegexOptions.Compiled);

        private static string BuildPath(TypeSetup typeSetup, CodeElement codeElement, IParentType currentParent)
        {
            if (!typeSetup[codeElement.Type] && currentParent != null && codeElement.Parent == currentParent)
                return string.Empty;

            CodeElement temp;
            Stack<string> elements = new Stack<string>();
            IParentType? parentType = codeElement.Parent;

            if (typeSetup[codeElement.Type])
            {
                if (codeElement.Declaration is CodeGenericDeclaration genericDeclaration)
                    elements.Push($"T{genericDeclaration.SubTypes.Count}");

                elements.Push(clearFileNames.Replace(codeElement.Declaration.GetName(), string.Empty));
            }
            else
            {
                while (parentType != null && !typeSetup[parentType.GetElement().Type])
                    parentType = parentType.GetParent();
            }



            while (parentType != null)
            {
                temp = parentType.GetElement();

                if (temp.Declaration is CodeGenericDeclaration genericDeclaration)
                    elements.Push($"x{genericDeclaration.SubTypes.Count}");

                elements.Push(clearFileNames.Replace(temp.Declaration.GetName(), string.Empty));
                parentType = parentType.GetParent();
            }


            if (elements.Count > 0)
                return $"{string.Join(".", elements.ToArray())}{codeElement.Declaration.GetHash()}.md";


            return string.Empty;
        }

        private static string ReferencePathDefault(TypeSetup typeSetup, CodeElement codeElement, IParentType currentParent, bool includePath, bool includeHash)
        {
            StringBuilder sb = new StringBuilder();

            if (includePath)
                sb.Append(BuildPath(typeSetup, codeElement, currentParent));


            if (includeHash)
                sb.Append('#').Append(codeElement.Declaration.GetHash());

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

        private static StringBuilder DeclarationDefaultConvert(TypeSetup typeSetup, BaseCodeDeclarationKind codeDeclaration, IParentType currentParent, string mainTypeSelector, string defaultTypeSelector, bool anchorInsteadOfLinkForMainElement = false)
        {
            StringBuilder Analyze(BaseCodeDeclarationKind element, string selector, bool anchorInsteadOfLink)
            {
                StringBuilder stringBuilder = new StringBuilder();

                switch (element)
                {
                    case CodeRegularDeclaration regularDeclaration:
                        string content;
                        string typeSelector = selector;

                        if (regularDeclaration.TypeReference != null)
                        {
                            var hash = regularDeclaration.GetHash();

                            content = anchorInsteadOfLink ? $"<a name=\"{hash}\" id=\"{hash}\">{regularDeclaration.Name}</a>" : $"[{regularDeclaration.Name}]({ReferencePathDefault(typeSetup, regularDeclaration.TypeReference, currentParent, true, true)})";
                            typeSelector += $" {GetSubType(regularDeclaration.TypeReference.Type)}";
                        }
                        else
                        {
                            content = regularDeclaration.Name;
                        }

                        AddElementWithSelector(content, typeSelector, stringBuilder);
                        break;

                    case CodeGenericDeclaration genericDeclaration:
                        stringBuilder.Append(Analyze(genericDeclaration.MainType, mainTypeSelector, anchorInsteadOfLinkForMainElement));

                        string temp = string.Join(", ", genericDeclaration.SubTypes.Select(x => Analyze(x, defaultTypeSelector, false)));
                        stringBuilder.Append($"&lt;{temp}&gt;");
                        break;

                    default:
                        throw new ArgumentException("Unknown Declaration Kind");
                }

                return stringBuilder;
            }

            return Analyze(codeDeclaration, mainTypeSelector, anchorInsteadOfLinkForMainElement);
        }

        private static StringBuilder TypeHeaderDefaultConvert(BaseCodeDeclarationKind declaration, string content, string indent)
        {
            StringBuilder sb = new StringBuilder();

            if (indent == string.Empty)
                sb.Append($"## {content}");
            else
                sb.Append(content);

            sb.Append("\n\n");
            return sb;
        }

        private static StringBuilder MethodParametersDefault(TypeSetup typeSetup, List<CodeField> parameters, IParentType currentParent)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append('(');
            int numberOfAddedCommas = 0;

            foreach (CodeField parameter in parameters)
            {
                AddElementWithSelectorAndSpace(parameter.AccessModifier, KEYWORD_SELECTOR, sb);
                sb.AppendWithSpace(DeclarationDefaultConvert(typeSetup, parameter.Declaration, currentParent, TYPE_DECLARATION_SELECTOR, TYPE_DECLARATION_SELECTOR));
                AddElementWithSelector(string.Join(", ", parameter.VariableNames), METHOD_PARAMETER_NAME_SELECTOR, sb);

                if (numberOfAddedCommas < parameters.Count - 1)
                {
                    sb.Append(", ");
                    numberOfAddedCommas++;
                }
            }

            sb.Append(')');
            return sb;
        }

        private static async Task WriteElementToFileAsync(ConverterConstParams constParams, StringBuilder codeElementContent, IParentType currentParent, CodeElement codeElement)
        {
            string fileOutputPath = Path.Combine(_outputFolder, ReferencePathDefault(constParams.TypeSetup, codeElement, currentParent, true, false));


            StringBuilder sb = new StringBuilder();

            sb.Append(CSS_STYLE_DECLARATION).Append("\n\n");

            CodeElement? parentElement = codeElement.Parent?.GetElement();
            string path = parentElement != null ? ReferencePathDefault(constParams.TypeSetup, parentElement, null, true, true) : "README.md";

            sb.Append($"### [&#x21E6; Go Back]({path})\n");
            sb.Append(codeElementContent);

            using (StreamWriter elementFile = new StreamWriter(fileOutputPath))
                await elementFile.WriteAsync(sb.ToString());
        }

        private static StringBuilder ConvertToMarkdown(ConverterConstParams constParams, CodeElement codeElement, IParentType currentParent, string indent)
        {
            MarkdownCodeElement convertedElement = MarkdownConverters[(codeElement.Type, constParams.TypeSetup[codeElement.Type])](constParams, codeElement, currentParent, indent);
            StringBuilder fullContent = new StringBuilder();

            fullContent.Append(convertedElement.Header).TryAppend(convertedElement.Documentation).TryAppend(convertedElement.Content);

            if (constParams.TypeSetup[codeElement.Type])
            {
                constParams.Tasks.Add(Task.Run(async () => await WriteElementToFileAsync(constParams, fullContent, currentParent, codeElement)));
                //WriteElementToFileAsync(constParams, fullContent, currentParent, codeElement);
                return convertedElement.DisplayHeader ?? convertedElement.Header;
            }

            return fullContent;
        }

        #endregion


        private static MarkdownCodeElement NamespaceDefaultConvert(ConverterConstParams constParams, CodeElement codeElement, IParentType currentParent, string indent)
        {
            CodeNamespace codeNamespace = codeElement as CodeNamespace;
            StringBuilder header = new StringBuilder();
            StringBuilder content = new StringBuilder();
            content.Append($"***\n\n<h1><strong><span class=\"{NAMESPACE_DECLARATION_SELECTOR}\"><a name=\"{codeNamespace.Declaration.GetName()}\" id=\"{codeNamespace.Declaration.GetHash()}\">{codeNamespace.Declaration.GetName()}</a></span></strong></h1>\n\n");


            CodeElementType currentCodeElementType = CodeElementType.None;
            foreach (var internalType in codeNamespace.InternalTypes)
            {
                if (internalType.Type != currentCodeElementType)
                {
                    content.Append("\n");
                    content.Append($"# {ConvertCodeElementType.ConvertToPluralForm[internalType.Type]}\n\n");
                    currentCodeElementType = internalType.Type;

                    if (constParams.TypeSetup[internalType.Type])
                        content.Append("| Name | Description |\n| --- | --- |\n");
                }

                if (constParams.TypeSetup[currentCodeElementType])
                {
                    var declaration = ConvertToMarkdown(constParams, internalType, constParams.TypeSetup[codeElement.Type] ? codeNamespace : currentParent, "");

                    var docsDescription = internalType.Documentation?.Summary != null ?
                        ConvertDocumentationElementToMarkdown.GetValue(CodeDocumentationElementType.Summary)?.Invoke(constParams.TypeSetup, currentParent, internalType.Documentation.Summary, string.Empty).ToString() : string.Empty;

                    content.Append($"| {declaration} | {docsDescription} |\n");
                }
                else
                {
                    content.Append(ConvertToMarkdown(constParams, internalType, constParams.TypeSetup[codeElement.Type] ? codeNamespace : currentParent, ""));
                    content.Append("\n\n");
                }
            }

            if (codeNamespace.InternalTypes.Count == 0) content.Append(TYPE_NO_CONTENT);

            return new MarkdownCodeElement(header, content, null);
        }

        private static MarkdownCodeElement PropertyDefaultConvert(ConverterConstParams constParams, CodeElement codeElement, IParentType currentParent, string indent)
        {
            CodeProperty codeProperty = codeElement as CodeProperty;
            StringBuilder header = new StringBuilder();

            AddElementWithSelectorAndSpace(codeProperty.AccessModifier, KEYWORD_SELECTOR, header);
            header.AppendWithSpace(DeclarationDefaultConvert(constParams.TypeSetup, codeProperty.Declaration, currentParent, TYPE_DECLARATION_SELECTOR, TYPE_DECLARATION_SELECTOR));
            AddElementWithSelectorAndSpace(codeProperty.VariableNames.FirstOrDefault(), VARIABLE_NAME_SELECTOR, header);
            header.Append($"{{ {string.Join(" ", codeProperty.Accessors.Select(x => $"<span class=\"{KEYWORD_SELECTOR}\">{x}</span>;") ?? Enumerable.Empty<string>())} }}");

            StringBuilder documentation = Documentation(constParams, currentParent, codeElement.Documentation, indent + "  ");
            return new MarkdownCodeElement(header, null, documentation);
        }

        private static MarkdownCodeElement VariableDefaultConvert(ConverterConstParams constParams, CodeElement codeElement, IParentType currentParent, string indent)
        {
            CodeField codeField = codeElement as CodeField;
            StringBuilder header = new StringBuilder();

            AddElementWithSelectorAndSpace(codeField.AccessModifier, KEYWORD_SELECTOR, header);
            header.AppendWithSpace(DeclarationDefaultConvert(constParams.TypeSetup, codeField.Declaration, currentParent, TYPE_DECLARATION_SELECTOR, TYPE_DECLARATION_SELECTOR));
            AddElementWithSelector(string.Join(", ", codeField.VariableNames), VARIABLE_NAME_SELECTOR, header);

            StringBuilder documentation = Documentation(constParams, currentParent, codeElement.Documentation, indent + "  ");
            return new MarkdownCodeElement(header, null, documentation);
        }

        private static MarkdownCodeElement ConstructorDefaultConvert(ConverterConstParams constParams, CodeElement codeElement, IParentType currentParent, string indent)
        {
            CodeConstructor codeConstructor = codeElement as CodeConstructor;
            StringBuilder header = new StringBuilder();

            AddElementWithSelectorAndSpace(codeConstructor.AccessModifier, KEYWORD_SELECTOR, header);
            header.Append(DeclarationDefaultConvert(constParams.TypeSetup, codeConstructor.Declaration, currentParent, METHOD_DECLARATION_SELECTOR, TYPE_PARAMETER_SELECTOR));
            header.Append(MethodParametersDefault(constParams.TypeSetup, codeConstructor.Parameters, currentParent));

            StringBuilder documentation = Documentation(constParams, currentParent, codeElement.Documentation, indent + "  ");
            return new MarkdownCodeElement(header, null, documentation);
        }

        private static MarkdownCodeElement DestructorDefaultConvert(ConverterConstParams constParams, CodeElement codeElement, IParentType currentParent, string indent)
        {
            CodeDestructor codeDestructor = codeElement as CodeDestructor;
            StringBuilder header = new StringBuilder();

            header.Append('~');
            header.Append(DeclarationDefaultConvert(constParams.TypeSetup, codeDestructor.Declaration, currentParent, METHOD_DECLARATION_SELECTOR, TYPE_PARAMETER_SELECTOR));
            header.Append("()");

            StringBuilder documentation = Documentation(constParams, currentParent, codeElement.Documentation, indent + "  ");
            return new MarkdownCodeElement(header, null, documentation);
        }

        private static MarkdownCodeElement MethodDefaultConvert(ConverterConstParams constParams, CodeElement codeElement, IParentType currentParent, string indent)
        {
            CodeMethod codeMethod = codeElement as CodeMethod;
            StringBuilder header = new StringBuilder();

            AddElementWithSelectorAndSpace(codeMethod.AccessModifier, KEYWORD_SELECTOR, header);
            header.AppendWithSpace(DeclarationDefaultConvert(constParams.TypeSetup, codeMethod.ReturnType, currentParent, TYPE_DECLARATION_SELECTOR, TYPE_DECLARATION_SELECTOR));
            header.Append(DeclarationDefaultConvert(constParams.TypeSetup, codeMethod.Declaration, currentParent, METHOD_DECLARATION_SELECTOR, TYPE_PARAMETER_SELECTOR));
            header.Append(MethodParametersDefault(constParams.TypeSetup, codeMethod.Parameters, currentParent));

            StringBuilder documentation = Documentation(constParams, currentParent, codeElement.Documentation, indent + "  ");
            return new MarkdownCodeElement(header, null, documentation);
        }

        private static MarkdownCodeElement OperatorDefaultConvert(ConverterConstParams constParams, CodeElement codeElement, IParentType currentParent, string indent)
        {
            CodeOperator codeOperator = codeElement as CodeOperator;
            StringBuilder header = new StringBuilder();

            AddElementWithSelectorAndSpace(codeOperator.AccessModifier, KEYWORD_SELECTOR, header);
            header.TryAppend(DeclarationDefaultConvert(constParams.TypeSetup, codeOperator.ReturnType, currentParent, TYPE_DECLARATION_SELECTOR, TYPE_DECLARATION_SELECTOR));
            header.AppendWithSpace("operator");
            header.Append(DeclarationDefaultConvert(constParams.TypeSetup, codeOperator.Declaration, currentParent, METHOD_DECLARATION_SELECTOR, TYPE_PARAMETER_SELECTOR));
            header.Append(MethodParametersDefault(constParams.TypeSetup, codeOperator.Parameters, currentParent));

            StringBuilder documentation = Documentation(constParams, currentParent, codeElement.Documentation, indent + "  ");
            return new MarkdownCodeElement(header, null, documentation);
        }

        private static MarkdownCodeElement DelegateDefaultConvert(ConverterConstParams constParams, CodeElement codeElement, IParentType currentParent, string indent)
        {
            CodeDelegate codeDelegate = codeElement as CodeDelegate;

            StringBuilder header = new StringBuilder();
            AddElementWithSelectorAndSpace(codeDelegate.AccessModifier, KEYWORD_SELECTOR, header);
            header.AppendWithSpace($"<span class=\"{KEYWORD_SELECTOR}\">delegate</span>");
            header.AppendWithSpace(DeclarationDefaultConvert(constParams.TypeSetup, codeDelegate.ReturnType, currentParent, TYPE_DECLARATION_SELECTOR, TYPE_DECLARATION_SELECTOR));
            header.Append(DeclarationDefaultConvert(constParams.TypeSetup, codeDelegate.Declaration, currentParent, TYPE_DECLARATION_SELECTOR, TYPE_PARAMETER_SELECTOR));
            header.Append(MethodParametersDefault(constParams.TypeSetup, codeDelegate.Parameters, currentParent));

            StringBuilder h = TypeHeaderDefaultConvert(codeElement.Declaration, header.ToString(), indent);
            StringBuilder documentation = Documentation(constParams, currentParent, codeElement.Documentation, indent + "  ");
            return new MarkdownCodeElement(h, null, documentation);
        }

        private static MarkdownCodeElement EnumDefaultConvert(ConverterConstParams constParams, CodeElement codeElement, IParentType currentParent, string indent)
        {
            CodeEnum codeEnum = codeElement as CodeEnum;
            StringBuilder header = TypeHeaderDefaultConvert(codeEnum.Declaration, DeclarationDefaultConvert(constParams.TypeSetup, codeElement.Declaration, currentParent, TYPE_DECLARATION_SELECTOR, TYPE_PARAMETER_SELECTOR).ToString(), indent);
            StringBuilder content = new StringBuilder();

            content.Append($"{indent}Elements:\n{indent}");

            if (codeEnum.Elements.Count > 0)
                content.Append(string.Join(", ", codeEnum.Elements));
            else
                content.Append($"- {TYPE_NO_CONTENT}\n");

            StringBuilder documentation = Documentation(constParams, currentParent, codeElement.Documentation, indent + "  ");
            return new MarkdownCodeElement(header, content, documentation);
        }

        private static MarkdownCodeElement TypeDefaultConvert(ConverterConstParams constParams, CodeElement codeElement, IParentType currentParent, string indent)
        {
            CodeType codeType = codeElement as CodeType;
            IParentType newCurrentParent = constParams.TypeSetup[codeElement.Type] ? codeType : currentParent;
            StringBuilder header = TypeHeaderDefaultConvert(codeType.Declaration, DeclarationDefaultConvert(constParams.TypeSetup, codeElement.Declaration, newCurrentParent, TYPE_DECLARATION_SELECTOR, TYPE_PARAMETER_SELECTOR).ToString(), indent);

            string headerIndent = indent == string.Empty ? indent : indent + '\t';
            string newIndent = headerIndent + '\t';
            CodeElementType currentElementType = CodeElementType.None;

            StringBuilder content = new StringBuilder();
            foreach (var member in codeType.Members)
            {
                if (member.Type != currentElementType)
                {
                    content.Append($"{headerIndent}- <span class=\"{LIST_HEADER_DECLARATION_SELECTOR}\"> __{ConvertCodeElementType.ConvertToPluralForm[member.Type]}__ </span>\n");
                    currentElementType = member.Type;
                }

                content.Append(newIndent);
                content.AppendWithSpace($"- <span class=\"{LIST_ELEMENT_DECLARATION_SELECTOR}\">");
                content.Append(ConvertToMarkdown(constParams, member, newCurrentParent, newIndent));
                content.Append($"{newIndent}  </span>\n");
            }

            if (codeType.Members.Count == 0) content.Append($"{headerIndent}- {TYPE_NO_CONTENT}\n");

            StringBuilder documentation = new StringBuilder();
            if (codeElement.Documentation != null)
            {
                documentation.Append($"{headerIndent}- __Documentation__\n{headerIndent}");
                documentation.Append(Documentation(constParams, currentParent, codeElement.Documentation, headerIndent + "  ")).Append('\n');
            }

            return new MarkdownCodeElement(header, content, documentation);
        }

        #endregion



        #region Specific Converters

        private static MarkdownCodeElement TypeSpecificConvert(ConverterConstParams constParams, CodeElement codeElement, IParentType currentParent, string indent)
        {
            CodeType codeType = codeElement as CodeType;
            IParentType newCurrentParent = constParams.TypeSetup[codeElement.Type] ? codeType : currentParent;
            StringBuilder header = DeclarationDefaultConvert(constParams.TypeSetup, codeElement.Declaration, newCurrentParent, TYPE_DECLARATION_SELECTOR, TYPE_PARAMETER_SELECTOR, true);
            StringBuilder displayHeader = DeclarationDefaultConvert(constParams.TypeSetup, codeElement.Declaration, newCurrentParent, TYPE_DECLARATION_SELECTOR, TYPE_PARAMETER_SELECTOR);

            CodeElementType currentElementType = CodeElementType.None;

            StringBuilder content = new StringBuilder();
            content.Append("\n\n");

            foreach (var member in codeType.Members)
            {
                if (member.Type != currentElementType)
                {
                    content.Append($"\n\n<span class=\"{LIST_HEADER_DECLARATION_SELECTOR}\"> __{ConvertCodeElementType.ConvertToPluralForm[member.Type]}__ </span>\n\n");
                    currentElementType = member.Type;
                }

                content.AppendWithSpace($"- <span class=\"{LIST_ELEMENT_DECLARATION_SELECTOR}\">");
                content.Append(ConvertToMarkdown(constParams, member, newCurrentParent, indent));
                content.Append($"  </span>\n");
            }

            if (codeType.Members.Count == 0) content.Append($"{indent}- {TYPE_NO_CONTENT}\n");

            StringBuilder documentation = new StringBuilder();
            if (codeElement.Documentation != null)
            {
                documentation.Append($"\n- __Documentation__\n{indent}");
                documentation.Append(Documentation(constParams, currentParent, codeElement.Documentation, indent + "  ")).Append('\n');
            }

            return new MarkdownCodeElement(header, content, documentation, displayHeader);
        }

        private static MarkdownCodeElement EnumSpecificConvert(ConverterConstParams constParams, CodeElement codeElement, IParentType currentParent, string indent)
        {
            CodeEnum codeEnum = codeElement as CodeEnum;
            StringBuilder header = DeclarationDefaultConvert(constParams.TypeSetup, codeElement.Declaration, currentParent, TYPE_DECLARATION_SELECTOR, TYPE_PARAMETER_SELECTOR, true);
            StringBuilder displayHeader = DeclarationDefaultConvert(constParams.TypeSetup, codeElement.Declaration, currentParent, TYPE_DECLARATION_SELECTOR, TYPE_PARAMETER_SELECTOR);
            StringBuilder content = new StringBuilder();

            content.Append($"\n\nElements:\n");

            if (codeEnum.Elements.Count > 0)
                codeEnum.Elements.ForEach(x => content.Append($"- {x}\n"));
            else
                content.Append($"- {TYPE_NO_CONTENT}\n");

            StringBuilder documentation = Documentation(constParams, currentParent, codeElement.Documentation, indent + "  ");
            return new MarkdownCodeElement(header, content, documentation, displayHeader);
        }

        private static MarkdownCodeElement DelegateSpecificConvert(ConverterConstParams constParams, CodeElement codeElement, IParentType currentParent, string indent)
        {
            CodeDelegate codeDelegate = codeElement as CodeDelegate;

            StringBuilder header = new StringBuilder();
            StringBuilder displayHeader = new StringBuilder();

            AddElementWithSelectorAndSpace(codeDelegate.AccessModifier, KEYWORD_SELECTOR, header);

            header.AppendWithSpace($"<span class=\"{KEYWORD_SELECTOR}\">delegate</span>");

            var returnType = DeclarationDefaultConvert(constParams.TypeSetup, codeDelegate.ReturnType, currentParent, TYPE_DECLARATION_SELECTOR, TYPE_DECLARATION_SELECTOR);
            header.AppendWithSpace(returnType);
            displayHeader.AppendWithSpace(returnType);

            header.Append(DeclarationDefaultConvert(constParams.TypeSetup, codeDelegate.Declaration, currentParent, TYPE_DECLARATION_SELECTOR, TYPE_PARAMETER_SELECTOR));
            displayHeader.Append(DeclarationDefaultConvert(constParams.TypeSetup, codeDelegate.Declaration, currentParent, TYPE_DECLARATION_SELECTOR, TYPE_PARAMETER_SELECTOR));

            var parameters = MethodParametersDefault(constParams.TypeSetup, codeDelegate.Parameters, currentParent);
            header.Append(parameters);
            displayHeader.Append(parameters);

            StringBuilder documentation = Documentation(constParams, currentParent, codeElement.Documentation, indent + "  ");
            return new MarkdownCodeElement(header, null, documentation, displayHeader);
        }

        #endregion
    }
}
