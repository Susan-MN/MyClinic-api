using Microsoft.EntityFrameworkCore;
using MyClinic.Domain.Entities;
using MyClinic.Infrastructure.Data;
using MyClinic.Infrastructure.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyClinic.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
    
        private readonly AppDbContext _db;
        public PaymentRepository(AppDbContext db)
        {
            _db = db;
        }
        public async Task<Appointment?> GetAppointmentWithDoctorAndPatientAsync(int appointmentId)
        {
            return await _db.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);
        }
        public async Task<Appointment?> GetAppointmentByIdAsync(int appointmentId)
        {
            return await _db.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);
        }
        public async Task<Appointment?> GetAppointmentByStripeSessionIdAsync(string stripeSessionId)
        {
            if (string.IsNullOrWhiteSpace(stripeSessionId))
            {
                return null;
            }
            return await _db.Appointments
                .FirstOrDefaultAsync(a => a.StripeSessionId == stripeSessionId);
        }
        public async Task<bool> IsWebhookEventProcessedAsync(string stripeEventId)
        {
            if (string.IsNullOrWhiteSpace(stripeEventId))
            {
                return false;
            }
            return await _db.ProcessedWebhookEvents
                .AnyAsync(e => e.StripeEventId == stripeEventId);
        }
        public async Task AddProcessedWebhookEventAsync(ProcessedWebhookEvent webhookEvent)
        {
            await _db.ProcessedWebhookEvents.AddAsync(webhookEvent);
        }
        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }


}
