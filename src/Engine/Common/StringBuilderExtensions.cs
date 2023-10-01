using System.Text;

namespace DocsMaster.Engine.Common
{
    internal static class StringBuilderExtensions
    {
        public static StringBuilder TryAppend(this StringBuilder stringBuilder, string? valueToAdd)
        {
            if (valueToAdd != null && valueToAdd != string.Empty)
            {
                stringBuilder.Append(valueToAdd);
                stringBuilder.Append(' ');
            }

            return stringBuilder;
        }

        public static StringBuilder TryAppend(this StringBuilder stringBuilder, StringBuilder? valueToAdd)
        {
            if (valueToAdd != null)
            {
                stringBuilder.Append(valueToAdd);
                stringBuilder.Append(' ');
            }

            return stringBuilder;
        }

        public static void AppendWithSpace(this StringBuilder stringBuilder, char valueToAdd)
        {
            stringBuilder.Append(valueToAdd);
            stringBuilder.Append(' ');
        }

        public static void AppendWithSpace(this StringBuilder stringBuilder, string valueToAdd)
        {
            stringBuilder.Append(valueToAdd);
            stringBuilder.Append(' ');
        }

        public static void AppendWithSpace(this StringBuilder stringBuilder, StringBuilder valueToAdd)
        {
            stringBuilder.Append(valueToAdd);
            stringBuilder.Append(' ');
        }
    }
}
