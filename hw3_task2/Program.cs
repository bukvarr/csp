using System;
using System.Collections.Generic;

namespace Task2
{
    public static class CollectionUtils
    {
        // 1. Вернуть новый список без дубликатов, сохраняя порядок первых вхождений
        public static List<T> Distinct<T>(List<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            
            var result = new List<T>();
            var seen = new HashSet<T>();
            
            foreach (var item in source)
            {
                if (!seen.Contains(item))
                {
                    seen.Add(item);
                    result.Add(item);
                }
            }
            
            return result;
        }
        
        // 2. Сгруппировать элементы по ключу
        public static Dictionary<TKey, List<TValue>> GroupBy<TValue, TKey>(
            List<TValue> source,
            Func<TValue, TKey> keySelector) where TKey : notnull
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));
            
            var result = new Dictionary<TKey, List<TValue>>();
            
            foreach (var item in source)
            {
                var key = keySelector(item);
                if (!result.ContainsKey(key))
                    result[key] = new List<TValue>();
                
                result[key].Add(item);
            }
            
            return result;
        }
        
        // 3. Объединить два словаря с разрешением конфликтов
        public static Dictionary<TKey, TValue> Merge<TKey, TValue>(
            Dictionary<TKey, TValue> first,
            Dictionary<TKey, TValue> second,
            Func<TValue, TValue, TValue> conflictResolver) where TKey : notnull
        {
            if (first == null)
                throw new ArgumentNullException(nameof(first));
            if (second == null)
                throw new ArgumentNullException(nameof(second));
            if (conflictResolver == null)
                throw new ArgumentNullException(nameof(conflictResolver));
            
            var result = new Dictionary<TKey, TValue>(first);
            
            foreach (var kvp in second)
            {
                if (result.ContainsKey(kvp.Key))
                    result[kvp.Key] = conflictResolver(result[kvp.Key], kvp.Value);
                else
                    result[kvp.Key] = kvp.Value;
            }
            
            return result;
        }
        
        // 4. Найти элемент с максимальным значением селектора
        public static T MaxBy<T, TKey>(List<T> source, Func<T, TKey> selector)
            where TKey : IComparable<TKey>
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
            if (source.Count == 0)
                throw new InvalidOperationException("Collection is empty");
            
            T maxItem = source[0];
            TKey maxValue = selector(maxItem);
            
            for (int i = 1; i < source.Count; i++)
            {
                TKey currentValue = selector(source[i]);
                if (currentValue.CompareTo(maxValue) > 0)
                {
                    maxValue = currentValue;
                    maxItem = source[i];
                }
            }
            
            return maxItem;
        }
    }
    
    // Тестовый класс для проверки
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        
        public override string ToString() => $"{Name} (${Price})";
    }
    
    // Проверка
    class Program
    {
        static void Main()
        {
            Console.WriteLine("=== Task 2: CollectionUtils ===\n");
            
            // Test 1: Distinct
            Console.WriteLine("1. Distinct:");
            var numbers = new List<int> { 1, 2, 2, 3, 4, 4, 4, 5 };
            var distinctNumbers = CollectionUtils.Distinct(numbers);
            Console.WriteLine($"  Original: [{string.Join(", ", numbers)}]");
            Console.WriteLine($"  Distinct: [{string.Join(", ", distinctNumbers)}]");
            
            var words = new List<string> { "apple", "banana", "apple", "cherry", "banana", "date" };
            var distinctWords = CollectionUtils.Distinct(words);
            Console.WriteLine($"  Words: [{string.Join(", ", words)}]");
            Console.WriteLine($"  Distinct words: [{string.Join(", ", distinctWords)}]");
            
            // Test 2: GroupBy
            Console.WriteLine("\n2. GroupBy (by length):");
            var wordsForGroup = new List<string> { "cat", "dog", "elephant", "bird", "ant", "tiger", "fish" };
            var groupedByLength = CollectionUtils.GroupBy(wordsForGroup, w => w.Length);
            foreach (var group in groupedByLength)
            {
                Console.WriteLine($"  Length {group.Key}: [{string.Join(", ", group.Value)}]");
            }
            
            // Test 3: Merge
            Console.WriteLine("\n3. Merge dictionaries:");
            var dict1 = new Dictionary<string, int>
            {
                { "apple", 3 },
                { "banana", 2 },
                { "cherry", 5 }
            };
            
            var dict2 = new Dictionary<string, int>
            {
                { "banana", 4 },
                { "date", 1 },
                { "apple", 2 }
            };
            
            var merged = CollectionUtils.Merge(dict1, dict2, (v1, v2) => v1 + v2);
            Console.WriteLine("  Dictionary 1: " + string.Join(", ", dict1.Select(kv => $"{kv.Key}:{kv.Value}")));
            Console.WriteLine("  Dictionary 2: " + string.Join(", ", dict2.Select(kv => $"{kv.Key}:{kv.Value}")));
            Console.WriteLine("  Merged (sum): " + string.Join(", ", merged.Select(kv => $"{kv.Key}:{kv.Value}")));
            
            // Test 4: MaxBy
            Console.WriteLine("\n4. MaxBy:");
            var products = new List<Product>
            {
                new Product { Id = 1, Name = "Laptop", Price = 1200m },
                new Product { Id = 2, Name = "Mouse", Price = 25m },
                new Product { Id = 3, Name = "Keyboard", Price = 80m },
                new Product { Id = 4, Name = "Monitor", Price = 350m }
            };
            
            var mostExpensive = CollectionUtils.MaxBy(products, p => p.Price);
            Console.WriteLine($"  Most expensive product: {mostExpensive}");
            
            // Test empty collection
            Console.WriteLine("\n  Testing empty collection:");
            var emptyList = new List<int>();
            try
            {
                CollectionUtils.MaxBy(emptyList, x => x);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"  Exception: {ex.Message}");
            }
        }
    }
}
