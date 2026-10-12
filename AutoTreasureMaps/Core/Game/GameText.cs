namespace AutoTreasureMaps.Core.Game;

// Sheet text and window text spell the same words with different invisible characters: soft hyphens in long German
// names, no-break spaces before French punctuation, line breaks inside prompts. Both sides are folded the same way
// before they are compared.
internal static class GameText
{
    private const int StackLimit = 256;
    private const char SoftHyphen = '­';
    private const char ZeroWidthSpace = '​';
    private const char WordJoiner = '⁠';
    private const char NoBreakSpace = ' ';
    private const char NarrowNoBreakSpace = ' ';

    public static string Fold(ReadOnlySpan<char> text)
    {
        var buffer = text.Length <= StackLimit ? stackalloc char[text.Length] : new char[text.Length];
        var length = 0;
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            switch (character)
            {
                case SoftHyphen:
                case ZeroWidthSpace:
                case WordJoiner:
                    continue;
                case NoBreakSpace:
                case NarrowNoBreakSpace:
                case '\r':
                case '\n':
                    buffer[length++] = ' ';
                    continue;
                default:
                    buffer[length++] = character;
                    continue;
            }
        }

        return new string(buffer[..length].Trim());
    }

    public static bool Same(string folded, ReadOnlySpan<char> other)
        => string.Equals(folded, Fold(other), StringComparison.OrdinalIgnoreCase);

    public static bool Contains(string folded, string foldedPart)
        => foldedPart.Length > 0 && folded.Contains(foldedPart, StringComparison.OrdinalIgnoreCase);
}
