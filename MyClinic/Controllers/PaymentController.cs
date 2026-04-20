using System.IO;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyClinic.Application.DTO;
using MyClinic.Infrastructure.Interfaces.Services;

namespace MyClinic.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("checkout-session")]
        [Authorize(Policy = "PatientPolicy")]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "User not authenticated" });
            }

            var response = await _paymentService.CreateCheckoutSessionAsync(request, userId);
            return Ok(response);
        }

        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleWebhook()
        {
            var stripeSignature = Request.Headers["Stripe-Signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(stripeSignature))
            {
                return BadRequest(new { Message = "Missing Stripe-Signature header." });
            }

            string rawBody;
            using (var reader = new StreamReader(Request.Body))
            {
                rawBody = await reader.ReadToEndAsync();
            }

            await _paymentService.HandleStripeWebhookAsync(rawBody, stripeSignature);
            return Ok();
        }

        [HttpGet("status/{appointmentId:int}")]
        [Authorize(Policy = "PatientPolicy")]
        public async Task<IActionResult> GetPaymentStatus(int appointmentId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "User not authenticated" });
            }

            var response = await _paymentService.GetPaymentStatusAsync(appointmentId, userId);
            return Ok(response);
        }
    }
}
