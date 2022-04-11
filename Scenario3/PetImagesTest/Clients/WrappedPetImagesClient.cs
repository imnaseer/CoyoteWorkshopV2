using PetImages.Contracts;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PetImagesTest.Clients
{
    public class WrappedPetImagesClient : IPetImagesClient
    {
        private readonly IAsyncPolicy AsyncPolicy;
        
        private readonly IPetImagesClient Client;

        public WrappedPetImagesClient(IPetImagesClient client, IAsyncPolicy asyncPolicy)
        {
            this.Client = client;
            this.AsyncPolicy = asyncPolicy;
        }

        public Task<ServiceResponse<Account>> CreateAccountAsync(Account account)
        {
            return this.AsyncPolicy.ExecuteAsync(() => this.Client.CreateAccountAsync(account));
        }

        public Task<ServiceResponse<ImageRecord>> CreateOrUpdateImageAsync(string accountName, ImageRecord image)
        {
            return this.AsyncPolicy.ExecuteAsync(() => this.Client.CreateOrUpdateImageAsync(accountName, image));
        }

        public Task<ServiceResponse<byte[]>> GetImageAsync(string accountName, string imageName)
        {
            return this.AsyncPolicy.ExecuteAsync(() => this.Client.GetImageAsync(accountName, imageName));
        }

        public Task<ServiceResponse<ImageRecord>> GetImageRecordAsync(string accountName, string imageName)
        {
            return this.AsyncPolicy.ExecuteAsync(() => this.Client.GetImageRecordAsync(accountName, imageName));
        }

        public Task<ServiceResponse<byte[]>> GetImageThumbnailAsync(string accountName, string imageName)
        {
            return this.AsyncPolicy.ExecuteAsync(() => this.Client.GetImageThumbnailAsync(accountName, imageName));
        }
    }
}
