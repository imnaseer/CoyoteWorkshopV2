using Microsoft.AspNetCore.Mvc.Testing;
using PetImages;
using PetImages.Contracts;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace PetImagesTest.Clients
{
    internal class ServiceClient : IClient
    {
        private readonly HttpClient Client;

        // TODO: Need to fix routes and then write the client
        // TODO: To use the factory, in the tests, do this:
        // using var factory = new ServiceFactory();
        // await factory.InitializeAccountContainerAsync();
        // await factory.InitializeImageContainerAsync();
        // factory.InitializeMessagingClient();
        // using var client = new ServiceClient(factory);
        internal ServiceClient(ServiceFactory factory)
        {
            this.Client = factory.CreateClient(new WebApplicationFactoryClientOptions()
            {
                AllowAutoRedirect = false,
                HandleCookies = false
            });
        }

        public async Task<HttpStatusCode> CreateAccountAsync(Account account)
        {
            var response = await this.Client.PostAsync(new Uri($"Account", UriKind.RelativeOrAbsolute),
                JsonContent.Create(account));
            return response.StatusCode;
        }

        public Task<HttpStatusCode> CreateImageAsync(string accountName, ImageRecord image)
        {
            throw new NotImplementedException();
        }

        public Task<(HttpStatusCode, ImageRecord)> CreateOrUpdateImageAsync(string accountName, ImageRecord image)
        {
            throw new NotImplementedException();
        }

        public Task<(HttpStatusCode, byte[])> GetImageAsync(string accountName, string imageName)
        {
            throw new NotImplementedException();
        }

        public Task<(HttpStatusCode, byte[])> GetImageThumbnailAsync(string accountName, string imageName)
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> DeleteAccountAsync(string accountName)
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> DeleteAsync(string accountName, string imageName)
        {
            throw new NotImplementedException();
        }

        public Task<(HttpStatusCode, Account)> GetAccountAsync(string accountName)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            this.Client.Dispose();
        }
    }
}
