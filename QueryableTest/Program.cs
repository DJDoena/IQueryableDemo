using QueryableTest;

var storage = new Storage();

storage.AddCar(new Car { Color = ConsoleColor.Red, Doors = 2 });
storage.AddCar(new Car { Color = ConsoleColor.Blue, Doors = 4 });
storage.AddCar(new Car { Color = ConsoleColor.Red, Doors = 4 });
storage.AddCar(new Car { Color = ConsoleColor.White, Doors = 2 });
storage.AddCar(new Car { Color = ConsoleColor.Red, Doors = 5 });

Console.WriteLine("=== IEnumerable ===");
Console.WriteLine("Building query (nothing executed yet)...");
var enumerableQuery = storage.CarsEnumerable
    .WhereDebug(c => c.Color == ConsoleColor.Red);

Console.WriteLine("Enumerating now:");
foreach (var car in enumerableQuery)
{
    Console.WriteLine($"  {car.Color} car with {car.Doors} doors");
}

Console.WriteLine();
Console.WriteLine("=== IQueryable ===");
Console.WriteLine("Building query (nothing executed yet, expression tree only)...");
var queryableQuery = storage.CarsQueryable
    .WhereDebug(c => c.Color == ConsoleColor.Red);

Console.WriteLine("Enumerating now (provider translates expression tree, then executes):");
foreach (var car in queryableQuery)
{
    Console.WriteLine($"  {car.Color} car with {car.Doors} doors");
}

Console.WriteLine();
Console.WriteLine("=== Fictional SQL-backed IQueryable ===");
Console.WriteLine("Building query (nothing executed yet, expression tree only)...");
var sqlQueryable = new CarSqlQueryable<Car>(new CarSqlQueryProvider(storage.CarsEnumerable));
var sqlQuery = sqlQueryable
    .WhereDebug(c => c.Color == ConsoleColor.Red);

Console.WriteLine("Enumerating now (provider translates expression tree to SQL, then \"executes\"):");
foreach (var car in sqlQuery)
{
    Console.WriteLine($"  {car.Color} car with {car.Doors} doors");
}

Console.WriteLine("Press <enter> to exit...");
Console.ReadLine();
