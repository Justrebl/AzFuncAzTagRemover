namespace Justrebl.Utils
{
    public class NonCaseSensitiveKeyValuePairComparer : IEqualityComparer<KeyValuePair<string, string>>
    {
        public bool Equals(KeyValuePair<string, string> x, KeyValuePair<string, string> y)
        {
            //Compare a Key Value Pair in a non case sensitive manner and will return true if the key and value are the same
            return x.Key.Equals(y.Key, StringComparison.OrdinalIgnoreCase) 
                && x.Value.Equals(y.Value, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(KeyValuePair<string, string> obj)
        {
            return obj.Key.GetHashCode() ^ obj.Value.GetHashCode();
        }
    }
}