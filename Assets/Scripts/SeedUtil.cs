using System;

public static class SeedUtil
{
    // String veya sayýdan deterministik seed üret (platform baðýmsýz).
    public static int FromInput(string input)
    {
        if (int.TryParse(input, out int numeric))
            return numeric;

        if (string.IsNullOrWhiteSpace(input))
            return RandomSeed();

        unchecked
        {
            // FNV-1a (32-bit)
            uint hash = 2166136261;
            foreach (char c in input)
            {
                hash ^= c;
                hash *= 16777619;
            }
            int seed = (int)(hash & 0x7FFFFFFF);
            return seed == 0 ? 1 : seed;
        }
    }

    public static int RandomSeed()
    {
        // Basit zaman tabanlý; istersen RNGCryptoServiceProvider da kullanabilirsin.
        int seed = (int)(DateTime.UtcNow.Ticks & 0x7FFFFFFF);
        return seed == 0 ? 1 : seed;
    }
}
