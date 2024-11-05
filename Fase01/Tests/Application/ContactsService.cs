using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Services;
using Application.Validators;
using Domain.Entities;
using Domain.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Xunit;

namespace Tests.API
{
    public class ContactServiceTests
    {
        private readonly Mock<IContactRepository> _mockRepository;
        private readonly IValidator<ContactDTO> _validator;
        private readonly ContactService _service;
        private readonly Contact _validContact;
        private readonly ContactDTO _validContactDto;

        public ContactServiceTests()
        {
            _mockRepository = new Mock<IContactRepository>();
            _validator = new ContactValidator();
            _service = new ContactService(_mockRepository.Object, _validator);

            _validContactDto = new ContactDTO
            {
                Name = "Test User",
                Email = "test@example.com",
                Phone = "11999999999",
                RegionCode = "011"
            };

            _validContact = new Contact(
                _validContactDto.Name,
                _validContactDto.Phone,
                _validContactDto.Email,
                _validContactDto.RegionCode
            );
        }

        [Fact]
        public async Task AddContactAsync_WithValidData_ShouldReturnContact()
        {
            // Arrange
            _mockRepository
                .Setup(x => x.AddContactAsync(It.IsAny<Contact>()))
                .ReturnsAsync(_validContact);

            // Act
            var result = await _service.AddContactAsync(_validContactDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_validContactDto.Name, result.Name);
            Assert.Equal(_validContactDto.Email, result.Email);
        }

        [Fact]
        public async Task AddContactAsync_WithInvalidData_ShouldThrowValidationException()
        {
            // Arrange
            var invalidContactDto = new ContactDTO
            {
                Name = "", // Nome vazio para forçar erro de validação
                Email = "invalid-email", // Email inválido
                Phone = "123", // Telefone inválido
                RegionCode = "0" // RegionCode inválido
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() =>
                _service.AddContactAsync(invalidContactDto));
        }

        [Fact]
        public async Task GetContactsByRegionAsync_WithoutRegionCode_ShouldReturnAllContacts()
        {
            // Arrange
            var contacts = new List<Contact> { _validContact };
            _mockRepository
                .Setup(x => x.GetAllContactsAsync())
                .ReturnsAsync(contacts);

            // Act
            var result = await _service.GetContactsByRegionAsync(null);

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public async Task GetContactsByRegionAsync_WithRegionCode_ShouldReturnFilteredContacts()
        {
            // Arrange
            var regionCode = "011";
            var contacts = new List<Contact> { _validContact };
            _mockRepository
                .Setup(x => x.GetContactsByRegionAsync(regionCode))
                .ReturnsAsync(contacts);

            // Act
            var result = await _service.GetContactsByRegionAsync(regionCode);

            // Assert
            Assert.Single(result);
            Assert.All(result, contact => Assert.Equal(regionCode, contact.RegionCode));
        }

        [Fact]
        public async Task GetContactByIdAsync_WithValidId_ShouldReturnContact()
        {
            // Arrange
            _mockRepository
                .Setup(x => x.GetContactByIdAsync(1))
                .ReturnsAsync(_validContact);

            // Act
            var result = await _service.GetContactByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_validContact.Name, result.Name);
        }

        [Fact]
        public async Task GetContactByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            _mockRepository
                .Setup(x => x.GetContactByIdAsync(999))
                .ReturnsAsync((Contact)null);

            // Act
            var result = await _service.GetContactByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateContactAsync_WithValidData_ShouldReturnUpdatedContact()
        {
            // Arrange
            _mockRepository
                .Setup(x => x.GetContactByIdAsync(1))
                .ReturnsAsync(_validContact);

            _mockRepository
                .Setup(x => x.UpdateContactAsync(It.IsAny<Contact>()))
                .ReturnsAsync(_validContact);

            // Act
            var result = await _service.UpdateContactAsync(1, _validContactDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_validContactDto.Name, result.Name);
        }

        [Fact]
        public async Task UpdateContactAsync_WithInvalidId_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            _mockRepository
                .Setup(x => x.GetContactByIdAsync(999))
                .ReturnsAsync((Contact)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateContactAsync(999, _validContactDto));
        }

        [Fact]
        public async Task UpdateContactAsync_WithInvalidData_ShouldThrowValidationException()
        {
            // Arrange
            _mockRepository
                .Setup(x => x.GetContactByIdAsync(1))
                .ReturnsAsync(_validContact);

            var invalidContactDto = new ContactDTO
            {
                Name = "", // Nome vazio para forçar erro de validação
                Email = "invalid-email", // Email inválido
                Phone = "123", // Telefone inválido
                RegionCode = "0" // RegionCode inválido
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() =>
                _service.UpdateContactAsync(1, invalidContactDto));
        }

        [Fact]
        public async Task DeleteContactAsync_WithValidId_ShouldDeleteContact()
        {
            // Arrange
            _mockRepository
                .Setup(x => x.GetContactByIdAsync(1))
                .ReturnsAsync(_validContact);

            _mockRepository
                .Setup(x => x.DeleteContactAsync(1))
                .Returns(Task.CompletedTask);

            // Act & Assert
            await _service.DeleteContactAsync(1);
            _mockRepository.Verify(x => x.DeleteContactAsync(1), Times.Once);
        }

        [Fact]
        public async Task DeleteContactAsync_WithInvalidId_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            _mockRepository
                .Setup(x => x.GetContactByIdAsync(999))
                .ReturnsAsync((Contact)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.DeleteContactAsync(999));
        }
    }
}