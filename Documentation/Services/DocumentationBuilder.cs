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
            _solutionTree = await ProjectTreeBuilder.BuildProjectTreeAsync(_docsInfo);

            List<FormatBuilder> builders = new List<FormatBuilder>();
            foreach (var extension in _docsInfo.Configuration.ExtensionsToGenerate)
            {
                switch (extension)
                {
                    case DocsConfiguration.SupportedExtensions.md:
                        builders.Add(new MarkDownBuilder(_docsInfo, _solutionTree));
                        break;
                }
            }

            switch (_docsInfo.Configuration.FileLayout)
            {
                case DocsConfiguration.FileLayoutType.AllInOne:
                    builders.ForEach(b => b.GenerateAllInOne());
                    break;
                case DocsConfiguration.FileLayoutType.SplitByNamespace:
                    builders.ForEach(b => b.GenerateSplitByNamespace());
                    break;
                case DocsConfiguration.FileLayoutType.SplitByType:
                    builders.ForEach(b => b.GenerateSplitByType());
                    break;
            }
        }

        public async Task BuildAsyncDebug()
        {
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
                }
            }

            switch (_docsInfo.Configuration.FileLayout)
            {
                case DocsConfiguration.FileLayoutType.AllInOne:
                    builders.ForEach(b =>
                    {
                        watch.Restart();
                        b.GenerateAllInOne();
                        watch.Stop();
                        extensionsBuildTime.Add($"{b.GetType().Name}: {watch.Elapsed.TotalMilliseconds}");
                    });
                    break;
                case DocsConfiguration.FileLayoutType.SplitByNamespace:
                    builders.ForEach(b =>
                    {
                        watch.Restart();
                        b.GenerateSplitByNamespace();
                        watch.Stop();
                        extensionsBuildTime.Add($"{b.GetType().Name}: {watch.Elapsed.TotalMilliseconds}");
                    });
                    break;
                case DocsConfiguration.FileLayoutType.SplitByType:
                    builders.ForEach(b =>
                    {
                        watch.Restart();
                        b.GenerateSplitByType();
                        watch.Stop();
                        extensionsBuildTime.Add($"{b.GetType().Name}: {watch.Elapsed.TotalMilliseconds}");
                    });
                    break;
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
