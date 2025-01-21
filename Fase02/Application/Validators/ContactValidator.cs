using Application.DTOs;
using FluentValidation;

namespace Application.Validators
{
    public class ContactValidator : AbstractValidator<ContactDTO>
    {
        public ContactValidator()
        {
            RuleFor(contact => contact.Name)
                .NotEmpty().WithMessage("Name is required")
                .Length(1, 100).WithMessage("Name can't be longer than 100 characters");

            RuleFor(contact => contact.Phone)
                .NotEmpty().WithMessage("Phone is required")
                .Matches(@"^\d{10,15}$").WithMessage("Phone must be between 10 and 15 digits");

            RuleFor(contact => contact.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(contact => contact.RegionCode)
                .NotEmpty().WithMessage("RegionCode is required")
                .Length(3).WithMessage("RegionCode must be 3 characters");
        }
    }
}
