using System.Collections.Generic;

namespace Documentation.Engine.Models.CodeDocs
{
    internal class CodeDocumentation
    {
        public CodeDocumentationElement? Summary { get; set; }
        public CodeDocumentationElement? Remarks { get; set; }
        public CodeDocumentationElement? Returns { get; set; }
        public List<CodeDocumentationElement> Parameters { get; set; }
        public List<CodeDocumentationElement> Exceptions { get; set; }
        public List<CodeDocumentationElement> Examples { get; set; }
        public List<CodeDocumentationElement> SeeAlsos { get; set; }
        public bool Skip { get; set; }

        public CodeDocumentation()
        {
            Parameters = new List<CodeDocumentationElement>();
            Exceptions = new List<CodeDocumentationElement>();
            Examples = new List<CodeDocumentationElement>();
            SeeAlsos = new List<CodeDocumentationElement>();
            Skip = false;
        }
    }
}
