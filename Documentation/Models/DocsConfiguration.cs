using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Documentation.Models
{
    internal class DocsConfiguration
    {
        public List<string> ExcludedProjects { get; set; }
        public List<SupportedExtensions> ExtensionsToGenerate { get; set; }
        public DocumentationType CreateDocsBy { get; set; }
        public FileLayoutType FileLayout { get; set; }


        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum DocumentationType
        {
            Namespaces,
            Projects
        }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum SupportedExtensions
        {
            html,
            md
        }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum FileLayoutType
        {
            AllInOne,
            SplitByNamespace,
            SplitByType,
            SplitEverything
        }

        public static DocsConfiguration DefaultConfig()
        {
            return new DocsConfiguration
            {
                ExcludedProjects = new List<string> { },
                ExtensionsToGenerate = new List<SupportedExtensions> { },
                CreateDocsBy = DocumentationType.Namespaces,
                FileLayout = FileLayoutType.AllInOne,
            };
        }
    }
}
