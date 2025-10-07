namespace BackEnd.Models;

public class ADResource
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public List<string> Members { get; set; } = new();
}