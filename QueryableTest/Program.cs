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

bool blueCarFilter(Car c) => c.Color == ConsoleColor.Blue;

TestEnumerable();

TestQueryable();

TestSqlQueryable();

Console.WriteLine("Press <enter> to exit...");
Console.ReadLine();

void TestEnumerable()
{
    var output = new OutputProvider("Enumerable");
    output.WriteLine("=== IEnumerable (filter by 'c.Color == ConsoleColor.Red') ===");
    output.WriteLine("Building query (nothing executed yet)...");
    var colorQuery = storage.GetCarsEnumerable()
        .Where(c => c.Color == ConsoleColor.Red, output);

    output.WriteLine("Enumerating now:");
    PrintResults(output, colorQuery);

    output.WriteLine();
    output.WriteLine("=== IEnumerable (filter by 'c.Doors == 4') ===");
    output.WriteLine("Building query (nothing executed yet)...");
    var doorsQuery = storage.GetCarsEnumerable()
        .Where(c => c.Doors == 4, output);

    output.WriteLine("Enumerating now:");
    PrintResults(output, doorsQuery);

    output.WriteLine();
    output.WriteLine("=== IEnumerable (filter by 'c.Color == ConsoleColor.Red && c.Doors >= 3')===");
    output.WriteLine("Building query (nothing executed yet)...");
    var compoundQuery = storage.GetCarsEnumerable()
        .Where(c => c.Color == ConsoleColor.Red && c.Doors >= 3, output);

    output.WriteLine("Enumerating now:");
    PrintResults(output, compoundQuery);

    output.WriteLine();
    output.WriteLine("=== IEnumerable (filter by 'blueCarFilter') ===");
    output.WriteLine("Building query (nothing executed yet)...");
    var blueQuery = storage.GetCarsEnumerable()
        .Where(blueCarFilter, output);

    output.WriteLine("Enumerating now:");
    PrintResults(output, blueQuery);
}

void TestQueryable()
{
    var output = new OutputProvider("Queryable");
    output.WriteLine();
    output.WriteLine("=== IQueryable (filter by 'c.Color == ConsoleColor.Red') ===");
    output.WriteLine("Building query (nothing executed yet, expression tree only)...");
    var redQuery = storage.GetCarsQueryable(output)
        .Where(c => c.Color == ConsoleColor.Red, output);

    output.WriteLine("Enumerating now (provider translates expression tree, then executes):");
    PrintResults(output, redQuery);

    output.WriteLine();
    output.WriteLine("=== IQueryable (filter by 'c.Doors == 4') ===");
    output.WriteLine("Building query (nothing executed yet, expression tree only)...");
    var doorsQuery = storage.GetCarsQueryable(output)
        .Where(c => c.Doors == 4, output);

    output.WriteLine("Enumerating now (provider translates expression tree, then executes):");
    PrintResults(output, doorsQuery);

    output.WriteLine();
    output.WriteLine("=== IQueryable (filter by 'c.Color == ConsoleColor.Red && c.Doors >= 3')===");
    output.WriteLine("Building query (nothing executed yet, expression tree only)...");
    var compoundQuery = storage.GetCarsQueryable(output)
        .Where(c => c.Color == ConsoleColor.Red && c.Doors >= 3, output);

    output.WriteLine("Enumerating now (provider translates expression tree, then executes):");
    PrintResults(output, compoundQuery);

    output.WriteLine();
    output.WriteLine("=== IQueryable (filter by 'blueCarFilter') ===");
    output.WriteLine("Building query (nothing executed yet, expression tree only)...");
    var blueQuery = storage.GetCarsQueryable(output)
        .Where(blueCarFilter, output);

    output.WriteLine("Enumerating now (provider translates expression tree, then executes):");
    PrintResults(output, blueQuery);
}

void TestSqlQueryable()
{
    var output = new OutputProvider("SqlQueryable");
    output.WriteLine();
    output.WriteLine("=== Fictional SQL-backed IQueryable (filter by 'c.Color == ConsoleColor.Red') ===");
    output.WriteLine("Building query (nothing executed yet, expression tree only)...");
    var sqlColorQueryable = new CarSqlQueryable<Car>(new CarSqlQueryProvider(storage.GetCarsQueryable(output), output), output);
    var redQuery = sqlColorQueryable
        .Where(c => c.Color == ConsoleColor.Red, output);

    output.WriteLine("Enumerating now (provider translates expression tree to SQL, then \"executes\"):");
    PrintResults(output, redQuery);

    output.WriteLine();
    output.WriteLine("=== Fictional SQL-backed IQueryable (filter by 'c.Doors == 4') ===");
    output.WriteLine("Building query (nothing executed yet, expression tree only)...");
    var doorsQueryable = new CarSqlQueryable<Car>(new CarSqlQueryProvider(storage.GetCarsQueryable(output), output), output);
    var doorsQuery = doorsQueryable
        .Where(c => c.Doors == 4, output);

    output.WriteLine("Enumerating now (provider translates expression tree to SQL, then \"executes\"):");
    PrintResults(output, doorsQuery);

    output.WriteLine();
    output.WriteLine("=== Fictional SQL-backed IQueryable (filter by 'c.Color == ConsoleColor.Red && c.Doors >= 3')===");
    output.WriteLine("Building query (nothing executed yet, expression tree only)...");
    var compoundQueryable = new CarSqlQueryable<Car>(new CarSqlQueryProvider(storage.GetCarsQueryable(output), output), output);
    var compoundQuery = compoundQueryable
        .Where(c => c.Color == ConsoleColor.Red && c.Doors >= 3, output);

    output.WriteLine("Enumerating now (provider translates expression tree to SQL, then \"executes\"):");
    PrintResults(output, compoundQuery);

    output.WriteLine();
    output.WriteLine("=== IQueryable (filter by 'blueCarFilter') ===");
    output.WriteLine("Building query (nothing executed yet, expression tree only)...");
    var blueQueryable = new CarSqlQueryable<Car>(new CarSqlQueryProvider(storage.GetCarsQueryable(output), output), output);
    var blueQuery = blueQueryable
        .Where(blueCarFilter, output); //will throw exception because the provider cannot translate a delegate to SQL

    output.WriteLine("Enumerating now (provider translates expression tree, then executes):");
    PrintResults(output, blueQuery);
}

static void PrintResults(IOutputProvider output, IEnumerable<Car> enumerable)
{
    try
    {
        foreach (var car in enumerable)
        {
            output.WriteLine($"  {car.Color} car with {car.Doors} doors");
        }
    }
    catch (Exception ex)
    {
        output.WriteLine($"  Exception occurred during enumeration: {ex.Message}");
    }
}