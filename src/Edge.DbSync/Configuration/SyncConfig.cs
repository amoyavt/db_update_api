namespace Edge.DbSync.Configuration;

public class SyncConfig
{
    public const string SectionName = "SyncConfig";

    public int IntervalMinutes { get; set; } = 5;
}