using System;
using System.Collections.Generic;
using System.Linq;

namespace Task1
{
    public interface IEntity
    {
        int Id { get; }
    }

    public class Repository<T> where T : IEntity
    {
        private readonly Dictionary<int, T> _items = new Dictionary<int, T>();

        public void Add(T item)
        {
            if (_items.ContainsKey(item.Id))
                throw new InvalidOperationException($"Item with Id {item.Id} already exists");
            
            _items[item.Id] = item;
        }

        public bool Remove(int id)
        {
            return _items.Remove(id);
        }

        public T? GetById(int id)
        {
            _items.TryGetValue(id, out var item);
            return item;
        }

        public IReadOnlyList<T> GetAll()
        {
            return _items.Values.ToList().AsReadOnly();
        }

        public int Count => _items.Count;

        public IReadOnlyList<T> Find(Predicate<T> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            
            return _items.Values.Where(item => predicate(item)).ToList().AsReadOnly();
        }
    }

    // Тестовые классы
    public class Product : IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }

        public override string ToString() => $"Product[Id={Id}, Name={Name}, Price={Price:C}]";
    }

    public class User : IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }

        public override string ToString() => $"User[Id={Id}, Name={Name}, Email={Email}]";
    }

    // Проверка
    class Program
    {
        static void Main()
        {
            Console.WriteLine("=== Task 1: Generic Repository ===\n");
            
            // Работа с Product
            var productRepo = new Repository<Product>();
            
            productRepo.Add(new Product { Id = 1, Name = "Laptop", Price = 1200m });
            productRepo.Add(new Product { Id = 2, Name = "Mouse", Price = 25m });
            productRepo.Add(new Product { Id = 3, Name = "Keyboard", Price = 80m });
            
            Console.WriteLine("All products:");
            foreach (var product in productRepo.GetAll())
                Console.WriteLine($"  {product}");
            
            Console.WriteLine($"\nCount: {productRepo.Count}");
            
            // Поиск по Id
            var productById = productRepo.GetById(2);
            Console.WriteLine($"\nGetById(2): {productById}");
            
            // Поиск по предикату
            var expensiveProducts = productRepo.Find(p => p.Price > 100);
            Console.WriteLine("\nProducts with price > 100:");
            foreach (var product in expensiveProducts)
                Console.WriteLine($"  {product}");
            
            // Попытка добавить дубликат
            try
            {
                productRepo.Add(new Product { Id = 1, Name = "Phone", Price = 500m });
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"\nException caught: {ex.Message}");
            }
            
            // Удаление
            bool removed = productRepo.Remove(3);
            Console.WriteLine($"\nRemove(3): {removed}");
            Console.WriteLine($"Count after removal: {productRepo.Count}");
            
            // Работа с User
            Console.WriteLine("\n--- Working with Users ---");
            var userRepo = new Repository<User>();
            userRepo.Add(new User { Id = 1, Name = "Alice", Email = "alice@example.com" });
            userRepo.Add(new User { Id = 2, Name = "Bob", Email = "bob@example.com" });
            
            Console.WriteLine("All users:");
            foreach (var user in userRepo.GetAll())
                Console.WriteLine($"  {user}");
        }
    }
}
