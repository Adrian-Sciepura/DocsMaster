namespace Documentation.Models.CodeElements
{
    internal interface IParentType
    {
        void AddInternalElement(CodeElement element);
        CodeElement GetElement();
        IParentType? GetParent();
    }
}
