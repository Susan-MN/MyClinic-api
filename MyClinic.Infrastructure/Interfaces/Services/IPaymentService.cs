using MyClinic.Application.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyClinic.Infrastructure.Interfaces.Services
{
    public  interface IPaymentService
    {
        Task<CreateCheckoutSessionResponse> CreateCheckoutSessionAsync(CreateCheckoutSessionRequest request,
           string userId);
        Task HandleStripeWebhookAsync(string rawBody,string stripeSignature);
        Task<PaymentStatusResponse> GetPaymentStatusAsync(int appointmentId,string userId);
    }
}
