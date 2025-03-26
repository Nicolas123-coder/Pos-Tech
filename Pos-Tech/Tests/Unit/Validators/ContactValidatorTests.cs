using Application.DTOs;
using Application.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace Tests.Unit.Validators
{
    public class ContactValidatorTests
    {
        private readonly ContactValidator _validator;

        public ContactValidatorTests()
        {
            _validator = new ContactValidator();
        }

        [Fact]
        public void Should_Not_Have_Error_When_Contact_Is_Valid()
        {
            var contact = new ContactDTO
            {
                Name = "Test User",
                Phone = "1234567890",
                Email = "test@example.com",
                RegionCode = "123"
            };

            var result = _validator.TestValidate(contact);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Should_Have_Error_When_Name_Is_Empty()
        {
            var contact = new ContactDTO
            {
                Name = "",
                Phone = "1234567890",
                Email = "test@example.com",
                RegionCode = "123"
            };

            var result = _validator.TestValidate(contact);

            result.ShouldHaveValidationErrorFor(c => c.Name)
                .WithErrorMessage("Name is required");
        }

        [Fact]
        public void Should_Have_Error_When_Name_Is_Too_Long()
        {
            var contact = new ContactDTO
            {
                Name = new string('A', 101),
                Phone = "1234567890",
                Email = "test@example.com",
                RegionCode = "123"
            };

            var result = _validator.TestValidate(contact);

            result.ShouldHaveValidationErrorFor(c => c.Name)
                .WithErrorMessage("Name can't be longer than 100 characters");
        }

        [Fact]
        public void Should_Have_Error_When_Phone_Is_Empty()
        {
            var contact = new ContactDTO
            {
                Name = "Test User",
                Phone = "",
                Email = "test@example.com",
                RegionCode = "123"
            };

            var result = _validator.TestValidate(contact);

            result.ShouldHaveValidationErrorFor(c => c.Phone)
                .WithErrorMessage("Phone is required");
        }

        [Theory]
        [InlineData("123")]
        [InlineData("123456789")]
        [InlineData("1234567890123456")]
        [InlineData("123-456-7890")]
        public void Should_Have_Error_When_Phone_Is_Invalid(string phone)
        {
            var contact = new ContactDTO
            {
                Name = "Test User",
                Phone = phone,
                Email = "test@example.com",
                RegionCode = "123"
            };

            var result = _validator.TestValidate(contact);

            result.ShouldHaveValidationErrorFor(c => c.Phone)
                .WithErrorMessage("Phone must be between 10 and 15 digits");
        }

        [Theory]
        [InlineData("1234567890")]      // 10 dígitos
        [InlineData("12345678901")]     // 11 dígitos
        [InlineData("123456789012345")] // 15 dígitos
        public void Should_Not_Have_Error_When_Phone_Is_Valid(string phone)
        {
            var contact = new ContactDTO
            {
                Name = "Test User",
                Phone = phone,
                Email = "test@example.com",
                RegionCode = "123"
            };

            var result = _validator.TestValidate(contact);

            result.ShouldNotHaveValidationErrorFor(c => c.Phone);
        }

        [Fact]
        public void Should_Have_Error_When_Email_Is_Empty()
        {
            var contact = new ContactDTO
            {
                Name = "Test User",
                Phone = "1234567890",
                Email = "",
                RegionCode = "123"
            };

            var result = _validator.TestValidate(contact);

            result.ShouldHaveValidationErrorFor(c => c.Email)
                .WithErrorMessage("Email is required");
        }

        [Theory]
        [InlineData("not-an-email")]
        [InlineData("invalid")]
        [InlineData("@example.com")]
        public void Should_Have_Error_When_Email_Is_Invalid(string email)
        {
            var contact = new ContactDTO
            {
                Name = "Test User",
                Phone = "1234567890",
                Email = email,
                RegionCode = "123"
            };

            var result = _validator.TestValidate(contact);

            result.ShouldHaveValidationErrorFor(c => c.Email)
                .WithErrorMessage("Invalid email format");
        }

        [Fact]
        public void Should_Have_Error_When_RegionCode_Is_Empty()
        {
            var contact = new ContactDTO
            {
                Name = "Test User",
                Phone = "1234567890",
                Email = "test@example.com",
                RegionCode = ""
            };

            var result = _validator.TestValidate(contact);

            result.ShouldHaveValidationErrorFor(c => c.RegionCode)
                .WithErrorMessage("RegionCode is required");
        }

        [Theory]
        [InlineData("12")]
        [InlineData("1234")]
        public void Should_Have_Error_When_RegionCode_Is_Not_3_Characters(string regionCode)
        {
            var contact = new ContactDTO
            {
                Name = "Test User",
                Phone = "1234567890",
                Email = "test@example.com",
                RegionCode = regionCode
            };

            var result = _validator.TestValidate(contact);

            result.ShouldHaveValidationErrorFor(c => c.RegionCode)
                .WithErrorMessage("RegionCode must be 3 characters");
        }
    }
}