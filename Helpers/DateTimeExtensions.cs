namespace registroAsistencia.Helpers;

public static class DateTimeExtensions
{
    private static readonly TimeZoneInfo ChileTimeZone;

    static DateTimeExtensions()
    {
        try
        {
            ChileTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Santiago");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                ChileTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific SA Standard Time");
            }
            catch
            {
                ChileTimeZone = TimeZoneInfo.CreateCustomTimeZone(
                    "Chile Standard Time",
                    TimeSpan.FromHours(-4),
                    "Chile Standard Time",
                    "Chile Standard Time"
                );
            }
        }
    }

    public static DateTime ToChileTime(this DateTime utcDateTime)
    {
        if (utcDateTime.Kind != DateTimeKind.Utc)
        {
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, ChileTimeZone);
    }

    public static string ToChileDateString(this DateTime utcDateTime)
    {
        return utcDateTime.ToChileTime().ToString("dd/MM/yyyy");
    }

    public static string ToChileTimeString(this DateTime utcDateTime)
    {
        return utcDateTime.ToChileTime().ToString("HH:mm");
    }

    public static string ToChileDateTimeString(this DateTime utcDateTime)
    {
        return utcDateTime.ToChileTime().ToString("dd/MM/yyyy HH:mm");
    }

    public static string ToChileFullDateTimeString(this DateTime utcDateTime)
    {
        return utcDateTime.ToChileTime().ToString("dd/MM/yyyy HH:mm:ss");
    }
}
