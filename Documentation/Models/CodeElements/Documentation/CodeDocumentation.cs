using Documentation.FormatBuilders;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI.Design.WebControls;
using System.Xml.Linq;

namespace Documentation.Models.CodeElements.Documentation
{
    //internal record ParameterElement(string parameterName, string parameterValue, string value);    
    internal class CodeDocumentation
    {
        public CodeDocumentationElement? Summary { get; set; }
        public CodeDocumentationElement? Remarks { get; set; }
        public CodeDocumentationElement? Returns { get; set; }
        public List<CodeDocumentationElement> Parameters { get; set; }
        public List<CodeDocumentationElement> Exceptions { get; set; }
        public List<CodeDocumentationElement> Examples { get; set; }
        public List<CodeDocumentationElement> SeeAlsos { get; set; }


        public CodeDocumentation()
        {
            Parameters = new List<CodeDocumentationElement>();
            Exceptions = new List<CodeDocumentationElement>();
            Examples = new List<CodeDocumentationElement>();
            SeeAlsos = new List<CodeDocumentationElement>();
        }










        /*       public string? Summary { get; set; }
               public string? Remarks { get; set; }
               public string? Returns { get; set; }
               public string? Example { get; set; }
               public List<ParameterElement>? Exceptions { get; set; }
               public List<ParameterElement>? Parameters { get; set; }
               public List<ParameterElement>? SeeAlso { get; set; }

               public void Parse(IEnumerable<XmlElementSyntax> xmlElements)
               {
                   foreach (var xmlElement in xmlElements)
                   {
                       string name = xmlElement.StartTag.Name.ToString();
                       string content = xmlElement.Content.ToString().Replace("/// ", "").Trim();
                       content = Regex.Replace(content, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);
                       content.Replace("\r\n", " \\ \n");

                       switch (name)
                       {
                           case "summary":
                               Summary = content;
                               break;
                           case "remarks":
                               Remarks = content;
                               break;
                           case "returns":
                               Returns = content;
                               break;
                           case "example":
                               Example = content;
                               break;
                           case "param":
                               if (Parameters == null)
                                   Parameters = new List<ParameterElement>();

                               XmlNameAttributeSyntax attribute = xmlElement.StartTag.Attributes.OfType<XmlNameAttributeSyntax>().FirstOrDefault();
                               if (attribute == null)
                                   break;

                               Parameters.Add(new ParameterElement(
                                   parameterName: attribute.Name.ToString(),
                                   parameterValue: attribute.Identifier.Identifier.ValueText,
                                   value: content));
                               break;
                       }
                   }
               }


               private void ParseSummary(XmlElementSyntax summaryElement)
               {



               }


               public StringBuilder ConvertToMarkdownAllInOne(string indent)
               {
                   StringBuilder sb = new StringBuilder();
                   AddSimpleElementToResult(Summary, "Summary");
                   AddSimpleElementToResult(Remarks, "Remarks");
                   AddSimpleElementToResult(Returns, "Returns");
                   AddSimpleElementToResult(Example, "Example");

                   AddListElementToResult(Parameters, "Parameters");
                   AddListElementToResult(Exceptions, "Exceptions");
                   AddListElementToResult(SeeAlso, "See Also");


                   void AddSimpleElementToResult(string? element, string name)
                   {
                       if (element == null || element == string.Empty)
                           return;

                       sb.Append($"{indent}- __{name}__ - {element}\n");
                   }

                   void AddListElementToResult(List<ParameterElement>? elements, string name)
                   {
                       if (elements == null || elements.Count == 0)
                           return;

                       sb.Append($"{indent}- __{name}__:\n");
                       foreach (var element in elements)
                           sb.Append($"{indent}\t- {element.parameterValue} - {element.value}\n");
                   }

                   return sb;
               }


               public XElement ConvertToXml()
               {
                   throw new NotImplementedException();
               }
           }*/
    }
}
