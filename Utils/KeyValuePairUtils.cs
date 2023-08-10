namespace Justrebl.Utils
{
    public class KeyValuePairComparer : IEqualityComparer<KeyValuePair<string, string>>
    {
        private bool _ignoreCase;
        public KeyValuePairComparer(bool ignoreCase)
        {
            _ignoreCase = ignoreCase;
        }

        public bool Equals(KeyValuePair<string, string> x, KeyValuePair<string, string> y)
        {
            if(_ignoreCase)
            {
                //Compare a Key Value Pair in a *non* case sensitive manner and will return true if the key and value are the same
                return x.Key.Equals(y.Key, StringComparison.OrdinalIgnoreCase) 
                    && x.Value.Equals(y.Value, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                //Compare a Key Value Pair in a case sensitive manner and will return true if the key and value are the same
                return x.Key.Equals(y.Key) && x.Value.Equals(y.Value);
            }                
        }

        public int GetHashCode(KeyValuePair<string, string> obj)
        {
            return obj.Key.GetHashCode() ^ obj.Value.GetHashCode();
        }
    }
}