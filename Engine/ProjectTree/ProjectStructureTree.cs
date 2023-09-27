namespace Documentation.Engine.ProjectTree
{
    internal class ProjectStructureTree
    {
        public ProjectStructureTreeNode root { get; init; }

        public ProjectStructureTree()
        {
            root = new ProjectStructureTreeNode("root");
        }

        public ProjectStructureTreeNode? TryGetNodeByFullName(string name, ProjectStructureTreeNode? startingNode = null)
        {
            string[] path = name.Split('.');

            ProjectStructureTreeNode? currentNode = startingNode ?? root;
            foreach (var elementName in path)
            {
                if (currentNode == null)
                    return null;

                foreach (var child in currentNode.Childs)
                {
                    if (child.Name == elementName)
                    {
                        currentNode = child;
                        break;
                    }

                    currentNode = null;
                }
            }

            return currentNode;
        }

        public ProjectStructureTreeNode GetNodeByFullName(string name, ProjectStructureTreeNode? startingNode = null)
        {
            string[] path = name.Split('.');

            ProjectStructureTreeNode? currentNode = startingNode ?? root;

            bool foundNode = false;
            foreach (var elementName in path)
            {
                foundNode = false;

                foreach (var child in currentNode.Childs)
                {
                    if (child.Name == elementName)
                    {
                        currentNode = child;
                        foundNode = true;
                        break;
                    }
                }

                if (!foundNode)
                {
                    var newChild = new ProjectStructureTreeNode(elementName);
                    currentNode.Childs.Add(newChild);
                    currentNode = newChild;
                }
            }

            return currentNode;
        }
    }
}
