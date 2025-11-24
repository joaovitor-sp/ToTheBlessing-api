namespace ToTheBlessing.Helpers
{
    public static class DateTimeHelper
    {
        public static DateTime EnsureUtc(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }
            else if (dateTime.Kind == DateTimeKind.Local)
            {
                return dateTime.ToUniversalTime();
            }
            else
            {
                return dateTime;
            }
        }
    }
}
