namespace AgroControl.Application.Common;

public static class DatabaseTimestamp
{
    public static DateTime UtcNow() => NormalizeUtc(DateTime.UtcNow);

    public static DateTime NormalizeUtc(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        return new DateTime(utc.Ticks - (utc.Ticks % 10), DateTimeKind.Utc);
    }
}
