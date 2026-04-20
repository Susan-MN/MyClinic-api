using MyClinic.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyClinic.Infrastructure.Interfaces.Repositories
{
    public interface IPaymentRepository
    {
        Task<Appointment?> GetAppointmentWithDoctorAndPatientAsync(int appointmentId);
        Task<Appointment?> GetAppointmentByIdAsync(int appointmentId);
        Task<Appointment?> GetAppointmentByStripeSessionIdAsync(string stripeSessionId);
        Task<bool> IsWebhookEventProcessedAsync(string stripeEventId);
        Task AddProcessedWebhookEventAsync(ProcessedWebhookEvent webhookEvent);
        Task SaveChangesAsync();
    }
}
