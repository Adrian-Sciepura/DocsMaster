using Documentation.Engine.Models.CodeElements;
using Documentation.Engine.Models.Other;
using System.Collections.Generic;

namespace Documentation.Engine.Configuration
{
    public class DocsInfo
    {
        public string DocsPath { get; private set; }
        public List<Project> SolutionProjects { get; private set; }
        public List<SupportedExtensions> ExtensionsToGenerate { get; private set; }
        public Dictionary<CodeElementType, bool> CodeElementsToGenerateInSeparateFile { get; private set; }

        public enum SupportedExtensions
        {
            xml,
            md
        }

        public DocsInfo(string docsPath, List<Project> solutionProjects, List<SupportedExtensions> extensionsToGenerate, Dictionary<CodeElementType, bool> codeElementsToGenerateInSeparateFile)
        {
            DocsPath = docsPath;
            SolutionProjects = solutionProjects;
            ExtensionsToGenerate = extensionsToGenerate;
            CodeElementsToGenerateInSeparateFile = codeElementsToGenerateInSeparateFile;
        }
    }
}
