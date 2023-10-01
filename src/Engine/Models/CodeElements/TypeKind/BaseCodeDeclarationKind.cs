namespace DocsMaster.Engine.Models.CodeElements.TypeKind
{
    internal abstract class BaseCodeDeclarationKind
    {
        protected BaseCodeDeclarationKind()
        {
        }

        public abstract string GetName();
        public abstract string? GetFullName();
        public abstract string? GetHash();
        public abstract void AddReference(CodeElement codeElement);
        public abstract CodeElement GetTypeReference();
    }
}
