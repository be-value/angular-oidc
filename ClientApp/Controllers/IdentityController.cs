using System;
using System.Net.Http;
using System.Threading.Tasks;
using ClientApp.Models;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClientApp.Controllers
{
    public class IdentityController : Controller
    {
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userClaimsVm = new UserClaimsVM();
            var userClaimsWithClientCredentials = await GetUserClaimsFromApiWithClientCredentials();
            userClaimsVm.UserClaimsWithClientCredentials = userClaimsWithClientCredentials.IsSuccessStatusCode 
                ? await userClaimsWithClientCredentials.Content.ReadAsStringAsync() 
                : userClaimsWithClientCredentials.StatusCode.ToString();

            var userClaimsWithAccessToken = await CallApiUsingUserAccessToken();
            userClaimsVm.UserClaimsWithAccessToken = userClaimsWithAccessToken.IsSuccessStatusCode
                ? await userClaimsWithAccessToken.Content.ReadAsStringAsync()
                : userClaimsWithAccessToken.StatusCode.ToString();

            return View(userClaimsVm);
        }

        public async Task Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            await HttpContext.SignOutAsync("oidc");
        }

        private async Task<HttpResponseMessage> CallApiUsingUserAccessToken()
        {
            var accessToken = await HttpContext.GetTokenAsync("access_token");

            var client = new HttpClient();
            client.SetBearerToken(accessToken);

            return await client.GetAsync("http://localhost:5001/identity");
        }

        private async Task<HttpResponseMessage> GetUserClaimsFromApiWithClientCredentials()
        {
            var client = new HttpClient();

            // discover endpoints from metadata
            var discoveryClient = client;
            // discover endpoints from metadata
            var oidcDiscoveryResult = await discoveryClient.GetDiscoveryDocumentAsync("http://localhost:5000");
            if (oidcDiscoveryResult.IsError)
            {
                Console.WriteLine(oidcDiscoveryResult.Error);
                throw new HttpRequestException(oidcDiscoveryResult.Error);
            }

            // request token
            var tokenClient = client;
            var tokenResponse = await tokenClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = oidcDiscoveryResult.TokenEndpoint,

                ClientId = "clientApp",
                ClientSecret = "secret",

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

            // call API
            client.SetBearerToken(tokenResponse.AccessToken);

            var response = await client.GetAsync("http://localhost:5001/identity");
            return response;
        }
    }
}
