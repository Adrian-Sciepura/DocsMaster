using Microsoft.VisualStudio.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Documentation.Common
{
    internal static class DictionaryExtensions
    {
        public static TValue? GetValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue result;
            if(dictionary.TryGetValue(key, out result))
                return result;

            return default;
        }
    }
}
