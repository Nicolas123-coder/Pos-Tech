using API.Controllers;
using Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing.Impl;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Tests.Unit.Controllers
{
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseContent;
        private readonly HttpStatusCode _statusCode;

        public FakeHttpMessageHandler(string responseContent = "[]", HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _responseContent = responseContent;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseContent, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }

    public class ContactsControllerTests
    {
        private readonly Mock<ILogger<ContactsController>> _loggerMock;
        private readonly Mock<IConnectionFactory> _connectionFactoryMock;
        private readonly Mock<IConnection> _connectionMock;
        private readonly Mock<IModel> _channelMock;
        private readonly ContactsController _controller;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;

        public ContactsControllerTests()
        {
            _loggerMock = new Mock<ILogger<ContactsController>>();

            _connectionFactoryMock = new Mock<IConnectionFactory>();
            _connectionMock = new Mock<IConnection>();
            _channelMock = new Mock<IModel>();

            _connectionFactoryMock.Setup(cf => cf.CreateConnection()).Returns(_connectionMock.Object);
            _connectionMock.Setup(c => c.CreateModel()).Returns(_channelMock.Object);
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _channelMock = new Mock<IModel>();

            _controller = new ContactsController(
                _loggerMock.Object,
                _httpClientFactoryMock.Object,
                _channelMock.Object
            );
        }

        [Fact]
        public async Task GetAll_ShouldReturnOk_AndLogMessage()
        {
            var httpClient = new HttpClient(new FakeHttpMessageHandler());

            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
                                  .Returns(httpClient);

            var result = await _controller.GetAll();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var contacts = Assert.IsAssignableFrom<IEnumerable<ContactDTO>>(okResult.Value);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Obtendo contatos via Azure Function")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task GetContactById_ShouldReturnOk_AndLogMessage()
        {
            int testId = 123;

            var httpClient = new HttpClient(new FakeHttpMessageHandler("{\"id\":123,\"name\":\"Teste\"}")); // opcional: simula retorno com dados
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
                                  .Returns(httpClient);

            var result = await _controller.GetContactById(testId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var contact = Assert.IsType<ContactDTO>(okResult.Value);
            Assert.Equal("Teste", contact.Name);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Obtendo contato com ID {testId}")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task GetContactsByRegion_ShouldReturnOk_AndLogMessage()
        {
            string regionCode = "123";

            var httpClient = new HttpClient(new FakeHttpMessageHandler("[{\"id\":1,\"name\":\"Regional\"}]"));
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
                                  .Returns(httpClient);

            var result = await _controller.GetContactsByRegion(regionCode);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var contacts = Assert.IsAssignableFrom<IEnumerable<ContactDTO>>(okResult.Value);
            Assert.Single(contacts);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Obtendo contatos da região {regionCode}")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task AddContact_ShouldReturnOk_WhenContactIsValid()
        {
            var contactDto = new ContactDTO
            {
                Name = "Test Contact",
                Email = "test@example.com",
                Phone = "1234567890",
                RegionCode = "123"
            };

            var result = await _controller.AddContact(contactDto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Mensagem enviada para o RabbitMQ.", okResult.Value);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Adicionando um novo contato")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task UpdateContact_ShouldReturnOk_WhenContactIsValid()
        {
            int contactId = 123;
            var contactDto = new ContactDTO
            {
                Name = "Updated Contact",
                Email = "updated@example.com",
                Phone = "9876543210",
                RegionCode = "456"
            };

            var result = await _controller.UpdateContact(contactId, contactDto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Mensagem enviada para o RabbitMQ.", okResult.Value);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Atualizando contato com ID {contactId}")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task DeleteContact_ShouldReturnOk()
        {
            int contactId = 123;

            var result = await _controller.DeleteContact(contactId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Mensagem enviada para o RabbitMQ.", okResult.Value);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Excluindo contato com ID {contactId}")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }
    }
}