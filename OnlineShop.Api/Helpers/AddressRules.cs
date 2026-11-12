namespace OnlineShop.Api.Helpers;

public static class AddressRules
{
    public static bool TryNormalize(string? postalIn, string? addressIn,
        out string postal, out string address, out string? error)
    {
        postal = TextNormalizer.Normalize(postalIn ?? string.Empty);
        address = TextNormalizer.Normalize(addressIn ?? string.Empty);
        error = null;

        if (postal.Length != 10 || !postal.All(char.IsAsciiDigit))
            error = "کد پستی باید 10 رقم باشد.";
        else if (address.Length < 10 || address.Length > 500)
            error = "آدرس باید بین 10 تا 500 کاراکتر باشد.";

        return error is null;
    }
}