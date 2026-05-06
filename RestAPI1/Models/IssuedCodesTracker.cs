namespace RestAPI1.Models;

/// <summary>
/// In-memory evidence vydaných kódů SKz.
/// SKz je read-only replikovaná tabulka z Pohody — nově vytvořené karty
/// se v ní objeví až po synchronizaci. Tento tracker zajistí, že
/// CpqNextCode nikdy nevydá stejný kód dvakrát, i když Pohoda ještě
/// nezapsala záznam zpět do SQL.
/// </summary>
public static class IssuedCodesTracker
{
    private static readonly HashSet<int> _issuedNumbers = new();
    private static readonly object _lock = new();

    /// <summary>Zaregistruje vydaný kód (např. "S0104" → uloží 104).</summary>
    public static void Register(string code)
    {
        if (TryParseNumber(code, out int num))
        {
            lock (_lock)
            {
                _issuedNumbers.Add(num);
            }
            Console.WriteLine($"[IssuedCodesTracker] Registrován kód {code} ({num})");
        }
    }

    /// <summary>
    /// Vrátí nejmenší číslo > maxFromDb, které ještě nebylo vydáno.
    /// </summary>
    public static int GetNextNumber(int maxFromDb)
    {
        lock (_lock)
        {
            int candidate = maxFromDb + 1;
            while (_issuedNumbers.Contains(candidate))
                candidate++;
            return candidate;
        }
    }

    private static bool TryParseNumber(string code, out int num)
    {
        num = 0;
        if (string.IsNullOrWhiteSpace(code)) return false;
        string lastFour = code.Length >= 4 ? code[^4..].Trim() : code.Trim();
        return int.TryParse(lastFour, out num);
    }
}
