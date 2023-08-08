namespace Justrebl.Utils

{
    public static class BoolUtils
    {
        public static bool ParseBool(this string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (bool.TryParse(value, out bool result))
            {
                return result;
            }

            throw new ArgumentException($"Unable to parse '{value}' as a boolean value.");
        }
    }
}