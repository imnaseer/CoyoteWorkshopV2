using PetImages.Contracts;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PetImagesTest.Clients
{
    public class ResilientPetImagesClient : IPetImagesClient
    {
        private readonly IAsyncPolicy AsyncPolicy;
        
        private readonly IPetImagesClient Client;

        public ResilientPetImagesClient(IPetImagesClient client, IAsyncPolicy asyncPolicy)
        {
            this.Client = client;
            this.AsyncPolicy = asyncPolicy;
        }

        public Task<ServiceResponse<Account>> CreateAccountAsync(Account account)
        {
            return this.AsyncPolicy.ExecuteAsync(() => this.Client.CreateAccountAsync(account));
        }

        public Task<ServiceResponse<ImageRecord>> CreateImageAsync(string accountName, ImageRecord image)
        {
            return this.AsyncPolicy.ExecuteAsync(() => this.Client.CreateImageAsync(accountName, image));
        }

        public Task<ServiceResponse<byte[]>> GetImageAsync(string accountName, string imageName)
        {
            return this.AsyncPolicy.ExecuteAsync(() => this.Client.GetImageAsync(accountName, imageName));
        }
    }
}
