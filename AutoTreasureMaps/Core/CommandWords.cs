namespace AutoTreasureMaps.Core;

internal static class CommandWords
{
    // A whole leading word, so "decipherx" does not read as "decipher".
    public static bool StartsWith(string arguments, string word)
        => arguments.StartsWith(word, StringComparison.OrdinalIgnoreCase)
        && (arguments.Length == word.Length || char.IsWhiteSpace(arguments[word.Length]));

    public static string After(string arguments, string word) => arguments[word.Length..].Trim();
}
