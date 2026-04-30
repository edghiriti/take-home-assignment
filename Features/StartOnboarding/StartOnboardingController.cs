using Microsoft.AspNetCore.Mvc;

namespace StripeOnboardingSlice.Features.StartOnboarding;

[ApiController]
[Route("api/onboarding")]
public class StartOnboardingController : ControllerBase
{
    private readonly StartOnboardingHandler _handler;

    public StartOnboardingController(StartOnboardingHandler handler)
    {
        _handler = handler;
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] StartOnboardingRequest request, CancellationToken cancellationToken)
    {
        var url = await _handler.HandleAsync(request, cancellationToken);
        return Ok(new { CheckoutUrl = url });
    }
}