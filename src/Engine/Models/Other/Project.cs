using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.IO;

namespace Documentation.Engine.Models.Other
{
    public class Project
    {
        public string Name { get; set; }
        public List<SyntaxTree> SyntaxTrees { get; set; }

        private Project(string name, List<SyntaxTree>? syntaxTrees = null)
        {
            Name = name;
            SyntaxTrees = syntaxTrees ?? new List<SyntaxTree>();
        }

        public static Project? CreateFromCsFiles(string projName, List<string> filePaths)
        {
            List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();

            foreach (string file in filePaths)
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(File.ReadAllText(file)));

            return syntaxTrees.Count == 0 ? null : new Project(projName, syntaxTrees);
        }
    }
}
