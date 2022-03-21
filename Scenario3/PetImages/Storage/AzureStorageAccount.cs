using Azure.Storage.Blobs;
using PetImages.Exceptions;
using System.IO;
using System.Threading.Tasks;

namespace PetImages.Storage
{
    public class AzureStorageAccount : IStorageAccount
    {
        private readonly BlobServiceClient blobServiceClient;
        
        public AzureStorageAccount()
        {
            // Ref: https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator
            // Directly working with emulator, should come from configuration
            this.blobServiceClient = new BlobServiceClient("UseDevelopmentStorage=true;");
        }

        public async Task CreateContainerAsync(string containerName)
        {
            var containerClient = this.blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();
        }

        public async Task CreateOrUpdateBlockBlobAsync(string containerName, string blobName, string contentType, byte[] blobContents)
        {
            var containerClient = this.blobServiceClient.GetBlobContainerClient(containerName);
            var containerExists = await containerClient.ExistsAsync();
            if (!containerExists)
            {
                throw new StorageContainerDoesNotExistException();
            }

            using (var stream = new MemoryStream())
            {
                // TODO: How to set content type here?
                var blobClient = containerClient.GetBlobClient(blobName);
                stream.Write(blobContents);
                await blobClient.UploadAsync(stream);
            }
        }

        public async Task DeleteBlockBlobAsync(string containerName, string blobName)
        {
            var containerClient = this.blobServiceClient.GetBlobContainerClient(containerName);
            var containerExists = await containerClient.ExistsAsync();
            if (!containerExists)
            {
                throw new StorageContainerDoesNotExistException();
            }

            var blobClient = containerClient.GetBlobClient(blobName);
            var blobExists = await blobClient.ExistsAsync();
            if (!blobExists)
            {
                throw new BlobDoesNotExistException();
            }

            await blobClient.DeleteAsync();
        }

        public async Task DeleteContainerAsync(string containerName)
        {
            var containerClient = this.blobServiceClient.GetBlobContainerClient(containerName);
            var containerExists = await containerClient.ExistsAsync();
            if (!containerExists)
            {
                throw new StorageContainerDoesNotExistException();
            }

            await containerClient.DeleteAsync();
        }

        public async Task<byte[]> GetBlockBlobAsync(string containerName, string blobName)
        {
            var containerClient = this.blobServiceClient.GetBlobContainerClient(containerName);
            var containerExists = await containerClient.ExistsAsync();
            if (!containerExists)
            {
                throw new StorageContainerDoesNotExistException();
            }

            var blobClient = containerClient.GetBlobClient(blobName);
            var blobExists = await blobClient.ExistsAsync();
            if(!blobExists)
            {
                throw new BlobDoesNotExistException();
            }

            using(var blobStream = await blobClient.OpenReadAsync())
            {
                var memoryStream = new MemoryStream();
                blobStream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}
