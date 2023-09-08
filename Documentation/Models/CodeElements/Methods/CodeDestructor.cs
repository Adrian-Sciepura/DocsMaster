using Documentation.Common;
using Documentation.Models.CodeElements.Documentation;
using Documentation.Models.CodeElements.TypeKind;
using Documentation.Models.CodeElements.Types;
using Microsoft.VisualStudio.Package;
using System.Text;
using System.Xml.Linq;

namespace Documentation.Models.CodeElements.Methods
{
    internal class CodeDestructor : BaseCodeMethod
    {
        public CodeDestructor(IParentType parent, CodeRegularDeclaration declaration, CodeDocumentation documentation = null) :
            base(parent, CodeElementType.Destructor, declaration, null, documentation)
        {
        }
    }
}
