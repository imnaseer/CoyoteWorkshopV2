// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using PetImages.Contracts;
using PetImages.Entities;
using PetImages.Exceptions;
using PetImages.Messaging;
using PetImages.Messaging.Worker;
using PetImages.Storage;

namespace PetImages.Worker
{
    public class GenerateThumbnailWorker : IWorker
    {
        private readonly ICosmosContainer AccountContainer;
        private readonly ICosmosContainer ImageRecordContainer;
        private readonly IStorageAccount StorageAccount;

        public GenerateThumbnailWorker(
            IAccountContainer accountContainer,
            IImageContainer imageRecordContainer,
            IStorageAccount storageAccount)
        {
            this.AccountContainer = accountContainer;
            this.ImageRecordContainer = imageRecordContainer;
            this.StorageAccount = storageAccount;
        }

        public async Task<WorkerResult> ProcessMessage(Message message)
        {
            var thumbnailMessage = (GenerateThumbnailMessage)message;

            var accountName = thumbnailMessage.AccountName;
            var imageName = thumbnailMessage.ImageName;
            var requestId = thumbnailMessage.RequestId;

            var maybeImageRecordItem = await CosmosHelper.GetItemIfExists<ImageRecordItem>(
                this.ImageRecordContainer,
                partitionKey: imageName,
                id: imageName);

            if (maybeImageRecordItem == null || maybeImageRecordItem.LastTouchedByRequestId != requestId)
            {
                return new WorkerResult
                {
                    ResultCode = WorkerResultCode.Enabled,
                    Message = "Needs Retry.",
                };
            }

            var maybeImageBytes = await StorageHelper.GetBlobIfExists(this.StorageAccount, accountName, maybeImageRecordItem.BlobName);
            if (maybeImageBytes == null)
            {
                return new WorkerResult
                {
                    ResultCode = WorkerResultCode.Faulted, // Should be completed, not faulted
                    Message = "Worker Faulted. Blob not found",
                };
            }

            var thumbnailBytes = GenerateThumbnail(maybeImageBytes);
            var thumbnailBlobName = Guid.NewGuid().ToString();
            await this.StorageAccount.CreateOrUpdateBlockBlobAsync(accountName, thumbnailBlobName, "image/jpeg", thumbnailBytes);

            maybeImageRecordItem.ThumbnailBlobName = thumbnailBlobName;
            maybeImageRecordItem.State = ImageRecordState.Created.ToString();

            try
            {
                await this.ImageRecordContainer.ReplaceItem(maybeImageRecordItem, ifMatchEtag: maybeImageRecordItem.ETag);
            }
            catch (DatabasePreconditionFailedException)
            {
                return new WorkerResult
                {
                    ResultCode = WorkerResultCode.Enabled,
                    Message = "Needs Retry.",
                };
            }

            return new WorkerResult
            {
                ResultCode = WorkerResultCode.Completed,
                Message = "Thumbnail Generated.",
            };
        }

        /// <summary>
        /// Dummy implementation of GenerateThumbnail that returns the same bytes as the image.
        /// </summary>
        private static byte[] GenerateThumbnail(byte[] imageContents) => imageContents;
    }
}
