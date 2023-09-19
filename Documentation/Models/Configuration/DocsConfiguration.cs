using Documentation.Models.CodeElements;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Documentation.Models
{
    internal class DocsConfiguration
    {
        public DocumentationType CreateDocsBy { get; set; }
        public List<string> ExcludedProjects { get; set; }
        public List<SupportedExtensions> ExtensionsToGenerate { get; set; }
        public Dictionary<CodeElementType, bool> CodeElementsToGenerateInSeparateFile { get; set; }

        public enum DocumentationType
        {
            Namespaces,
            Projects
        }

        public enum SupportedExtensions
        {
            xml,
            md
        }


        public static DocsConfiguration DefaultConfig()
        {
            return new DocsConfiguration
            {
                CreateDocsBy = DocumentationType.Namespaces,
                ExcludedProjects = new List<string> { },
                ExtensionsToGenerate = new List<SupportedExtensions> { },
                CodeElementsToGenerateInSeparateFile = new Dictionary<CodeElementType, bool>()
                {
                    { CodeElementType.Namespace, false },
                    { CodeElementType.Interface, false },
                    { CodeElementType.Class, false },
                    { CodeElementType.Struct, false },
                    { CodeElementType.Record, false },
                    { CodeElementType.Enum, false },
                    { CodeElementType.Delegate, false },
                    { CodeElementType.Method, false },
                    { CodeElementType.Constructor, false },
                    { CodeElementType.Destructor, false },
                    { CodeElementType.Operator, false },
                    { CodeElementType.Property, false },
                    { CodeElementType.Variable, false },

                },
            };
        }
    }
}
