namespace OnlineShop.Api.DTOs;

public class SetPurchasedRequest
{
    public bool? Purchased { get; set; }   // true or false = customer's choice ; null = system decides
}