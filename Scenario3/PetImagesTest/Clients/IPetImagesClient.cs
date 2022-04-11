// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using PetImages.Contracts;

namespace PetImagesTest.Clients
{
    public interface IPetImagesClient
    {
        public Task<ServiceResponse<Account>> CreateAccountAsync(Account account);

        public Task<ServiceResponse<ImageRecord>> CreateOrUpdateImageAsync(string accountName, ImageRecord image);

        public Task<ServiceResponse<ImageRecord>> GetImageRecordAsync(string accountName, string imageName);

        public Task<ServiceResponse<byte[]>> GetImageAsync(string accountName, string imageName);

        public Task<ServiceResponse<byte[]>> GetImageThumbnailAsync(string accountName, string imageName);
    }
}
