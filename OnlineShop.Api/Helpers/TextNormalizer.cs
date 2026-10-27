namespace OnlineShop.Api.Helpers;

public static class TextNormalizer
{
    public static string Normalize(string input)
        => input.Trim().Replace('ی','ي').Replace('ک','ك');
}