using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyClinic.Application.DTO;
using MyClinic.Domain.Entities;
using MyClinic.Infrastructure.Interfaces.Repositories;
using MyClinic.Infrastructure.Interfaces.Services;
using MyClinic.Infrastructure.Payments;
using Stripe;
using Stripe.Checkout;

namespace MyClinic.Infrastructure.Servives
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly StripeOptions _stripeOptions;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
        IPaymentRepository paymentRepository,
        IOptions<StripeOptions> stripeOptions,
        ILogger<PaymentService> logger)
        {
            _paymentRepository = paymentRepository;
            _stripeOptions = stripeOptions.Value;
            _logger = logger;
        }

        public async Task<CreateCheckoutSessionResponse> CreateCheckoutSessionAsync(CreateCheckoutSessionRequest request, string userId)
        {
            if (request == null)
            {
                throw new ArgumentException("Request body is required.");
            }

            if (request.AppointmentId <= 0)
            {
                throw new ArgumentException("AppointmentId must be greater than 0.");
            }

            var appointment = await _paymentRepository.GetAppointmentWithDoctorAndPatientAsync(request.AppointmentId)
                ?? throw new InvalidOperationException("Appointment not found.");

            if (!IsAppointmentOwnedByUser(appointment, userId))
            {
                throw new UnauthorizedAccessException("You are not authorized to pay for this appointment.");
            }

            if (appointment.PaymentStatus == PaymentStatus.Paid)
            {
                throw new InvalidOperationException("Appointment is already paid.");
            }

            if (appointment.Amount <= 0)
            {
                throw new InvalidOperationException("Appointment amount must be greater than zero.");
            }

            StripeConfiguration.ApiKey = _stripeOptions.SecretKey;

            var sessionOptions = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = _stripeOptions.SuccessUrl,
                CancelUrl = _stripeOptions.CancelUrl,
                CustomerEmail = appointment.Patient?.Email,
                Metadata = new Dictionary<string, string>
                {
                    ["appointmentId"] = appointment.Id.ToString(),
                    ["userId"] = userId
                },
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["appointmentId"] = appointment.Id.ToString(),
                        ["userId"] = userId
                    }
                },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = appointment.Currency.ToLowerInvariant(),
                            UnitAmount = (long)(appointment.Amount * 100m),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Clinic Appointment #{appointment.Id}"
                            }
                        }
                    }
                }
            };

            var sessionService = new SessionService();
            var session = await sessionService.CreateAsync(sessionOptions);

            appointment.StripeSessionId = session.Id;
            appointment.PaymentStatus = PaymentStatus.Pending;
            await _paymentRepository.SaveChangesAsync();

            return new CreateCheckoutSessionResponse
            {
                SessionId = session.Id,
                CheckoutUrl = session.Url ?? string.Empty
            };
        }

        public async Task HandleStripeWebhookAsync(string rawBody, string stripeSignature)
        {
            if (string.IsNullOrWhiteSpace(rawBody))
            {
                throw new ArgumentException("Webhook body is required.");
            }

            if (string.IsNullOrWhiteSpace(stripeSignature))
            {
                throw new ArgumentException("Stripe signature header is missing.");
            }

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(rawBody, stripeSignature, _stripeOptions.WebhookSecret);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid Stripe webhook signature.");
                throw new InvalidOperationException("Invalid webhook signature.");
            }

            if (await _paymentRepository.IsWebhookEventProcessedAsync(stripeEvent.Id))
            {
                _logger.LogInformation("Ignoring duplicate Stripe event {EventId}", stripeEvent.Id);
                return;
            }

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session == null)
                    {
                        break;
                    }

                    if (session.Metadata == null ||
                        !session.Metadata.TryGetValue("appointmentId", out var appointmentIdValue) ||
                        !int.TryParse(appointmentIdValue, out var appointmentId))
                    {
                        _logger.LogWarning("Missing appointmentId metadata in checkout.session.completed event {EventId}", stripeEvent.Id);
                        break;
                    }

                    var appointment = await _paymentRepository.GetAppointmentByIdAsync(appointmentId);
                    if (appointment == null)
                    {
                        _logger.LogWarning("Appointment {AppointmentId} not found for Stripe event {EventId}", appointmentId, stripeEvent.Id);
                        break;
                    }

                    appointment.PaymentStatus = PaymentStatus.Paid;
                    appointment.PaidAt = DateTime.UtcNow;
                    appointment.StripePaymentIntentId = session.PaymentIntentId;
                    appointment.Status = AppointmentStatus.Confirmed;
                    break;
                }
                case "payment_intent.payment_failed":
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (paymentIntent == null)
                    {
                        break;
                    }

                    if (paymentIntent.Metadata == null ||
                        !paymentIntent.Metadata.TryGetValue("appointmentId", out var appointmentIdValue) ||
                        !int.TryParse(appointmentIdValue, out var appointmentId))
                    {
                        _logger.LogWarning("Missing appointmentId metadata in payment_intent.payment_failed event {EventId}", stripeEvent.Id);
                        break;
                    }

                    var appointment = await _paymentRepository.GetAppointmentByIdAsync(appointmentId);
                    if (appointment == null)
                    {
                        _logger.LogWarning("Appointment {AppointmentId} not found for failed payment intent {PaymentIntentId}", appointmentId, paymentIntent.Id);
                        break;
                    }

                    appointment.PaymentStatus = PaymentStatus.Failed;
                    appointment.StripePaymentIntentId = paymentIntent.Id;
                    appointment.PaymentFailureReason = paymentIntent.LastPaymentError?.Message;
                    break;
                }
            }

            await _paymentRepository.AddProcessedWebhookEventAsync(new ProcessedWebhookEvent
            {
                StripeEventId = stripeEvent.Id,
                EventType = stripeEvent.Type,
                ProcessedAt = DateTime.UtcNow
            });

            await _paymentRepository.SaveChangesAsync();
        }

        public async Task<PaymentStatusResponse> GetPaymentStatusAsync(int appointmentId, string userId)
        {
            if (appointmentId <= 0)
            {
                throw new ArgumentException("AppointmentId must be greater than 0.");
            }

            var appointment = await _paymentRepository.GetAppointmentWithDoctorAndPatientAsync(appointmentId)
                ?? throw new InvalidOperationException("Appointment not found.");

            if (!IsAppointmentOwnedByUser(appointment, userId))
            {
                throw new UnauthorizedAccessException("You are not authorized to view this payment status.");
            }

            return new PaymentStatusResponse
            {
                AppointmentId = appointment.Id,
                PaymentStatus = appointment.PaymentStatus.ToString(),
                PaidAt = appointment.PaidAt,
                Amount = appointment.Amount,
                Currency = appointment.Currency
            };
        }

        private static bool IsAppointmentOwnedByUser(Appointment appointment, string userId)
        {
            return string.Equals(appointment.Patient?.KeycloakId, userId, StringComparison.Ordinal);
        }
    }
}
