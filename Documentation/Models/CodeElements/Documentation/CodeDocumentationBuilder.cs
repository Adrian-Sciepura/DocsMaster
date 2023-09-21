using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using static Documentation.Models.CodeElements.Documentation.CodeDocumentationElement;

namespace Documentation.Models.CodeElements.Documentation
{
    internal class CodeDocumentationBuilder
    {
        private delegate void ActionForTag(ActionParams parameters);
        private record ActionParams(ReferenceBuilder references, SemanticModel semanticModel, CodeDocumentation documentation, XmlNodeSyntax element, SyntaxList<XmlAttributeSyntax> attributes, CodeDocumentationElement? parent);

        private static readonly Regex cleanupXmlElementSyntax = new Regex(@"((\s?$[\r\n]*)|(\s*///))", RegexOptions.Compiled);


        private static Dictionary<string, ActionForTag> RunAction = new Dictionary<string, ActionForTag>()
        {
            { "summary",        (p) => p.documentation.Summary = ParseTag(p, CodeDocumentationElementType.Summary) },
            { "remarks",        (p) => p.documentation.Remarks = ParseTag(p, CodeDocumentationElementType.Remarks) },
            { "returns",        (p) => p.documentation.Returns = ParseTag(p, CodeDocumentationElementType.Returns) },
            { "param",          (p) => p.documentation.Parameters.Add(ParseTag(p, CodeDocumentationElementType.Param)) },
            { "typeparam",      (p) => p.documentation.Parameters.Add(ParseTag(p, CodeDocumentationElementType.Typeparam)) },
            { "exception",      (p) => p.documentation.Exceptions.Add(ParseTag(p, CodeDocumentationElementType.Exception)) },
            { "example",        (p) => p.documentation.Examples.Add(ParseTag(p, CodeDocumentationElementType.Example)) },
            { "seealso",        (p) => p.documentation.SeeAlsos.Add(ParseTag(p, CodeDocumentationElementType.SeeAlso)) },
            { "skip",           (p) => p.documentation.Skip = true },

            { "para",           (p) => ParseTag(p, CodeDocumentationElementType.Para) },
            { "paramref",       (p) => ParseTag(p, CodeDocumentationElementType.Paramref) },
            { "typeparamref",   (p) => ParseTag(p, CodeDocumentationElementType.Typeparamref) },
            { "c",              (p) => ParseTag(p, CodeDocumentationElementType.C) },
            { "code",           (p) => ParseTag(p, CodeDocumentationElementType.Code) },
            { "value",          (p) => ParseTag(p, CodeDocumentationElementType.Value) },
            { "see",            (p) => ParseTag(p, CodeDocumentationElementType.See) },


            { "list",           (p) => ParseTag(p, CodeDocumentationElementType.List) },
            { "listheader",     (p) => ParseTag(p, CodeDocumentationElementType.ListHeader) },
            { "item",           (p) => ParseTag(p, CodeDocumentationElementType.Item) },
            { "term",           (p) => ParseTag(p, CodeDocumentationElementType.Term) },
            { "description",    (p) => ParseTag(p, CodeDocumentationElementType.Description) },
        };


        private static CodeDocumentationElement ParseTag(ActionParams parameters, CodeDocumentationElementType type)
        {
            CodeDocumentationElement documentationElement = new CodeDocumentationElement(type);
            StringBuilder sb = new StringBuilder();
            ActionForTag InvokeAction;
            int noOfElements = 0;


            foreach (var elementAttribute in parameters.attributes)
            {
                switch (elementAttribute)
                {
                    case XmlCrefAttributeSyntax crefAttribute:
                        var crefSymbol = parameters.semanticModel.GetSymbolInfo(crefAttribute.Cref).Symbol;

                        if (crefSymbol == null)
                            continue;

                        var declaration = parameters.references.GetTypeDeclaration(crefSymbol, parameters.semanticModel);
                        documentationElement.Attributes.Add("cref", declaration);
                        parameters.references.AddDeclarationToQueue(declaration);
                        break;

                    case XmlNameAttributeSyntax regularAttribute:
                        documentationElement.Attributes.Add(regularAttribute.Name.ToString(), new TypeKind.CodeRegularDeclaration(regularAttribute.Identifier.Identifier.ValueText, null));
                        break;
                }
            }


            foreach (var child in parameters.element.ChildNodes())
            {
                switch (child.Kind())
                {
                    case SyntaxKind.XmlText:
                        var text = child.ToString();
                        sb.Append(cleanupXmlElementSyntax.Replace(text, string.Empty));
                        break;

                    case SyntaxKind.XmlElement:
                        XmlElementSyntax xmlElementSyntax = (XmlElementSyntax)child;

                        if (RunAction.TryGetValue(xmlElementSyntax.StartTag.Name.ToString(), out InvokeAction))
                        {
                            sb.Append($"{{{noOfElements}}}");

                            InvokeAction(new ActionParams(parameters.references, parameters.semanticModel, parameters.documentation, xmlElementSyntax, xmlElementSyntax.StartTag.Attributes, documentationElement));
                            noOfElements++;
                        }

                        break;

                    case SyntaxKind.XmlEmptyElement:
                        XmlEmptyElementSyntax xmlEmptyElementSyntax = (XmlEmptyElementSyntax)child;

                        if (RunAction.TryGetValue(xmlEmptyElementSyntax.Name.ToString(), out InvokeAction))
                        {
                            sb.Append($"{{{noOfElements}}}");

                            InvokeAction(new ActionParams(parameters.references, parameters.semanticModel, parameters.documentation, xmlEmptyElementSyntax, xmlEmptyElementSyntax.Attributes, documentationElement));
                            noOfElements++;
                        }

                        break;
                }
            }

            documentationElement.Text = sb.ToString();

            if (parameters.parent != null)
                parameters.parent.SubElements.Add(documentationElement);

            return documentationElement;
        }


        public static CodeDocumentation BuildDocumentation(IEnumerable<XmlElementSyntax> xmlElements, SemanticModel semanticModel, ReferenceBuilder references)
        {
            ActionForTag actionForTag;
            CodeDocumentation codeDocumentation = new CodeDocumentation();

            foreach (var xmlElement in xmlElements)
                if (!codeDocumentation.Skip && RunAction.TryGetValue(xmlElement.StartTag.Name.ToString(), out actionForTag))
                    actionForTag(new ActionParams(references, semanticModel, codeDocumentation, xmlElement, xmlElement.StartTag.Attributes, null));

            if (codeDocumentation.Parameters.Count != 0)
                codeDocumentation.Parameters.Sort();

            return codeDocumentation;
        }
    }
}
