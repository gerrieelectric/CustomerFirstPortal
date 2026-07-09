namespace Portal.Application.Infrastructure.Caching;

public class PhotoCacheOptions
{
    public int DurationMinutes { get; set; } = 60;
    public bool Enabled { get; set; } = true;
}
