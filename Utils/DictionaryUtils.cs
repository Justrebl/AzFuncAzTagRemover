namespace Justrebl.Utils
{
    public static class DictionaryExtension
    {
        public static bool ContainsKey<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, bool ignoreCase)
        {
            bool? keyExists;

            var keyString = key as string;
            if (keyString != null && ignoreCase)
            {
                // Key is a string and case must be ignored
                // Using string.Equals to perform case insensitive comparison of the dictionary key.
                keyExists =
                    dictionary.Keys.OfType<string>()
                    .Any(k => string.Equals(k, keyString, StringComparison.InvariantCultureIgnoreCase));
            }
            else
            {
                // Key is any other type, use default comparison.
                keyExists = dictionary.ContainsKey(key);
            }

            return keyExists ?? false;
        }

        public static bool ContainsKey<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, bool ignoreCase, out string actualDeleteByKey)
        {
            //If the key to look for is null, then we return false
            if (key == null)
            {
                actualDeleteByKey = String.Empty;
                return false;
            }

            bool? keyExists;

            var keyString = key as string;
            if (keyString != null && ignoreCase)
            {
                // Key is a string and case must be ignored
                // Using string.Equals to perform case insensitive comparison of the dictionary key.
                var retrievedTags = dictionary.Keys.OfType<string>()
                    .Where(k => string.Equals(k, keyString, StringComparison.InvariantCultureIgnoreCase));

                //If the dictionnary contains any equivalent key, non case sensitive, then we say it exists.
                keyExists = retrievedTags.Any();

                //And we return the first occurence of the tag key, in it's original form to help retrieving its value in the original dictionnary
                actualDeleteByKey = retrievedTags?.FirstOrDefault(String.Empty) ?? String.Empty;
            }
            else
            {
                // Key is any other type, use default comparison.
                keyExists = dictionary.ContainsKey(key);

                //Provide the Original Dictionary Key if it exists, or a String.Empty if it doesn't
                actualDeleteByKey = keyExists != null && keyExists.Value ? key.ToString() : String.Empty;
            }

            return keyExists ?? false;
        }
    }
}