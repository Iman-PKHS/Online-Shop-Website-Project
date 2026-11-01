namespace OnlineShop.Api.Helpers;

public static class TextNormalizer
{
    public static string Normalize(string input)
    {
        var s = input.Trim()
            .Replace('\u064A', '\u06CC')   // Arabic  ي -> Persian  ی
            .Replace('\u0643', '\u06A9');  // Arabic  ك -> Persian  ک

        for (int i = 0; i < 10; i++)
        {
            s = s.Replace((char)('\u06F0' + i), (char)('0' + i));  // Persian digits ۰-۹
            s = s.Replace((char)('\u0660' + i), (char)('0' + i));  // Arabic-Indic digits ٠-٩
        }

        return s;
    }
}