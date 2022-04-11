using Microsoft.AspNetCore.Mvc.Testing;
using PetImages;
using PetImages.Contracts;
using PetImagesTest.Exceptions;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace PetImagesTest.Clients
{
    internal class TestServiceClient : IServiceClient, IDisposable
    {
        private readonly HttpClient Client;

        // TODO: Need to fix routes and then write the client
        // TODO: To use the factory, in the tests, do this:
        // using var factory = new ServiceFactory();
        // await factory.InitializeAccountContainerAsync();
        // await factory.InitializeImageContainerAsync();
        // factory.InitializeMessagingClient();
        // using var client = new ServiceClient(factory);
        internal TestServiceClient(ServiceFactory factory)
        {
            this.Client = factory.CreateClient(new WebApplicationFactoryClientOptions()
            {
                AllowAutoRedirect = false,
                HandleCookies = false
            });
        }

        public async Task<ServiceResponse<Account>> CreateAccountAsync(Account account)
        {
            var response = await this.Client.PostAsync(
                new Uri($"Account", UriKind.RelativeOrAbsolute),
                JsonContent.Create(account));

            return await CreateServiceResponseAsync<Account>(response);
        }

        Task<ServiceResponse<ImageRecord>> IServiceClient.CreateOrUpdateImageAsync(string accountName, ImageRecord image)
        {
            throw new NotImplementedException();
        }

        public Task<ServiceResponse<ImageRecord>> GetImageRecordAsync(string accountName, string imageName)
        {
            throw new NotImplementedException();
        }

        Task<ServiceResponse<byte[]>> IServiceClient.GetImageAsync(string accountName, string imageName)
        {
            throw new NotImplementedException();
        }

        Task<ServiceResponse<byte[]>> IServiceClient.GetImageThumbnailAsync(string accountName, string imageName)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            this.Client.Dispose();
        }

        private static async Task<ServiceResponse<T>> CreateServiceResponseAsync<T>(HttpResponseMessage httpResponse)
            where T : class
        {
            var statusCode = (int)httpResponse.StatusCode;
            
            if (statusCode >= 200 && statusCode <= 299)
            {
                return new ServiceResponse<T>()
                {
                    StatusCode = httpResponse.StatusCode,
                    Resource = JsonSerializer.Deserialize<T>(await httpResponse.Content.ReadAsStringAsync())
                };
            }
            else if (statusCode >= 400 && statusCode <= 499)
            {
                return new ServiceResponse<T>()
                {
                    StatusCode = httpResponse.StatusCode,
                    Error = JsonSerializer.Deserialize<Error>(await httpResponse.Content.ReadAsStringAsync())
                };
            }
            else if (statusCode >= 500 && statusCode <= 599)
            {
                throw new InternalServerErrorException(httpResponse);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
