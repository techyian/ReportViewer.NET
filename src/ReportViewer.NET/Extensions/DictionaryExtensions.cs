using System.Collections.Generic;

namespace ReportViewer.NET.Extensions
{
    public static class DictionaryExtensions
    {        
        public static void ChangeDapperKeys(this IDictionary<string, object> dict)
        {
            foreach (KeyValuePair<string, object> kvp in dict)
            {
                // To ensure we're able to identify columns correctly as part of the dynamic result set, we need to ensure all keys are lowercase.
                if (kvp.Key.Contains(' '))
                {
                    // If SQL column has alias with spaces then we need to replace with underscore so expressions are parsed correctly.
                    dict.ChangeKey(kvp.Key, kvp.Key.Replace(' ', '_').ToLower());
                }
                else
                {                    
                    dict.ChangeKey(kvp.Key, kvp.Key.ToLower());
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
