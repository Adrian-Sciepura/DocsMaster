namespace Documentation.Models
{
    internal class ProjectStructureTree
    {
        public ProjectStructureTreeNode root { get; init; }

        public ProjectStructureTree()
        {
            root = new ProjectStructureTreeNode("root");
        }

        /// <summary>
        /// A function that attempts to find a given node
        /// </summary>
        /// <param name="name">Full tree path</param>
        /// <param name="startingNode">Node from which the function will start the search - by default it searches from the root</param>
        /// <returns>Null when it fails to find</returns>
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

        /// <summary>
        /// A function that attempts to find a given node. 
        /// When it does not exist - it will be created along with the entire path
        /// </summary>
        /// <param name="name">Full tree path</param>
        /// <param name="startingNode">Node from which the function will start the search - by default it searches from the root</param>
        /// <returns>Node searched for</returns>
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
