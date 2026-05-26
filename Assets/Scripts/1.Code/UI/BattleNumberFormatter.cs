using System;
using System.Globalization;

public static class BattleNumberFormatter
{
    private const double KiloThreshold = 1000.0;

    public static string Format(double value)
    {
        double roundedValue = Math.Ceiling(value);

        if (roundedValue < KiloThreshold)
            return roundedValue.ToString("N0", CultureInfo.InvariantCulture);

        double kiloValue = roundedValue / KiloThreshold;
        return kiloValue.ToString("0.###", CultureInfo.InvariantCulture) + "K";
    }
}
