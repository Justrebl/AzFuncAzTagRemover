namespace Justrebl.Utils{
    public static class DateUtils{
        // Pattern Matching for DateTime comparison
        public static bool IsInTheFuture(this DateTime toCompare) => (toCompare, DateTime.Now) switch
        {
            var (date, now) when date > now => true,
            var (date, now) when date <= now => false,
            var (_, _) => throw new ArgumentException($"Invalid date comparison : {toCompare} cannot be compared to {DateTime.Now.ToString()}"),
        };
    }
}