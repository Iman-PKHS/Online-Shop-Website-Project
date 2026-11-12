using OnlineShop.Api.DTOs;

namespace OnlineShop.Api.Interfaces;

public enum InitiateOutcome { Created, CartEmpty, ProblemsFound, AddressRequired }

public class InitiateResult
{
    public InitiateOutcome Outcome { get; set; }
    public CheckoutResponse? Response { get; set; }
    public List<CheckoutProblem> Problems { get; set; } = new();
}

public enum CallbackOutcome { Processed, NotFound }

public class CallbackResult
{
    public CallbackOutcome Outcome { get; set; }
    public PaymentResultResponse? Result { get; set; }
}

public interface IPaymentService
{
    Task<InitiateResult> InitiateAsync(int userId, string baseUrl);
    Task<CallbackResult> ProcessCallbackAsync(Guid referenceId, bool success);
}