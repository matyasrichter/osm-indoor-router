namespace Settings;

public record Settings
{
    public required string DbConnectionString { get; init; }
}
