using System;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace ClientApp.Controllers
{
    [Route("[controller]")]
    public class IdentityController : Controller
    {
        public async Task<IActionResult> Index()
        {
            var client = new HttpClient();

            // discover endpoints from metadata
            var discoveryClient = client;
            var oidcDiscoveryResult = await discoveryClient.GetDiscoveryDocumentAsync("http://localhost:5000");
            if (oidcDiscoveryResult.IsError)
            {
                Console.WriteLine(oidcDiscoveryResult.Error);
                return Json(oidcDiscoveryResult.Error);
            }

            // request token from token endpoint
            var tokenClient = client;
            var tokenResponse = await tokenClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = oidcDiscoveryResult.TokenEndpoint,

                ClientId = "clientApp",
                ClientSecret = "very secret",

                Parameters =
                {
                    { "scope", "resourceApi" }
                }
            });
            if (tokenResponse.IsError)
            {
                Console.WriteLine(tokenResponse.Error);
                throw new HttpRequestException(tokenResponse.Error);
            }
            Console.WriteLine(tokenResponse.Json);
            Console.WriteLine("\n\n");

            // call api with access token
            client.SetBearerToken(tokenResponse.AccessToken);
            var response = await client.GetAsync("http://localhost:5001/identity");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.StatusCode);
                throw new HttpRequestException(response.StatusCode.ToString());
            }

            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine(JArray.Parse(content));
            return Json(content);
        }
    }
}
