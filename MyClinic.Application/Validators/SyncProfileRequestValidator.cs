using FluentValidation;
using MyClinic.Application.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MyClinic.Application.Validators
{
    public class SyncProfileRequestValidator : AbstractValidator<SyncProfileRequest>
    {
        public SyncProfileRequestValidator()
        {
            RuleFor(x => x.KeycloakId)
                .NotEmpty().WithMessage("KeycloakId is required");

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required")
                .Length(3, 50).WithMessage("Username must be 3-50 characters");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Role is required");
        }
    }
}