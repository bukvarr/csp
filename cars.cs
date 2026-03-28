using System;
using System.Collections.Generic;

namespace CarDescriptionApp
{
    // Основной интерфейс автомобиля
    public interface ICar
    {
        string GetDescription();
        string GetBrand();
        int GetSeats();
    }

    // Интерфейс электрокара
    public interface IElectric
    {
        int GetBatteryCapacity(); // кВт·ч
        int GetRange(); // км
    }

    // Интерфейс обычной машины (механической, с ДВС)
    public interface IMechanicalCar
    {
        double GetEngineVolume(); // литры
        string GetFuelType(); // бензин, дизель
    }

    // Интерфейс автоматической коробки передач
    public interface IAutomatical
    {
        string GetTransmissionType(); // автомат, вариатор, робот
        int GetGearsCount();
    }

    // Интерфейс механической коробки передач
    public interface IManual
    {
        int GetGearsCount();
        bool HasClutch();
    }

    // Абстрактный класс
    public abstract class ACar : ICar
    {
        public string Brand { get; protected set; }
        public int Seats { get; protected set; }
        public string InfotainmentSystem { get; protected set; } // мультимедиа

        protected ACar(string brand, int seats, string infotainment)
        {
            Brand = brand;
            Seats = seats;
            InfotainmentSystem = infotainment;
        }

        public virtual string GetBrand() => Brand;
        public virtual int GetSeats() => Seats;

        public abstract string GetDescription();

        protected virtual string GetBaseDescription()
        {
            return $"{Brand}: {GetEngineType()}, {GetTransmissionInfo()}, {Seats} мест, {InfotainmentSystem} на борту";
        }

        protected abstract string GetEngineType();
        protected abstract string GetTransmissionInfo();
    }

    // Электромобиль с автоматом
    public class ElectricCar : ACar, IElectric, IAutomatical
    {
        public int BatteryCapacity { get; private set; }
        public int Range { get; private set; }
        public string TransmissionType { get; private set; }
        public int GearsCount { get; private set; }

        public ElectricCar(string brand, int seats, string infotainment, int batteryCapacity, int range, string transmissionType, int gearsCount)
            : base(brand, seats, infotainment)
        {
            BatteryCapacity = batteryCapacity;
            Range = range;
            TransmissionType = transmissionType;
            GearsCount = gearsCount;
        }

        public int GetBatteryCapacity() => BatteryCapacity;
        public int GetRange() => Range;
        public string GetTransmissionType() => TransmissionType;
        public int GetGearsCount() => GearsCount;

        protected override string GetEngineType() => $"электрический (батарея {BatteryCapacity} кВт·ч, запас хода {Range} км)";
        protected override string GetTransmissionInfo() => $"{TransmissionType} ({GearsCount} ступеней)";

        public override string GetDescription()
        {
            return GetBaseDescription();
        }
    }

    // Обычный автомобиль с автоматом
    public class AutomaticCar : ACar, IMechanicalCar, IAutomatical
    {
        public double EngineVolume { get; private set; }
        public string FuelType { get; private set; }
        public string TransmissionType { get; private set; }
        public int GearsCount { get; private set; }

        public AutomaticCar(string brand, int seats, string infotainment, double engineVolume, string fuelType, string transmissionType, int gearsCount)
            : base(brand, seats, infotainment)
        {
            EngineVolume = engineVolume;
            FuelType = fuelType;
            TransmissionType = transmissionType;
            GearsCount = gearsCount;
        }

        public double GetEngineVolume() => EngineVolume;
        public string GetFuelType() => FuelType;
        public string GetTransmissionType() => TransmissionType;
        public int GetGearsCount() => GearsCount;

        protected override string GetEngineType() => $"{FuelType} двигатель {EngineVolume:F1} л";
        protected override string GetTransmissionInfo() => $"{TransmissionType} ({GearsCount} ступеней)";

        public override string GetDescription()
        {
            return GetBaseDescription();
        }
    }

    // Обычный автомобиль с механикой
    public class ManualCar : ACar, IMechanicalCar, IManual
    {
        public double EngineVolume { get; private set; }
        public string FuelType { get; private set; }
        public int GearsCount { get; private set; }
        public bool HasClutchPedal { get; private set; }

        public ManualCar(string brand, int seats, string infotainment, double engineVolume, string fuelType, int gearsCount, bool hasClutch)
            : base(brand, seats, infotainment)
        {
            EngineVolume = engineVolume;
            FuelType = fuelType;
            GearsCount = gearsCount;
            HasClutchPedal = hasClutch;
        }

        public double GetEngineVolume() => EngineVolume;
        public string GetFuelType() => FuelType;
        public int GetGearsCount() => GearsCount;
        public bool HasClutch() => HasClutchPedal;

        protected override string GetEngineType() => $"{FuelType} двигатель {EngineVolume:F1} л";
        protected override string GetTransmissionInfo() => $"механическая ({GearsCount} ступеней{(HasClutchPedal ? ", сцепление" : "")})";

        public override string GetDescription()
        {
            return GetBaseDescription();
        }
    }

    // Типы автомобилей для фабрики
    public enum CarType
    {
        TeslaModel3,
        TeslaModelY,
        BMWX5,
        LadaVesta,
        ToyotaCamry
    }

    // Фабрика автомобилей
    public static class CarFactory
    {
        public static ICar CreateCar(CarType type)
        {
            switch (type)
            {
                case CarType.TeslaModel3:
                    return new ElectricCar(
                        brand: "Tesla Model 3",
                        seats: 5,
                        infotainment: "Tesla OS",
                        batteryCapacity: 60,
                        range: 491,
                        transmissionType: "автомат (одноступенчатый)",
                        gearsCount: 1
                    );

                case CarType.TeslaModelY:
                    return new ElectricCar(
                        brand: "Tesla Model Y",
                        seats: 5,
                        infotainment: "Tesla OS",
                        batteryCapacity: 75,
                        range: 533,
                        transmissionType: "автомат (одноступенчатый)",
                        gearsCount: 1
                    );

                case CarType.BMWX5:
                    return new AutomaticCar(
                        brand: "BMW X5",
                        seats: 5,
                        infotainment: "iDrive",
                        engineVolume: 3.0,
                        fuelType: "бензиновый",
                        transmissionType: "автомат (Steptronic)",
                        gearsCount: 8
                    );

                case CarType.LadaVesta:
                    return new ManualCar(
                        brand: "Lada Vesta",
                        seats: 5,
                        infotainment: "Эра-ГЛОНАСС",
                        engineVolume: 1.6,
                        fuelType: "бензиновый",
                        gearsCount: 5,
                        hasClutch: true
                    );

                case CarType.ToyotaCamry:
                    return new AutomaticCar(
                        brand: "Toyota Camry",
                        seats: 5,
                        infotainment: "Toyota Touch",
                        engineVolume: 2.5,
                        fuelType: "бензиновый",
                        transmissionType: "вариатор",
                        gearsCount: 8 // условно, для вариатора это имитированные ступени
                    );

                default:
                    throw new ArgumentException("Неизвестный тип автомобиля");
            }
        }
    }

    // Основная программа
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Добро пожаловать в каталог автомобилей!");
            Console.WriteLine("Доступные марки: Tesla Model 3, Tesla Model Y, BMW X5, Lada Vesta, Toyota Camry");

            while (true)
            {
                Console.Write("\nВведите марку автомобиля или done для остановки ввода: ");
                string input = Console.ReadLine()?.Trim();

                if (string.Equals(input, "done", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Работа завершена.");
                    break;
                }

                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("Марка не может быть пустой. Попробуйте снова.");
                    continue;
                }

                // Сопоставление ввода с CarType
                CarType? carType = input.ToLower() switch
                {
                    "tesla model 3" => CarType.TeslaModel3,
                    "tesla model y" => CarType.TeslaModelY,
                    "bmw x5" => CarType.BMWX5,
                    "lada vesta" => CarType.LadaVesta,
                    "toyota camry" => CarType.ToyotaCamry,
                    _ => null
                };

                if (carType == null)
                {
                    Console.WriteLine($"Марка \"{input}\" не найдена в каталоге. Доступные: Tesla Model 3, Tesla Model Y, BMW X5, Lada Vesta, Toyota Camry");
                    continue;
                }

                try
                {
                    ICar car = CarFactory.CreateCar(carType.Value);
                    Console.WriteLine(car.GetDescription());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при создании автомобиля: {ex.Message}");
                }
            }
        }
    }
}