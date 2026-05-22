using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TaskHub
{
    // ==================== ENUMS ====================
    public enum Priority
    {
        Low,
        Medium,
        High
    }

    public enum Status
    {
        New,
        InProgress,
        Done
    }

    // ==================== ENTITY ====================
    public interface IEntity
    {
        int Id { get; set; }  // Исправлено: добавлен set
    }

    public class TaskItem : IEntity, IDisposable
    {
        private bool _disposed = false;

        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Priority Priority { get; set; }
        public DateTime Deadline { get; set; }
        public Status Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public TaskItem()
        {
            CreatedAt = DateTime.Now;
        }

        public bool IsOverdue()
        {
            return Deadline < DateTime.Now && Status != Status.Done;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Console.WriteLine($"[DISPOSE] Task '{Title}' (ID: {Id}) has been disposed.");
            }
            GC.SuppressFinalize(this);
        }

        ~TaskItem()
        {
            if (!_disposed)
            {
                Console.WriteLine($"[FINALIZER] Task '{Title}' (ID: {Id}) was finalized.");
            }
        }

        public override string ToString()
        {
            string overdueMark = IsOverdue() ? " ⚠️ OVERDUE!" : "";
            return $"[{Id}] {Title} | Priority: {Priority} | Status: {Status} | Deadline: {Deadline:yyyy-MM-dd HH:mm}{overdueMark}";
        }
    }

    // ==================== REPOSITORY ====================
    public class Repository<T> where T : IEntity
    {
        private readonly Dictionary<int, T> _items = new Dictionary<int, T>();
        private int _nextId = 1;

        public void Add(T item)
        {
            if (_items.ContainsKey(item.Id))
                throw new InvalidOperationException($"Item with Id {item.Id} already exists");

            if (item.Id == 0)
            {
                item.Id = _nextId++;
            }
            else
            {
                _nextId = Math.Max(_nextId, item.Id + 1);
            }

            _items[item.Id] = item;
        }

        public bool Remove(int id) => _items.Remove(id);

        public T GetById(int id) => _items.TryGetValue(id, out var item) ? item : default;

        public IReadOnlyList<T> GetAll() => _items.Values.ToList().AsReadOnly();

        public int Count => _items.Count;

        public IReadOnlyList<T> Find(Predicate<T> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return _items.Values.Where(item => predicate(item)).ToList().AsReadOnly();
        }

        public void Update(int id, T updatedItem)
        {
            if (!_items.ContainsKey(id))
                throw new KeyNotFoundException($"Item with Id {id} not found");

            updatedItem.Id = id;
            _items[id] = updatedItem;
        }
    }

    // ==================== DELEGATES ====================
    public delegate void TaskNotification(string message);
    public delegate bool TaskFilter(TaskItem task);

    // ==================== STATIC CLASS ====================
    public static class TaskStatistics
    {
        public static int GetTotalCount(Repository<TaskItem> repository) => repository.Count;

        public static int GetCompletedCount(Repository<TaskItem> repository)
        {
            return repository.Find(t => t.Status == Status.Done).Count;
        }

        public static int GetOverdueCount(Repository<TaskItem> repository)
        {
            return repository.Find(t => t.IsOverdue()).Count;
        }

        public static Dictionary<Priority, int> GetPriorityStats(Repository<TaskItem> repository)
        {
            var stats = new Dictionary<Priority, int>
            {
                { Priority.Low, 0 },
                { Priority.Medium, 0 },
                { Priority.High, 0 }
            };

            foreach (var task in repository.GetAll())
            {
                stats[task.Priority]++;
            }

            return stats;
        }

        public static void PrintStatistics(Repository<TaskItem> repository)
        {
            Console.WriteLine("\n========== STATISTICS ==========");
            Console.WriteLine($"Total tasks: {GetTotalCount(repository)}");
            Console.WriteLine($"Completed tasks: {GetCompletedCount(repository)}");
            Console.WriteLine($"Overdue tasks: {GetOverdueCount(repository)}");

            var priorityStats = GetPriorityStats(repository);
            Console.WriteLine("\nBy priority:");
            foreach (var stat in priorityStats)
            {
                Console.WriteLine($"  {stat.Key}: {stat.Value}");
            }
            Console.WriteLine("================================\n");
        }
    }

    // ==================== FILE MANAGER (Async) ====================
    public static class FileManager
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions { WriteIndented = true };

        public static async Task SaveToFileAsync(Repository<TaskItem> repository, string filePath)
        {
            try
            {
                var tasks = repository.GetAll().ToList();
                string json = JsonSerializer.Serialize(tasks, _options);
                await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
                Console.WriteLine($"✓ Tasks successfully saved to {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error saving tasks: {ex.Message}");
                throw;
            }
        }

        public static async Task LoadFromFileAsync(Repository<TaskItem> repository, string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"File {filePath} not found. Starting with empty repository.");
                    return;
                }

                string json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                var tasks = JsonSerializer.Deserialize<List<TaskItem>>(json);

                if (tasks != null)
                {
                    foreach (var task in tasks)
                    {
                        repository.Add(task);
                    }
                    Console.WriteLine($"✓ Tasks successfully loaded from {filePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error loading tasks: {ex.Message}");
                throw;
            }
        }
    }

    // ==================== BACKGROUND SERVICE ====================
    public class DeadlineMonitor : IDisposable
    {
        private readonly Repository<TaskItem> _repository;
        private readonly TaskNotification _notificationCallback;
        private CancellationTokenSource _cts;
        private Task _monitorTask;
        private bool _disposed;

        public DeadlineMonitor(Repository<TaskItem> repository, TaskNotification notificationCallback)
        {
            _repository = repository;
            _notificationCallback = notificationCallback;
        }

        public void Start(int checkIntervalSeconds = 10)
        {
            _cts = new CancellationTokenSource();
            _monitorTask = Task.Run(() => MonitorLoop(checkIntervalSeconds, _cts.Token));
            Console.WriteLine($"Deadline monitor started (checking every {checkIntervalSeconds} seconds)");
        }

        public void Stop()
        {
            _cts?.Cancel();
            _monitorTask?.Wait(5000);
            Console.WriteLine("Deadline monitor stopped");
        }

        private async Task MonitorLoop(int intervalSeconds, CancellationToken token)
        {
            var lastNotifiedTasks = new HashSet<int>();

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var overdueTasks = _repository.Find(t => t.IsOverdue());
                    var newOverdueTasks = overdueTasks.Where(t => !lastNotifiedTasks.Contains(t.Id)).ToList();

                    foreach (var task in newOverdueTasks)
                    {
                        string message = $"⚠️ ALERT: Task '{task.Title}' (ID: {task.Id}) is OVERDUE! Deadline was {task.Deadline:yyyy-MM-dd HH:mm}";
                        _notificationCallback?.Invoke(message);
                        lastNotifiedTasks.Add(task.Id);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _notificationCallback?.Invoke($"Error in monitor: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                _cts?.Dispose();
                _disposed = true;
            }
        }
    }

    // ==================== TASK MANAGER ====================
    public class TaskManager
    {
        private readonly Repository<TaskItem> _repository;
        private readonly DeadlineMonitor _monitor;

        public event TaskNotification OnTaskAdded;
        public event TaskNotification OnTaskDeleted;
        public event TaskNotification OnTaskUpdated;

        public TaskManager()
        {
            _repository = new Repository<TaskItem>();
            _monitor = new DeadlineMonitor(_repository, ShowNotification);
            _monitor.Start(15);
        }

        private void ShowNotification(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n{DateTime.Now:HH:mm:ss} - {message}");
            Console.ResetColor();
        }

        public void AddTask(TaskItem task)
        {
            _repository.Add(task);
            OnTaskAdded?.Invoke($"Task '{task.Title}' created successfully!");
        }

        public bool DeleteTask(int id)
        {
            var task = _repository.GetById(id);
            if (task != null)
            {
                bool removed = _repository.Remove(id);
                if (removed)
                {
                    task.Dispose();
                    OnTaskDeleted?.Invoke($"Task '{task.Title}' deleted successfully!");
                }
                return removed;
            }
            return false;
        }

        public bool UpdateTask(int id, TaskItem updatedTask)
        {
            try
            {
                var existingTask = _repository.GetById(id);
                if (existingTask == null) return false;

                _repository.Update(id, updatedTask);
                OnTaskUpdated?.Invoke($"Task '{updatedTask.Title}' updated successfully!");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public TaskItem GetTask(int id) => _repository.GetById(id);
        public IReadOnlyList<TaskItem> GetAllTasks() => _repository.GetAll();
        public IReadOnlyList<TaskItem> GetTasksByFilter(TaskFilter filter) => _repository.Find(t => filter(t));
        
        public IReadOnlyList<TaskItem> SearchByTitle(string title) 
            => _repository.Find(t => t.Title.ToLower().Contains(title.ToLower()));
        
        public IReadOnlyList<TaskItem> SearchByStatus(Status status) 
            => _repository.Find(t => t.Status == status);
        
        public IReadOnlyList<TaskItem> SearchByPriority(Priority priority) 
            => _repository.Find(t => t.Priority == priority);

        public Repository<TaskItem> GetRepository() => _repository;

        public void Dispose()
        {
            _monitor.Dispose();
        }
    }

    // ==================== UI HELPER ====================
    public static class ConsoleUI
    {
        public static void ShowMenu()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║            TASKHUB MANAGER             ║");
            Console.WriteLine("╠════════════════════════════════════════╣");
            Console.WriteLine("║  1. Create new task                    ║");
            Console.WriteLine("║  2. View all tasks                     ║");
            Console.WriteLine("║  3. View tasks by status               ║");
            Console.WriteLine("║  4. View high priority tasks           ║");
            Console.WriteLine("║  5. Edit task                          ║");
            Console.WriteLine("║  6. Delete task                        ║");
            Console.WriteLine("║  7. Search tasks                       ║");
            Console.WriteLine("║  8. Show statistics                    ║");
            Console.WriteLine("║  9. Save to file                       ║");
            Console.WriteLine("║ 10. Load from file                     ║");
            Console.WriteLine("║  0. Exit                               ║");
            Console.WriteLine("╚════════════════════════════════════════╝");
            Console.ResetColor();
            Console.Write("\nSelect option: ");
        }

        public static TaskItem CreateTaskFromInput(int id = 0)
        {
            var task = new TaskItem();
            if (id > 0) task.Id = id;

            Console.Write("Title: ");
            task.Title = Console.ReadLine();

            Console.Write("Description: ");
            task.Description = Console.ReadLine();

            Console.Write("Priority (Low/Medium/High): ");
            string priorityInput = Console.ReadLine();
            task.Priority = priorityInput?.ToLower() switch
            {
                "low" => Priority.Low,
                "medium" => Priority.Medium,
                "high" => Priority.High,
                _ => Priority.Medium
            };

            Console.Write("Deadline (yyyy-MM-dd HH:mm): ");
            if (DateTime.TryParse(Console.ReadLine(), out DateTime deadline))
                task.Deadline = deadline;
            else
                task.Deadline = DateTime.Now.AddDays(7);

            Console.Write("Status (New/InProgress/Done): ");
            string statusInput = Console.ReadLine();
            task.Status = statusInput?.ToLower() switch
            {
                "new" => Status.New,
                "inprogress" => Status.InProgress,
                "done" => Status.Done,
                _ => Status.New
            };

            return task;
        }

        public static void ShowTasks(IReadOnlyList<TaskItem> tasks, string title)
        {
            Console.WriteLine($"\n========== {title} ==========");
            if (tasks.Count == 0)
            {
                Console.WriteLine("No tasks found.");
            }
            else
            {
                foreach (var task in tasks)
                {
                    if (task.IsOverdue())
                        Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(task);
                    Console.ResetColor();
                    Console.WriteLine($"  Description: {task.Description}");
                    Console.WriteLine($"  Created: {task.CreatedAt:yyyy-MM-dd HH:mm}");
                    Console.WriteLine();
                }
            }
            Console.WriteLine($"Total: {tasks.Count}");
            Console.WriteLine("================================\n");
        }
    }

    // ==================== MAIN PROGRAM ====================
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Title = "TaskHub Manager";
            var taskManager = new TaskManager();

            // Subscribe to events
            taskManager.OnTaskAdded += (msg) => 
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n✓ {msg}");
                Console.ResetColor();
            };

            taskManager.OnTaskDeleted += (msg) => 
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n✓ {msg}");
                Console.ResetColor();
            };

            taskManager.OnTaskUpdated += (msg) => 
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"\n✓ {msg}");
                Console.ResetColor();
            };

            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("\n\nSaving tasks before exit...");
                taskManager.Dispose();
            };

            bool running = true;

            while (running)
            {
                ConsoleUI.ShowMenu();
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1": // Create task
                        Console.Clear();
                        Console.WriteLine("=== CREATE NEW TASK ===\n");
                        var newTask = ConsoleUI.CreateTaskFromInput();
                        taskManager.AddTask(newTask);
                        Console.WriteLine("\nPress any key to continue...");
                        Console.ReadKey();
                        break;

                    case "2": // View all tasks
                        Console.Clear();
                        ConsoleUI.ShowTasks(taskManager.GetAllTasks(), "ALL TASKS");
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                        break;

                    case "3": // View by status
                        Console.Clear();
                        Console.WriteLine("=== VIEW TASKS BY STATUS ===\n");
                        Console.WriteLine("1. New");
                        Console.WriteLine("2. InProgress");
                        Console.WriteLine("3. Done");
                        Console.Write("Select status: ");
                        string statusChoice = Console.ReadLine();
                        
                        Status selectedStatus = statusChoice switch
                        {
                            "1" => Status.New,
                            "2" => Status.InProgress,
                            "3" => Status.Done,
                            _ => Status.New
                        };
                        
                        var statusTasks = taskManager.GetTasksByFilter(t => t.Status == selectedStatus);
                        ConsoleUI.ShowTasks(statusTasks, $"{selectedStatus} TASKS");
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                        break;

                    case "4": // View high priority
                        Console.Clear();
                        var highPriorityTasks = taskManager.GetTasksByFilter(t => t.Priority == Priority.High);
                        ConsoleUI.ShowTasks(highPriorityTasks, "HIGH PRIORITY TASKS");
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                        break;

                    case "5": // Edit task
                        Console.Clear();
                        Console.WriteLine("=== EDIT TASK ===\n");
                        Console.Write("Enter task ID to edit: ");
                        if (int.TryParse(Console.ReadLine(), out int editId))
                        {
                            var existingTask = taskManager.GetTask(editId);
                            if (existingTask != null)
                            {
                                Console.WriteLine($"\nEditing: {existingTask}");
                                Console.WriteLine("\nEnter new values (leave empty to keep current):\n");
                                
                                Console.Write($"Title [{existingTask.Title}]: ");
                                string title = Console.ReadLine();
                                
                                Console.Write($"Description [{existingTask.Description}]: ");
                                string description = Console.ReadLine();
                                
                                Console.Write($"Priority (Low/Medium/High) [{existingTask.Priority}]: ");
                                string priority = Console.ReadLine();
                                
                                Console.Write($"Status (New/InProgress/Done) [{existingTask.Status}]: ");
                                string status = Console.ReadLine();
                                
                                var updatedTask = new TaskItem
                                {
                                    Id = editId,
                                    Title = string.IsNullOrWhiteSpace(title) ? existingTask.Title : title,
                                    Description = string.IsNullOrWhiteSpace(description) ? existingTask.Description : description,
                                    Priority = priority?.ToLower() switch
                                    {
                                        "low" => Priority.Low,
                                        "medium" => Priority.Medium,
                                        "high" => Priority.High,
                                        _ => existingTask.Priority
                                    },
                                    Status = status?.ToLower() switch
                                    {
                                        "new" => Status.New,
                                        "inprogress" => Status.InProgress,
                                        "done" => Status.Done,
                                        _ => existingTask.Status
                                    },
                                    Deadline = existingTask.Deadline,
                                    CreatedAt = existingTask.CreatedAt
                                };
                                
                                taskManager.UpdateTask(editId, updatedTask);
                            }
                            else
                            {
                                Console.WriteLine("Task not found!");
                            }
                        }
                        Console.WriteLine("\nPress any key to continue...");
                        Console.ReadKey();
                        break;

                    case "6": // Delete task
                        Console.Clear();
                        Console.WriteLine("=== DELETE TASK ===\n");
                        Console.Write("Enter task ID to delete: ");
                        if (int.TryParse(Console.ReadLine(), out int deleteId))
                        {
                            if (taskManager.DeleteTask(deleteId))
                            {
                                Console.WriteLine("Task deleted successfully!");
                            }
                            else
                            {
                                Console.WriteLine("Task not found!");
                            }
                        }
                        Console.WriteLine("\nPress any key to continue...");
                        Console.ReadKey();
                        break;

                    case "7": // Search tasks
                        Console.Clear();
                        Console.WriteLine("=== SEARCH TASKS ===\n");
                        Console.WriteLine("1. Search by title");
                        Console.WriteLine("2. Search by status");
                        Console.WriteLine("3. Search by priority");
                        Console.Write("Select search type: ");
                        string searchType = Console.ReadLine();
                        
                        IReadOnlyList<TaskItem> searchResults = null;
                        
                        switch (searchType)
                        {
                            case "1":
                                Console.Write("Enter title keyword: ");
                                string titleKeyword = Console.ReadLine();
                                searchResults = taskManager.SearchByTitle(titleKeyword);
                                break;
                            case "2":
                                Console.Write("Enter status (New/InProgress/Done): ");
                                if (Enum.TryParse<Status>(Console.ReadLine(), true, out Status searchStatus))
                                    searchResults = taskManager.SearchByStatus(searchStatus);
                                break;
                            case "3":
                                Console.Write("Enter priority (Low/Medium/High): ");
                                if (Enum.TryParse<Priority>(Console.ReadLine(), true, out Priority searchPriority))
                                    searchResults = taskManager.SearchByPriority(searchPriority);
                                break;
                        }
                        
                        if (searchResults != null)
                            ConsoleUI.ShowTasks(searchResults, "SEARCH RESULTS");
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                        break;

                    case "8": // Statistics
                        Console.Clear();
                        TaskStatistics.PrintStatistics(taskManager.GetRepository());
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                        break;

                    case "9": // Save to file
                        Console.Clear();
                        Console.WriteLine("=== SAVE TO FILE ===\n");
                        Console.Write("Enter filename (default: tasks.json): ");
                        string saveFile = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(saveFile)) saveFile = "tasks.json";
                        
                        try
                        {
                            await FileManager.SaveToFileAsync(taskManager.GetRepository(), saveFile);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error: {ex.Message}");
                        }
                        Console.WriteLine("\nPress any key to continue...");
                        Console.ReadKey();
                        break;

                    case "10": // Load from file
                        Console.Clear();
                        Console.WriteLine("=== LOAD FROM FILE ===\n");
                        Console.Write("Enter filename (default: tasks.json): ");
                        string loadFile = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(loadFile)) loadFile = "tasks.json";
                        
                        try
                        {
                            await FileManager.LoadFromFileAsync(taskManager.GetRepository(), loadFile);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error: {ex.Message}");
                        }
                        Console.WriteLine("\nPress any key to continue...");
                        Console.ReadKey();
                        break;

                    case "0": // Exit
                        Console.WriteLine("\nSaving tasks before exit...");
                        await FileManager.SaveToFileAsync(taskManager.GetRepository(), "tasks_autosave.json");
                        taskManager.Dispose();
                        running = false;
                        Console.WriteLine("Goodbye!");
                        break;

                    default:
                        Console.WriteLine("Invalid option! Press any key...");
                        Console.ReadKey();
                        break;
                }
            }
        }
    }
}
