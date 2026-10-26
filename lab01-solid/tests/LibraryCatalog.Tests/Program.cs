using LibraryCatalog.Tests;

var tests = CoreTests.All().Concat(FactoryTests.All()).Concat(ConsoleTests.All()).ToList();
int failed = 0;
foreach (var (name, test) in tests)
{
    try
    {
        test();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception exception)
    {
        failed++;
        Console.WriteLine($"FAIL {name}: {exception.Message}");
    }
}

Console.WriteLine($"Tests: {tests.Count}, passed: {tests.Count - failed}, failed: {failed}");
return failed == 0 ? 0 : 1;
