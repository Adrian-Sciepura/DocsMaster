using System.Collections.Generic;

namespace Documentation.Common
{
    internal static class DictionaryExtensions
    {
        public static TValue? GetValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue result;
            if (dictionary.TryGetValue(key, out result))
                return result;

            return default;
        }
    }
}
