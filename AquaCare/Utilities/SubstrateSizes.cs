namespace AquaCare;

public static class SubstrateSizes
{
    private const string UltraNano = "0,4-1,2";
    private const string Nano = "1-2";
    private const string Fine = "1,4-2";
    private const string Medium = "2-4";
    private const string Large = "4-8";
    private const string UltraLarge = "10-20";

    public static List<string> GetAll() => new() { UltraNano, Nano, Fine, Medium, Large, UltraLarge };
}