namespace registroAsistencia.Services;

public interface ITimeZoneService
{
    DateTime GetChileNow();
    DateTime ConvertToChileTime(DateTime utcDateTime);
    DateTime ConvertToUtc(DateTime chileDateTime);
    string GetChileTimeZoneId();
}

public class TimeZoneService : ITimeZoneService
{
    private readonly TimeZoneInfo _chileTimeZone;

    public TimeZoneService()
    {
        // Chile usa "Pacific SA Standard Time" en Windows o "America/Santiago" en Linux/Mac
        try
        {
            _chileTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Santiago");
        }
        catch (TimeZoneNotFoundException)
        {
            // Fallback para Windows
            try
            {
                _chileTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific SA Standard Time");
            }
            catch
            {
                // Si tampoco existe, usar UTC-3/-4 manualmente
                _chileTimeZone = TimeZoneInfo.CreateCustomTimeZone(
                    "Chile Standard Time",
                    TimeSpan.FromHours(-4), // UTC-4 (horario de verano)
                    "Chile Standard Time",
                    "Chile Standard Time"
                );
            }
        }
    }

    public DateTime GetChileNow()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _chileTimeZone);
    }

    public DateTime ConvertToChileTime(DateTime utcDateTime)
    {
        if (utcDateTime.Kind != DateTimeKind.Utc)
        {
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, _chileTimeZone);
    }

    public DateTime ConvertToUtc(DateTime chileDateTime)
    {
        // Si la fecha viene sin especificar tipo, asumimos que es hora de Chile
        if (chileDateTime.Kind == DateTimeKind.Unspecified)
        {
            chileDateTime = DateTime.SpecifyKind(chileDateTime, DateTimeKind.Unspecified);
        }

        return TimeZoneInfo.ConvertTimeToUtc(chileDateTime, _chileTimeZone);
    }

    public string GetChileTimeZoneId()
    {
        return _chileTimeZone.Id;
    }
}
