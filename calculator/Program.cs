using System;

while (true)
{
    Console.Write("Первое число (или 'q' для выхода): ");
    string input = Console.ReadLine();
    if (input == "q") break;
    double a = Convert.ToDouble(input);

    Console.Write("Второе число: ");
    input = Console.ReadLine();
    if (input == "q") break;
    double b = Convert.ToDouble(input);

    Console.Write("Операция (+, -, *, /): ");
    string op = Console.ReadLine();
    if (op == "q") break;

    double result;
    if (op == "+") result = a + b;
    else if (op == "-") result = a - b;
    else if (op == "*") result = a * b;
    else if (op == "/") result = a / b;
    else
    {
        Console.WriteLine("Неверная операция!");
        continue;
    }

    Console.WriteLine($"Результат: {result}\n");
}