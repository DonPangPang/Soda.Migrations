namespace Soda.Migrations.Helpers;

public static class ConsoleHelper
{
    private const string HEAD = "SODA";
    public static void WriteInfo(string str)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"{HEAD}: ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(str);
    }

    public static void WriteError(string str)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write($"{HEAD}: ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(str);
    }

    public static void WriteWarning(string str)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"{HEAD}: ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(str);
    }
}