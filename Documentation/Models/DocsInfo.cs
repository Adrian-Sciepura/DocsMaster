using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Documentation.Models
{
    internal class DocsInfo
    {
        private DocsConfiguration LoadConfig()
        {
            if (!Directory.Exists(DocsPath))
                Directory.CreateDirectory(DocsPath);
            if (!File.Exists(_docsConfigurationPath))
                CreateConfigFile();

            DocsConfiguration result;

            try
            {
                string jsonFile = File.ReadAllText(_docsConfigurationPath);
                result = JsonSerializer.Deserialize<DocsConfiguration>(jsonFile);
            }
            catch (Exception ex)
            {
                CreateConfigFile();
                result = DocsConfiguration.DefaultConfig();
            }

            return result;
        }

        private void CreateConfigFile()
        {
            var config = DocsConfiguration.DefaultConfig();

            var serializeOptions = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(config, serializeOptions);
            File.WriteAllText(_docsConfigurationPath, json);
        }


        private readonly string _docsConfigurationPath;
        public DocsConfiguration Configuration { get; }
        public string DocsPath { get; }
        public IEnumerable<Project> SolutionProjects { get; }

        public DocsInfo(IEnumerable<Project> solutionProjects, string docsPath)
        {
            SolutionProjects = solutionProjects;
            DocsPath = docsPath;
            _docsConfigurationPath = Path.Combine(docsPath, "config.json");
            Configuration = LoadConfig();
        }
    }
}
