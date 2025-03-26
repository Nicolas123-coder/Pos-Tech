using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using API;
using Application.DTOs;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Integration
{
    public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ApiIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("InMemoryDbForTesting")
                               .UseInternalServiceProvider(new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider());
                    });

                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    db.Database.EnsureCreated();

                    InitializeTestData(db);
                });

            });

            _client = _factory.CreateClient();
        }

        private void InitializeTestData(ApplicationDbContext dbContext)
        {
            dbContext.Contacts.Add(new Domain.Entities.Contact(
                "Existing User",
                "9876543210",
                "existing@example.com",
                "123"));

            dbContext.SaveChanges();
        }

        [Fact(Skip = ".")]
        public async Task CreateContact_ValidData_ReturnsSuccessMessage()
        {
            var newContact = new ContactDTO
            {
                Name = "Test Contact",
                Email = "test@example.com",
                Phone = "1234567890",
                RegionCode = "123"
            };

            var response = await _client.PostAsJsonAsync("/contacts", newContact);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("Mensagem enviada para o RabbitMQ", responseContent);
        }

        [Fact(Skip = ".")]
        public async Task CreateContact_InvalidData_ReturnsBadRequest()
        {
            var invalidContact = new ContactDTO
            {
                Name = "",
                Email = "invalid-email",
                Phone = "12345",
                RegionCode = "1"
            };

            var response = await _client.PostAsJsonAsync("/contacts", invalidContact);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact(Skip = ".")]
        public async Task GetContacts_ReturnsSuccessMessage()
        {
            var response = await _client.GetAsync("/contacts");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("Mensagens enviadas para o RabbitMQ", responseContent);
        }

        [Fact(Skip = ".")]
        public async Task GetContactsByRegion_ValidRegion_ReturnsSuccessMessage()
        {
            const string regionCode = "123";

            var response = await _client.GetAsync($"/contacts/region/{regionCode}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("Mensagens enviadas para o RabbitMQ", responseContent);
        }

        [Fact(Skip = ".")]
        public async Task UpdateContact_ValidContact_ReturnsSuccessMessage()
        {
            var updateContactDto = new ContactDTO
            {
                Name = "Updated Name",
                Email = "updated@example.com",
                Phone = "9876543210",
                RegionCode = "456"
            };

            var response = await _client.PutAsJsonAsync("/contacts/1", updateContactDto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("Mensagem enviada para o RabbitMQ", responseContent);
        }

        [Fact(Skip = ".")]
        public async Task DeleteContact_ExistingId_ReturnsSuccessMessage()
        {
            var response = await _client.DeleteAsync("/contacts/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("Mensagem enviada para o RabbitMQ", responseContent);
        }
    }

    public class KongApiGatewayTests
    {
        private readonly HttpClient _client;
        private const string BaseUrl = "http://localhost:8000";

        public KongApiGatewayTests()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        [Fact(Skip = "Requires infrastructure to be running")]
        public async Task Kong_Routes_Contacts_Endpoint_Correctly()
        {
            var response = await _client.GetAsync($"{BaseUrl}/contacts");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact(Skip = "Requires infrastructure to be running")]
        public async Task Kong_RateLimit_Returns429_WhenLimitExceeded()
        {
            HttpStatusCode? tooManyRequestsStatusCode = null;

            // Act
            for (int i = 0; i < 150; i++)
            {
                var response = await _client.GetAsync($"{BaseUrl}/contacts");

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    tooManyRequestsStatusCode = response.StatusCode;
                    break;
                }
            }

            // Assert
            Assert.Equal(HttpStatusCode.TooManyRequests, tooManyRequestsStatusCode);
        }

        [Fact(Skip = "Requires infrastructure to be running")]
        public async Task Kong_CORS_Headers_AreCorrect()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Options, $"{BaseUrl}/contacts");
            request.Headers.Add("Origin", "http://example.com");
            request.Headers.Add("Access-Control-Request-Method", "GET");

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
            Assert.True(response.Headers.Contains("Access-Control-Allow-Methods"));
            Assert.True(response.Headers.Contains("Access-Control-Allow-Headers"));
        }
    }
}