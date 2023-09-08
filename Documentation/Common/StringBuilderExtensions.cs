using System.Text;

namespace Documentation.Common
{
    internal static class StringBuilderExtensions
    {
        public static void TryAppend(this StringBuilder stringBuilder, string? valueToAdd)
        {
            if (valueToAdd != null && valueToAdd != string.Empty)
            {
                stringBuilder.Append(valueToAdd);
                stringBuilder.Append(' ');
            }
        }

        public static void TryAppend(this StringBuilder stringBuilder, StringBuilder? valueToAdd)
        {
            if (valueToAdd != null)
            {
                stringBuilder.Append(valueToAdd);
                stringBuilder.Append(' ');
            }
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
