namespace BackEnd.Services;

public class ADSettings
{
    public string Domain { get; set; } = string.Empty;
    public string[] PrimaryControllers { get; set; } = Array.Empty<string>();
    public string Container { get; set; } = string.Empty;
    public int RetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 2;
}