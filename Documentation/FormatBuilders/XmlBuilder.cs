using Documentation.Models;
using System.Xml.Linq;

namespace Documentation.FormatBuilders
{
    internal class XmlBuilder : FormatBuilder
    {
        private void BuildTreeDocumentRecursive(ProjectStructureTreeNode nodeToGetXml, XElement previousElement, string path)
        {
            /*XElement currentElement;
            string currentPath = path;

            int noOfChilds = nodeToGetXml.Childs.Count;

            if (currentPath != string.Empty)
                currentPath += '.';

            currentPath += nodeToGetXml.Name;

            switch (noOfChilds)
            {
                case 0:
                    if (nodeToGetXml.NamespaceReference == null)
                        return;

                    currentElement = nodeToGetXml.NamespaceReference.ConvertToXml();
                    previousElement.Add(currentElement);
                    currentElement.Add(new XAttribute("name", currentPath));
                    break;
                case 1:
                    currentElement = previousElement;
                    break;
                default:
                    if (nodeToGetXml.NamespaceReference != null)
                        currentElement = nodeToGetXml.NamespaceReference.ConvertToXml();
                    else
                        currentElement = new XElement("namespace");

                    previousElement.Add(currentElement);
                    currentElement.Add(new XAttribute("name", currentPath));
                    currentPath = string.Empty;
                    break;
            }

            foreach (var child in nodeToGetXml.Childs)
                BuildTreeDocumentRecursive(child, currentElement, currentPath);*/
        }

        public XmlBuilder(DocsInfo docsInfo, ProjectStructureTree projectStructureTree) :
            base(docsInfo, projectStructureTree)
        {
        }

        /*public override void Generate()
        {
            XElement docsElement = new XElement("documentation");

            foreach (var proj in _solutionTree.root.Childs)
                BuildTreeDocumentRecursive(proj, docsElement, string.Empty);

            XDocument xDocument = new XDocument(docsElement);
            xDocument.Save(Path.Combine(_docsInfo.DocsPath, "docs.xml"));
        }*/

        public override void GenerateAllInOne()
        {
            throw new NotImplementedException();
        }

        public override void GenerateSplitByNamespace()
        {
            throw new NotImplementedException();
        }

        public override void GenerateSplitByType()
        {
            throw new NotImplementedException();
        }
    }
}
