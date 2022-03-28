using Microsoft.AspNetCore.Mvc.Testing;
using PetImages;
using PetImages.Contracts;
using PetImages.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PetImagesTest.Clients
{
    internal class ServiceClient : IClient
    {
        private readonly HttpClient Client;

        internal ServiceClient(ServiceFactory factory)
        {
            this.Client = factory.CreateClient(new WebApplicationFactoryClientOptions()
            {
                AllowAutoRedirect = false,
                HandleCookies = false
            });
        }

        public Task<HttpStatusCode> CreateAccountAsync(Account account)
        {
            // TODO: Need to fix routes and then write the client
            // TODO: To use the factory, in the tests, do this:
            // using var factory = new ServiceFactory();
            // await factory.InitializeAccountContainerAsync();
            // await factory.InitializeImageContainerAsync();
            // factory.InitializeMessagingClient();
            // using var client = new ServiceClient(factory);
            throw new NotImplementedException();
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

        public void Dispose()
        {
            this.Client.Dispose();
        }
    }
}
