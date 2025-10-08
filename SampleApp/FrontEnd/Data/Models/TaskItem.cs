namespace FrontEnd.Data.Models;

public class TaskItem
{
    public string TaskNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}