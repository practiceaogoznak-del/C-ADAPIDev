using FrontEnd.Data.Models;

namespace FrontEnd.Data.Services;

public class TaskService
{
    private List<FrontEnd.Data.Models.TaskItem> _tasks = new();

    public TaskService()
    {
        // Демо-данные
        _tasks = new List<TaskItem>
        {
            new TaskItem 
            { 
                TaskNumber = "T-001",
                Title = "Обновление системы безопасности",
                Status = "В работе",
                Priority = "Высокий",
                DueDate = DateTime.Now.AddDays(5),
                AssignedTo = "Иванов И.И.",
                Description = "Обновление системы безопасности до последней версии"
            },
            new TaskItem 
            { 
                TaskNumber = "T-002",
                Title = "Проверка оборудования",
                Status = "Новая",
                Priority = "Средний",
                DueDate = DateTime.Now.AddDays(10),
                AssignedTo = "Петров П.П.",
                Description = "Ежемесячная проверка оборудования"
            },
            new TaskItem 
            { 
                TaskNumber = "T-003",
                Title = "Подготовка отчетности",
                Status = "Завершена",
                Priority = "Низкий",
                DueDate = DateTime.Now.AddDays(-1),
                AssignedTo = "Сидоров С.С.",
                Description = "Подготовка ежемесячного отчета"
            }
        };
    }

    public IEnumerable<FrontEnd.Data.Models.TaskItem> GetTasks() => _tasks;

    public FrontEnd.Data.Models.TaskItem? GetTask(string taskNumber) => 
        _tasks.FirstOrDefault(t => t.TaskNumber == taskNumber);

    public void AddTask(FrontEnd.Data.Models.TaskItem task)
    {
        task.TaskNumber = $"T-{(_tasks.Count + 1):D3}";
        _tasks.Add(task);
    }

    public void UpdateTask(FrontEnd.Data.Models.TaskItem task)
    {
        var index = _tasks.FindIndex(t => t.TaskNumber == task.TaskNumber);
        if (index != -1)
        {
            _tasks[index] = task;
        }
    }

    public void DeleteTask(string taskNumber)
    {
        var task = GetTask(taskNumber);
        if (task != null)
        {
            _tasks.Remove(task);
        }
    }
}