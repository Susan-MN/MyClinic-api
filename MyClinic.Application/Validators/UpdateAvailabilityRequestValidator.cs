using FluentValidation;
using MyClinic.Application.DTO;
using System;
using System.Linq;

namespace MyClinic.Application.Validators
{
    public class UpdateAvailabilityRequestValidator : AbstractValidator<UpdateAvailabilityRequest>
    {
        private static readonly string[] ValidDayNames = 
        { 
            "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday" 
        };

        public UpdateAvailabilityRequestValidator()
        {
            RuleFor(x => x.StartTime)
                .NotEmpty().WithMessage("StartTime is required")
                .Must(BeValidTimeFormat).WithMessage("StartTime must be in HH:mm format (e.g., 09:00)")
                .Must(BeValidTime).WithMessage("StartTime must be a valid time");

            RuleFor(x => x.EndTime)
                .NotEmpty().WithMessage("EndTime is required")
                .Must(BeValidTimeFormat).WithMessage("EndTime must be in HH:mm format (e.g., 17:00)")
                .Must(BeValidTime).WithMessage("EndTime must be a valid time");

            RuleFor(x => x)
                .Must(x => BeValidTimeRange(x.StartTime, x.EndTime))
                .WithMessage("EndTime must be after StartTime");

            RuleFor(x => x.SlotDuration)
                .GreaterThan(0).WithMessage("SlotDuration must be greater than 0")
                .LessThanOrEqualTo(480).WithMessage("SlotDuration cannot exceed 480 minutes (8 hours)")
                .Must(BeReasonableDuration).WithMessage("SlotDuration should be between 5 and 120 minutes");

            RuleFor(x => x.WorkingDays)
                .NotNull().WithMessage("WorkingDays cannot be null")
                .Must(days => days != null && days.Count <= 7)
                .WithMessage("WorkingDays cannot exceed 7 days");

            RuleForEach(x => x.WorkingDays)
                .Must(BeValidDayName)
                .WithMessage("Each working day must be a valid day name (monday, tuesday, etc.)")
                .When(x => x.WorkingDays != null && x.WorkingDays.Any());
        }

        private bool BeValidTimeFormat(string time)
        {
            if (string.IsNullOrWhiteSpace(time))
                return false;

            return System.Text.RegularExpressions.Regex.IsMatch(time, @"^([0-1][0-9]|2[0-3]):[0-5][0-9]$");
        }

        private bool BeValidTime(string time)
        {
            if (string.IsNullOrWhiteSpace(time))
                return false;

            return TimeOnly.TryParse(time, out _);
        }

        private bool BeValidTimeRange(string startTime, string endTime)
        {
            if (string.IsNullOrWhiteSpace(startTime) || string.IsNullOrWhiteSpace(endTime))
                return false;

            if (!TimeOnly.TryParse(startTime, out var start) || !TimeOnly.TryParse(endTime, out var end))
                return false;

            return end > start;
        }

        private bool BeReasonableDuration(int duration)
        {
            return duration >= 5 && duration <= 120;
        }

        private bool BeValidDayName(string dayName)
        {
            if (string.IsNullOrWhiteSpace(dayName))
                return false;

            return ValidDayNames.Contains(dayName.ToLowerInvariant());
        }
    }
}








