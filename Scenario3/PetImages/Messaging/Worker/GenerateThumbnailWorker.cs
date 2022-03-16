// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using PetImages.Contracts;
using PetImages.Entities;
using PetImages.Messaging;
using PetImages.Storage;

namespace PetImages.Worker
{
    public class GenerateThumbnailWorker : IWorker
    {
        private readonly ICosmosContainer AccountContainer;
        private readonly ICosmosContainer ImageRecordContainer;
        private readonly IStorageAccount StorageAccount;

        public GenerateThumbnailWorker(
            ICosmosContainer accountContainer,
            ICosmosContainer imageRecordContainer,
            IStorageAccount storageAccount)
        {
            this.AccountContainer = accountContainer;
            this.ImageRecordContainer = imageRecordContainer;
            this.StorageAccount = storageAccount;
        }

        public async Task ProcessMessage(Message message)
        {
            var thumbnailMessage = (GenerateThumbnailMessage)message;

            var accountName = thumbnailMessage.AccountName;
            var imageName = thumbnailMessage.ImageName;

            var maybeImageRecordItem = await CosmosHelper.GetItemIfExists<ImageRecordItem>(
                this.ImageRecordContainer,
                partitionKey: imageName,
                id: imageName);

            if (maybeImageRecordItem == null)
            {
                // retry?
            }

            var maybeImageBytes = await StorageHelper.GetBlobIfExists(this.StorageAccount, accountName, maybeImageRecordItem.BlobName);
            if (maybeImageBytes == null)
            {
                // ???
            }

            var thumbnailBytes = GenerateThumbnail(maybeImageBytes);
            var thumbnailBlobName = Guid.NewGuid().ToString();
            await this.StorageAccount.CreateOrUpdateBlockBlobAsync(accountName, thumbnailBlobName, "image/jpeg", thumbnailBytes);

            maybeImageRecordItem.ThumbnailBlobName = thumbnailBlobName;
            maybeImageRecordItem.State = ImageRecordState.Created.ToString();
            await this.ImageRecordContainer.ReplaceItem(maybeImageRecordItem, ifMatchEtag: maybeImageRecordItem.ETag);
        }

        /// <summary>
        /// Dummy implementation of GenerateThumbnail that returns the same bytes as the image.
        /// </summary>
        private static byte[] GenerateThumbnail(byte[] imageContents) => imageContents;
    }
}
