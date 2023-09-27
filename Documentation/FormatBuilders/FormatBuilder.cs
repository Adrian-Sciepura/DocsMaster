using Documentation.Models;

namespace Documentation.FormatBuilders
{
    internal abstract class FormatBuilder
    {
        protected readonly DocsInfo _docsInfo;
        protected readonly ProjectStructureTree _solutionTree;

        public FormatBuilder(DocsInfo docsInfo, ProjectStructureTree solutionTree)
        {
            _docsInfo = docsInfo;
            _solutionTree = solutionTree;
        }

        public abstract Task Generate();
    }
}
