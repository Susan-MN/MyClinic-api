using FluentValidation;
using MyClinic.Application.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyClinic.Application.Validators
{
    public class CreateLeaveRequestValidator : AbstractValidator<CreateLeaveRequest>
    {
        public CreateLeaveRequestValidator()
        {
            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("StartDate is required")
                .Must(BeValidDate).WithMessage("StartDate must be in yyyy-MM-dd format");

            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("EndDate is required")
                .Must(BeValidDate).WithMessage("EndDate must be in yyyy-MM-dd format");

            RuleFor(x => x)
                .Must(x => BeValidDateRange(x.StartDate, x.EndDate))
                .WithMessage("EndDate must be after or equal to StartDate");

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Reason is required")
                .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");
        }

        private bool BeValidDate(string date)
        {
            if (string.IsNullOrWhiteSpace(date))
                return false;
            return DateOnly.TryParse(date, out _);
        }

        private bool BeValidDateRange(string startDate, string endDate)
        {
            if (!DateOnly.TryParse(startDate, out var start) ||
                !DateOnly.TryParse(endDate, out var end))
                return false;

            return end >= start;
        }
    }
}