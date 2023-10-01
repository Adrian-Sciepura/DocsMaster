using Documentation.Engine.Configuration;
using Documentation.Engine.ProjectTree;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Documentation.Engine.FormatBuilders
{
    internal abstract class FormatBuilder
    {
        protected readonly DocsInfo _docsInfo;
        protected readonly ProjectStructureTree _solutionTree;

        protected void CopyFileFromResources(string resourceName, string outputPath)
        {
            using (Stream style = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Documentation.Engine.Resources.{resourceName}"))
                using (Stream output = File.OpenWrite(outputPath))
                    style.CopyTo(output);
        }

        public FormatBuilder(DocsInfo docsInfo, ProjectStructureTree solutionTree)
        {
            _docsInfo = docsInfo;
            _solutionTree = solutionTree;
        }

        public abstract Task Generate();
    }
}
