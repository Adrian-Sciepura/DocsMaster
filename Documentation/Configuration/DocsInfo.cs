using Documentation.VSIX.Common;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using EngineProject = Documentation.Engine.Models.Other.Project;

namespace Documentation.VSIX.Configuration
{
    internal class DocsInfo
    {
        private string _docsPath;
        private string _docsConfigurationFilePath;
        private List<Project> _solutionProjects;
        private ConfigFile _configFile;

        private void CreateNewConfigFile()
        {
            ConfigFile newConfig = ConfigFile.DefaultConfig();

            var serializeOptions = new JsonSerializerOptions { WriteIndented = true };
            serializeOptions.Converters.Add(new JsonStringEnumConverter());

            string json = JsonSerializer.Serialize(newConfig, serializeOptions);
            File.WriteAllText(_docsConfigurationFilePath, json);
        }

        private ConfigFile LoadConfigFile()
        {
            if (!Directory.Exists(_docsPath))
                Directory.CreateDirectory(_docsPath);
            if (!File.Exists(_docsConfigurationFilePath))
                CreateNewConfigFile();

            ConfigFile result;

            try
            {
                string jsonFile = File.ReadAllText(_docsConfigurationFilePath);
                var serializeOptions = new JsonSerializerOptions();
                serializeOptions.Converters.Add(new JsonStringEnumConverter());
                serializeOptions.Converters.Add(new TypeSetupJsonConverter());
                result = JsonSerializer.Deserialize<ConfigFile>(jsonFile, serializeOptions);
            }
            catch (Exception)
            {
                CreateNewConfigFile();
                result = ConfigFile.DefaultConfig();
            }

            return result;
        }

        private EngineProject? MapProject(Project project)
        {
            List<string> filePaths = new List<string>();

            foreach (var projectChild in project.Children)
                GetChilds(projectChild);

            void GetChilds(SolutionItem item)
            {
                switch (item.Type)
                {
                    case SolutionItemType.PhysicalFile:
                        if (Path.GetExtension(item.Name) == ".cs")
                            filePaths.Add(item.Name);
                        break;
                    case SolutionItemType.PhysicalFolder:
                    case SolutionItemType.VirtualFolder:
                        foreach (var child in item.Children)
                            GetChilds(child);
                        break;
                }
            }

            return filePaths.Count > 0 ? EngineProject.CreateFromCsFiles(project.Name, filePaths) : null;
        }

        private List<Engine.Configuration.DocsInfo.SupportedExtensions> MapSupportedExtensions(List<ConfigFile.SupportedExtensions> supportedExtensions)
        {
            var result = new List<Engine.Configuration.DocsInfo.SupportedExtensions>();

            Engine.Configuration.DocsInfo.SupportedExtensions temp;

            foreach (var element in supportedExtensions)
                if (Enum.TryParse(element.ToString(), false, out temp))
                    result.Add(temp);

            return result;
        }

        private Dictionary<Engine.Models.CodeElements.CodeElementType, bool> MapElementsToGenerateInSeparateFile(Dictionary<CodeElementType, bool> elementsToGenerateInSeparateFile)
        {
            var result = new Dictionary<Engine.Models.CodeElements.CodeElementType, bool>();

            Engine.Models.CodeElements.CodeElementType temp;

            foreach (var element in elementsToGenerateInSeparateFile)
                if (Enum.TryParse(element.Key.ToString(), false, out temp))
                    result.Add(temp, element.Value);

            foreach (var element in Enum.GetValues(typeof(Engine.Models.CodeElements.CodeElementType)))
            {
                var convertedElement = (Engine.Models.CodeElements.CodeElementType)element;
                if (!result.ContainsKey(convertedElement))
                    result.Add(convertedElement, false);
            }

            return result;
        }

        public DocsInfo(string solutionPath, List<Project> solutionProjects)
        {
            _docsPath = Path.Combine(solutionPath, "docs");
            _docsConfigurationFilePath = Path.Combine(_docsPath, "config.json");
            _solutionProjects = solutionProjects;
            _configFile = LoadConfigFile();
        }

        public Engine.Configuration.DocsInfo Map()
        {
            var projects = new List<EngineProject>();

            EngineProject engineProject;
            foreach (var project in _solutionProjects)
            {
                if (_configFile.ExcludedProjects.Contains(project.Name))
                    continue;

                engineProject = MapProject(project);
                if (engineProject != null)
                    projects.Add(engineProject);
            }


            return new Engine.Configuration.DocsInfo(
                docsPath: _docsPath,
                solutionProjects: projects,
                extensionsToGenerate: MapSupportedExtensions(_configFile.ExtensionsToGenerate),
                codeElementsToGenerateInSeparateFile: MapElementsToGenerateInSeparateFile(_configFile.CodeElementsToGenerateInSeparateFile));
        }
    }
}
