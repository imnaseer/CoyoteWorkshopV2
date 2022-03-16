namespace PetImages.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using PetImages.Exceptions;

    public static class StorageHelper
    {
        public static async Task CreateContainerIfNotExists(IStorageAccount storageAccount, string containerName)
        {
            try
            {
                await storageAccount.CreateContainerAsync(containerName);
            }
            catch (StorageContainerAlreadyExistsException)
            {
            }
        }

        public static async Task DeleteContainerIfExists(IStorageAccount storageAccount, string containerName)
        {
            try
            {
                await storageAccount.DeleteContainerAsync(containerName);
            }
            catch (StorageContainerDoesNotExistException)
            {
            }
        }
        
        public static async Task<byte[]> GetBlobIfExists(IStorageAccount storageAccount, string containerName, string blobName)
        {
            try
            {
                return await storageAccount.GetBlockBlobAsync(containerName, blobName);
            }
            catch (StorageContainerDoesNotExistException)
            {
                return null;
            }
            catch (BlobDoesNotExistException)
            {
                return null;
            }
        }

        public static async Task DeleteBlobIfExists(IStorageAccount storageAccount, string containerName, string blobName)
        {
            try
            {
                await storageAccount.DeleteBlockBlobAsync(containerName, blobName);
            }
            catch (StorageContainerDoesNotExistException)
            {
            }
            catch (BlobDoesNotExistException)
            {
            }
        }
    }
}
