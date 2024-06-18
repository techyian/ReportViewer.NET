using System.Collections.Generic;

namespace ReportViewer.NET.Extensions
{
    public static class DictionaryExtensions
    {
        // If SQL column has alias with spaces then we need to replace with underscore so expressions are parsed correctly.
        public static void ChangeDapperKeys(this IDictionary<string, object> dict)
        {
            foreach (KeyValuePair<string, object> kvp in dict)
            {
                if (kvp.Key.Contains(' '))
                {
                    dict.ChangeKey(kvp.Key, kvp.Key.Replace(' ', '_'));
                }
            }
        }

        // https://stackoverflow.com/a/15728577
        public static bool ChangeKey<TKey, TValue>(
            this IDictionary<TKey, TValue> dict,
            TKey oldKey, 
            TKey newKey
        )
        {
            TValue value;
            if (!dict.Remove(oldKey, out value))
                return false;

            dict[newKey] = value;
            return true;
        }
    }
}
