using Documentation.FormatBuilders;
using Documentation.Models;
using System.Collections.Generic;
using System.IO;

namespace Documentation.Services
{
    internal class DocumentationBuilder
    {
        private readonly DocsInfo _docsInfo;
        private ProjectStructureTree _solutionTree;

        public DocumentationBuilder(DocsInfo docsInfo)
        {
            _docsInfo = docsInfo;
        }

        public async Task BuildAsync()
        {
            if (_docsInfo.Configuration.ExtensionsToGenerate.Count == 0)
                return;

            _solutionTree = await ProjectTreeBuilder.BuildProjectTreeAsync(_docsInfo);

            List<FormatBuilder> builders = new List<FormatBuilder>();
            foreach (var extension in _docsInfo.Configuration.ExtensionsToGenerate)
            {
                switch (extension)
                {
                    case DocsConfiguration.SupportedExtensions.md:
                        builders.Add(new MarkDownBuilder(_docsInfo, _solutionTree));
                        break;
                    case DocsConfiguration.SupportedExtensions.xml:
                        builders.Add(new XmlBuilder(_docsInfo, _solutionTree));
                        break;
                }
            }

            foreach (var builder in builders)
                await builder.Generate();
        }

        public async Task BuildAsyncDebug()
        {
            if (_docsInfo.Configuration.ExtensionsToGenerate.Count == 0)
                return;

            List<string> extensionsBuildTime = new List<string>();
            string treeBuildTime;

            var watch = System.Diagnostics.Stopwatch.StartNew();
            _solutionTree = await ProjectTreeBuilder.BuildProjectTreeAsync(_docsInfo);
            watch.Stop();
            treeBuildTime = watch.Elapsed.TotalMilliseconds.ToString();


            List<FormatBuilder> builders = new List<FormatBuilder>();
            foreach (var extension in _docsInfo.Configuration.ExtensionsToGenerate)
            {
                switch (extension)
                {
                    case DocsConfiguration.SupportedExtensions.md:
                        builders.Add(new MarkDownBuilder(_docsInfo, _solutionTree));
                        break;
                    case DocsConfiguration.SupportedExtensions.xml:
                        builders.Add(new XmlBuilder(_docsInfo, _solutionTree));
                        break;
                }
            }

            foreach (var builder in builders)
            {
                watch.Restart();
                await builder.Generate();
                watch.Stop();
                extensionsBuildTime.Add($"{builder.GetType().Name}: {watch.Elapsed.TotalMilliseconds}");
            }

            string documentExportTime = watch.Elapsed.TotalMilliseconds.ToString();

            using (StreamWriter sw = new StreamWriter(Path.Combine(_docsInfo.DocsPath, "logs.txt")))
            {
                sw.WriteLine($"Tree build time: {treeBuildTime}");
                sw.WriteLine("Extensions build time:");
                extensionsBuildTime.ForEach(sw.WriteLine);
            }
        }
    }
}
