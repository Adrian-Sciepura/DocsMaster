using DocsMaster.Engine.Models.CodeElements;
using System.Collections.Generic;

namespace DocsMaster.Engine.ProjectTree
{
    internal class ProjectStructureTreeNode
    {
        public string Name { get; private set; }
        public CodeNamespace? NamespaceReference { get; set; }
        public List<ProjectStructureTreeNode> Childs { get; set; }

        public ProjectStructureTreeNode(string name, CodeNamespace? namespaceReference = null)
        {
            Name = name;
            NamespaceReference = namespaceReference;
            Childs = new List<ProjectStructureTreeNode>();
        }
    }
}
