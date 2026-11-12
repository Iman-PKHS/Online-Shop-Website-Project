using Microsoft.Extensions.Options;
using OnlineShop.Api.Helpers;
using OnlineShop.Api.Interfaces;

namespace OnlineShop.Api.Services;

public class PendingOrderExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly CheckoutSettings _settings;
    private readonly ILogger<PendingOrderExpiryService> _log;

    public PendingOrderExpiryService(
        IServiceScopeFactory scopes,
        IOptions<CheckoutSettings> options,
        ILogger<PendingOrderExpiryService> log)
    {
        _scopes = scopes;
        _settings = options.Value;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, _settings.ExpiryCheckMinutes)));

        do
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var lifecycle = scope.ServiceProvider.GetRequiredService<IOrderLifecycleService>();
                var count = await lifecycle.ExpireOverdueAsync();

                if (count > 0)
                    _log.LogInformation("Expired {Count} pending orders", count);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Pending order expiry run failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}