using QueryableTest;
using QueryableTest.Enumerable;
using QueryableTest.Models;
using QueryableTest.Queryable;
using QueryableTest.SqlQueryable;

var storage = new Storage();

storage.AddCar(new Car { Color = ConsoleColor.Red, Doors = 2 });
storage.AddCar(new Car { Color = ConsoleColor.Blue, Doors = 4 });
storage.AddCar(new Car { Color = ConsoleColor.Red, Doors = 4 });
storage.AddCar(new Car { Color = ConsoleColor.White, Doors = 2 });
storage.AddCar(new Car { Color = ConsoleColor.Red, Doors = 5 });

var enumerableOutput = new OutputProvider("Enumerable");
enumerableOutput.WriteLine("=== IEnumerable ===");
enumerableOutput.WriteLine("Building query (nothing executed yet)...");
var enumerableQuery = storage.GetCarsEnumerable()
    .Where(c => c.Color == ConsoleColor.Red, enumerableOutput);

enumerableOutput.WriteLine("Enumerating now:");
foreach (var car in enumerableQuery)
{
    enumerableOutput.WriteLine($"  {car.Color} car with {car.Doors} doors");
}

var inMemoryOutput = new OutputProvider("Provider");
inMemoryOutput.WriteLine();
inMemoryOutput.WriteLine("=== IQueryable ===");
inMemoryOutput.WriteLine("Building query (nothing executed yet, expression tree only)...");
var queryableQuery = storage.GetCarsQueryable(inMemoryOutput)
    .Where(c => c.Color == ConsoleColor.Red, inMemoryOutput);

inMemoryOutput.WriteLine("Enumerating now (provider translates expression tree, then executes):");
foreach (var car in queryableQuery)
{
    inMemoryOutput.WriteLine($"  {car.Color} car with {car.Doors} doors");
}

var sqlOutput = new OutputProvider("SqlProvider");
sqlOutput.WriteLine();
sqlOutput.WriteLine("=== Fictional SQL-backed IQueryable ===");
sqlOutput.WriteLine("Building query (nothing executed yet, expression tree only)...");
var sqlQueryable = new CarSqlQueryable<Car>(new CarSqlQueryProvider(storage.GetCarsQueryable(sqlOutput), sqlOutput), sqlOutput);
var sqlQuery = sqlQueryable
    .Where(c => c.Color == ConsoleColor.Red, sqlOutput);

sqlOutput.WriteLine("Enumerating now (provider translates expression tree to SQL, then \"executes\"):");
foreach (var car in sqlQuery)
{
    sqlOutput.WriteLine($"  {car.Color} car with {car.Doors} doors");
}

Console.WriteLine("Press <enter> to exit...");
Console.ReadLine();
