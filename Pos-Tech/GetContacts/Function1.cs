using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Application.Services;

namespace GetContacts
{
    public class GetContactsFunction
    {
        private readonly ContactService _contactService;

        public GetContactsFunction(ContactService contactService)
        {
            _contactService = contactService;
        }

        [Function("GetContacts")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "contacts")] HttpRequestData req)
        {
            var result = await _contactService.GetContactsByRegionAsync(null);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }

        [Function("GetContactsByRegion")]
        public async Task<HttpResponseData> RunByRegion(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "contacts/region/{regionCode}")] HttpRequestData req,
            string regionCode)
        {
            var result = await _contactService.GetContactsByRegionAsync(regionCode);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }

        [Function("GetContactById")]
        public async Task<HttpResponseData> RunById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "contacts/{id:int}")] HttpRequestData req,
            int id)
        {
            var result = await _contactService.GetContactByIdAsync(id);

            var response = req.CreateResponse(result is not null ? HttpStatusCode.OK : HttpStatusCode.NotFound);
            await response.WriteAsJsonAsync(result);
            return response;
        }
    }
}
