using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Xunit;

namespace Tests.Integration;

public class ContactsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ContactsControllerTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("TestDb");
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateContact_ValidData_ReturnsCreatedContact()
    {
        var newContact = new ContactDTO
        {
            Name = "Test Contact",
            Email = "test@test.com",
            Phone = "1234567890",
            RegionCode = "ABC"
        };

        var response = await _client.PostAsJsonAsync("/contacts", newContact);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdContact = await response.Content.ReadFromJsonAsync<ContactDTO>();
        Assert.NotNull(createdContact);
        Assert.Equal(newContact.Name, createdContact.Name);
    }

    [Fact]
    public async Task GetContacts_ReturnsAllContacts()
    {
        var response = await _client.GetAsync("/contacts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var contacts = await response.Content.ReadFromJsonAsync<IEnumerable<ContactDTO>>();
        Assert.NotNull(contacts);
    }

    [Fact]
    public async Task GetContactsByRegion_ValidRegion_ReturnsFilteredContacts()
    {
        const string regionCode = "ABC";

        var response = await _client.GetAsync($"/contacts/region/{regionCode}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var contacts = await response.Content.ReadFromJsonAsync<IEnumerable<ContactDTO>>();
        Assert.NotNull(contacts);
        Assert.All(contacts, c => Assert.Equal(regionCode, c.RegionCode));
    }
}