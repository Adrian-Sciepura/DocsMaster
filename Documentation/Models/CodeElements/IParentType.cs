using Documentation.Models.CodeElements.Types;

namespace Documentation.Models.CodeElements
{
    internal interface IParentType
    {
        void AddInternalType(BaseCodeType internalType);

        string GetName();
        CodeElementType GetElementType();

        IParentType? GetParent();
    }
}
