using System.Collections.Generic;

namespace DocsMaster.VSIX.Configuration
{
    internal class ConfigFile
    {
        public List<string> ExcludedProjects { get; set; }
        public List<SupportedExtensions> ExtensionsToGenerate { get; set; }
        public Dictionary<CodeElementType, bool> CodeElementsToGenerateInSeparateFile { get; set; }

        public static ConfigFile DefaultConfig()
        {
            return new ConfigFile
            {
                ExcludedProjects = new List<string>(),
                ExtensionsToGenerate = new List<SupportedExtensions>() { SupportedExtensions.md, SupportedExtensions.xml },
                CodeElementsToGenerateInSeparateFile = new Dictionary<CodeElementType, bool>()
                {
                    { CodeElementType.Namespace, false },
                    { CodeElementType.Interface, false },
                    { CodeElementType.Class, false },
                    { CodeElementType.Struct, false },
                    { CodeElementType.Record, false },
                    { CodeElementType.Enum, false },
                    { CodeElementType.Delegate, false },
                }
            };
        }

        public enum SupportedExtensions
        {
            xml,
            md
        }
    }
}
