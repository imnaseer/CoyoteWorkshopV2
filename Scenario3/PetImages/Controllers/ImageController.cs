// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PetImages.Contracts;
using PetImages.Entities;
using PetImages.Exceptions;
using PetImages.Storage;
using PetImages.Messaging;

namespace PetImages.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly ICosmosContainer AccountContainer;
        private readonly ICosmosContainer ImageRecordContainer;
        private readonly IStorageAccount StorageAccount;
        private readonly IMessagingClient MessagingClient;

        private static HashAlgorithm hashAlgorithm = HashAlgorithm.Create("SHA-256");

        public ImageController(
            IAccountContainer accountContainer,
            IImageContainer imageRecordContainer,
            IStorageAccount storageAccount,
            IMessagingClient messagingClient)
        {
            this.AccountContainer = accountContainer;
            this.ImageRecordContainer = imageRecordContainer;
            this.StorageAccount = storageAccount;
            this.MessagingClient = messagingClient;
        }

        /// <summary>
        /// ...
        /// </summary>
        [HttpPost("{accountName}")]
        [NonAction]
        public async Task<ActionResult<ImageRecord>> CreateImageRecordSecondScenarioAsync(
            [FromRoute] string accountName,
            [FromBody] ImageRecord imageRecord)
        {
            var maybeError = await ValidateImageRecordAsync(accountName, imageRecord);
            if (maybeError != null)
            {
                return this.BadRequest(maybeError);
            }

            var imageRecordItem = imageRecord.ToImageRecordItem();

            var maybeExistingImageRecordItem = await CosmosHelper.GetItemIfExists<ImageRecordItem>(
                ImageRecordContainer,
                imageRecordItem.PartitionKey,
                imageRecordItem.Id);

            if (maybeExistingImageRecordItem == null)
            {
                try
                {
                    imageRecordItem = await this.ImageRecordContainer.CreateItem(imageRecordItem);
                }
                catch (DatabaseItemAlreadyExistsException)
                {
                    return this.Conflict();
                }
            }
            else
            {
                if (imageRecordItem.LastModifiedTimestamp < maybeExistingImageRecordItem.LastModifiedTimestamp)
                {
                    return this.BadRequest(ErrorFactory.StaleLastModifiedTime(
                        imageRecordItem.LastModifiedTimestamp,
                        maybeExistingImageRecordItem.LastModifiedTimestamp));
                }

                try
                {
                    await this.ImageRecordContainer.UpsertItem(imageRecordItem, maybeExistingImageRecordItem.ETag);
                }
                catch (DatabasePreconditionFailedException)
                {
                    return this.Conflict();
                }
            }

            return this.Ok(imageRecordItem.ToImageRecord());
        }

        /// <summary>
        /// ...
        /// </summary>
        [HttpPost("{accountName}")]
        [NonAction]
        public async Task<ActionResult<ImageRecord>> CreateImageRecordThirdScenarioBuggyAsync(string accountName, ImageRecord imageRecord)
        {
            var maybeError = await ValidateImageRecordAsync(accountName, imageRecord);
            if (maybeError != null)
            {
                return this.BadRequest(maybeError);
            }

            // We upload the image to Azure Storage, before adding an entry to Cosmos DB
            // so that it is guaranteed to be there when user does a GET request.
            // Note: we're calling CreateOrUpdateBlobAsync because Azure Storage doesn't
            // have a create-only API.
            await StorageHelper.CreateContainerIfNotExists(this.StorageAccount, accountName);
            await this.StorageAccount.CreateOrUpdateBlockBlobAsync(accountName, imageRecord.Name, imageRecord.ContentType, imageRecord.Content);

            var imageRecordItem = imageRecord.ToImageRecordItem();
            imageRecordItem.BlobName = imageRecord.Name; // TODO: Remove this line in workshop code
            var maybeExistingImageRecordItem = await CosmosHelper.GetItemIfExists<ImageRecordItem>(
                ImageRecordContainer,
                imageRecordItem.PartitionKey,
                imageRecordItem.Id);

            if (maybeExistingImageRecordItem == null)
            {
                try
                {
                    imageRecordItem = await this.ImageRecordContainer.CreateItem(imageRecordItem);
                }
                catch (DatabaseItemAlreadyExistsException)
                {
                    return this.Conflict();
                }
            }
            else
            {
                if (imageRecordItem.LastModifiedTimestamp < maybeExistingImageRecordItem.LastModifiedTimestamp)
                {
                    return this.BadRequest(ErrorFactory.StaleLastModifiedTime(
                        imageRecordItem.LastModifiedTimestamp,
                        maybeExistingImageRecordItem.LastModifiedTimestamp));
                }

                try
                {
                    await this.ImageRecordContainer.UpsertItem(imageRecordItem, maybeExistingImageRecordItem.ETag);
                }
                catch (DatabasePreconditionFailedException)
                {
                    return this.Conflict();
                }
            }

            return this.Ok(imageRecordItem.ToImageRecord());
        }

        /// <summary>
        /// ...
        /// </summary>
        [HttpPost("{accountName}")]
        [NonAction]
        public async Task<ActionResult<ImageRecord>> CreateImageRecordThirdScenarioFixedAsync(string accountName, ImageRecord imageRecord)
        {
            var maybeError = await ValidateImageRecordAsync(accountName, imageRecord);
            if (maybeError != null)
            {
                return this.BadRequest(maybeError);
            }

            var imageRecordItem = imageRecord.ToImageRecordItem(
                blobName: Guid.NewGuid().ToString());

            // We upload the image to Azure Storage, before adding an entry to Cosmos DB
            // so that it is guaranteed to be there when user does a GET request.
            // Note: we're calling CreateOrUpdateBlobAsync because Azure Storage doesn't
            // have a create-only API.
            await StorageHelper.CreateContainerIfNotExists(this.StorageAccount, accountName);
            await this.StorageAccount.CreateOrUpdateBlockBlobAsync(accountName, imageRecordItem.BlobName, imageRecord.ContentType, imageRecord.Content);

            var maybeExistingImageRecordItem = await CosmosHelper.GetItemIfExists<ImageRecordItem>(
                ImageRecordContainer,
                imageRecordItem.PartitionKey,
                imageRecordItem.Id);

            if (maybeExistingImageRecordItem == null)
            {
                try
                {
                    imageRecordItem = await this.ImageRecordContainer.CreateItem(imageRecordItem);
                }
                catch (DatabaseItemAlreadyExistsException)
                {
                    return this.Conflict();
                }
            }
            else
            {
                if (imageRecordItem.LastModifiedTimestamp < maybeExistingImageRecordItem.LastModifiedTimestamp)
                {
                    return this.BadRequest(ErrorFactory.StaleLastModifiedTime(
                        imageRecordItem.LastModifiedTimestamp,
                        maybeExistingImageRecordItem.LastModifiedTimestamp));
                }

                try
                {
                    await this.ImageRecordContainer.UpsertItem(imageRecordItem, maybeExistingImageRecordItem.ETag);
                }
                catch (DatabasePreconditionFailedException)
                {
                    return this.Conflict();
                }
            }

            return this.Ok(imageRecordItem.ToImageRecord());
        }

        /// <summary>
        /// ...
        /// </summary>
        [HttpPost("{accountName}")]
        public async Task<ActionResult<ImageRecord>> CreateImageRecordFourthScenarioAsync(string accountName, ImageRecord imageRecord)
        {
            var maybeError = await ValidateImageRecordAsync(accountName, imageRecord);
            if (maybeError != null)
            {
                return this.BadRequest(maybeError);
            }

            var requestId = Guid.NewGuid().ToString();

            await this.MessagingClient.SubmitMessage(new GenerateThumbnailMessage()
            {
                AccountName = accountName,
                ImageName = imageRecord.Name,
                RequestId = requestId
            });

            var imageRecordItem = imageRecord.ToImageRecordItem(blobName: Guid.NewGuid().ToString());
            imageRecordItem.State = ImageRecordState.Creating.ToString();
            imageRecordItem.LastTouchedByRequestId = requestId;

            // We upload the image to Azure Storage, before adding an entry to Cosmos DB
            // so that it is guaranteed to be there when user does a GET request.
            // Note: we're calling CreateOrUpdateBlobAsync because Azure Storage doesn't
            // have a create-only API.
            await StorageHelper.CreateContainerIfNotExists(this.StorageAccount, accountName);
            await this.StorageAccount.CreateOrUpdateBlockBlobAsync(accountName, imageRecordItem.BlobName, imageRecord.ContentType, imageRecord.Content);

            var maybeExistingImageRecordItem = await CosmosHelper.GetItemIfExists<ImageRecordItem>(
                ImageRecordContainer,
                imageRecordItem.PartitionKey,
                imageRecordItem.Id);

            if (maybeExistingImageRecordItem == null)
            {
                try
                {
                    imageRecordItem = await this.ImageRecordContainer.CreateItem(imageRecordItem);
                }
                catch (DatabaseItemAlreadyExistsException)
                {
                    return this.Conflict();
                }
            }
            else
            {
                if (imageRecordItem.LastModifiedTimestamp < maybeExistingImageRecordItem.LastModifiedTimestamp)
                {
                    return this.BadRequest(ErrorFactory.StaleLastModifiedTime(
                        imageRecordItem.LastModifiedTimestamp,
                        maybeExistingImageRecordItem.LastModifiedTimestamp));
                }

                try
                {
                    await this.ImageRecordContainer.UpsertItem(imageRecordItem, maybeExistingImageRecordItem.ETag);
                }
                catch (DatabasePreconditionFailedException)
                {
                    return this.Conflict();
                }
            }

            return this.Ok(imageRecordItem.ToImageRecord());
        }

        [HttpGet("{accountName}/{imageName}")]
        public async Task<ActionResult<ImageRecord>> GetImageRecord(
            [FromRoute] string accountName,
            [FromRoute] string imageName)
        {
            var maybeError = await ValidateAccountAsync(accountName);
            if (maybeError != null)
            {
                return this.BadRequest(maybeError);
            }

            try
            {
                var imageRecordItem = await this.ImageRecordContainer.GetItem<ImageRecordItem>(partitionKey: imageName, id: imageName);
                return this.Ok(imageRecordItem.ToImageRecord());
            }
            catch (DatabaseItemDoesNotExistException)
            {
                return this.NotFound();
            }
        }

        [HttpDelete("{accountName}/{imageName}")]
        public async Task<ActionResult> DeleteImageRecord(string accountName, string imageName)
        {
            var maybeError = await ValidateAccountAsync(accountName);
            if (maybeError != null)
            {
                return this.BadRequest(maybeError);
            }

            try
            {
                var imageRecordItem = await this.ImageRecordContainer.GetItem<ImageRecordItem>(partitionKey: imageName, id: imageName);
                
                await this.ImageRecordContainer.DeleteItem(partitionKey: imageName, id: imageName);
                await StorageHelper.DeleteBlobIfExists(this.StorageAccount, accountName, imageRecordItem.BlobName);

                return this.Ok();
            }
            catch (DatabaseItemDoesNotExistException)
            {
                return this.NoContent();
            }
        }

        // TODO: Fix this, its an action
        [HttpGet("{accountName}/{imageName}/content")]
        public async Task<ActionResult<byte[]>> GetImageContentsAsync(
            [FromRoute] string accountName,
            [FromRoute] string imageName)
        {
            var maybeError = await ValidateAccountAsync(accountName);
            if (maybeError != null)
            {
                return this.BadRequest(maybeError);
            }

            try
            {
                var imageRecordItem = await this.ImageRecordContainer.GetItem<ImageRecordItem>(partitionKey: imageName, id: imageName);
                var maybeBytes = await StorageHelper.GetBlobIfExists(this.StorageAccount, accountName, imageRecordItem.BlobName);

                if (maybeBytes == null)
                {
                    return this.NotFound();
                }

                return this.Ok(maybeBytes);
            }
            catch (DatabaseItemDoesNotExistException)
            {
                return this.NotFound();
            }
        }

        [HttpGet("{accountName}/{imageName}/thumbnail")]
        public async Task<ActionResult<byte[]>> GetImageThumbailAsync(
            [FromRoute] string accountName,
            [FromRoute] string imageName)
        {
            var maybeError = await ValidateAccountAsync(accountName);
            if (maybeError != null)
            {
                return this.BadRequest(maybeError);
            }

            try
            {
                var imageRecordItem = await this.ImageRecordContainer.GetItem<ImageRecordItem>(partitionKey: imageName, id: imageName);
                var maybeBytes = await StorageHelper.GetBlobIfExists(this.StorageAccount, accountName, imageRecordItem.ThumbnailBlobName);

                if (maybeBytes == null)
                {
                    return this.NotFound();
                }

                return this.Ok(maybeBytes);
            }
            catch (DatabaseItemDoesNotExistException)
            {
                return this.NotFound();
            }
        }

        private async Task<Error> ValidateImageRecordAsync(string accountName, ImageRecord imageRecord)
        {
            var maybeError = await ValidateAccountAsync(accountName);
            if (maybeError != null)
            {
                return maybeError;
            }

            if (imageRecord == null)
            {
                return ErrorFactory.ParsingError(nameof(ImageRecord));
            }

            if (string.IsNullOrWhiteSpace(imageRecord.Name))
            {
                return ErrorFactory.InvalidParameterValueError(nameof(ImageRecord.Name), imageRecord.Name);
            }

            if (string.IsNullOrWhiteSpace(imageRecord.ContentType))
            {
                return ErrorFactory.InvalidParameterValueError(nameof(ImageRecord.ContentType), imageRecord.ContentType);
            }

            if (imageRecord.Content == null)
            {
                return ErrorFactory.InvalidParameterValueError(nameof(ImageRecord.Content), imageRecord.Content);
            }

            return null;
        }

        private async Task<Error> ValidateAccountAsync(string accountName)
        {
            if (!await CosmosHelper.DoesItemExist<AccountItem>(AccountContainer, partitionKey: accountName, id: accountName))
            {
                return ErrorFactory.AccountDoesNotExistError(accountName);
            }

            return null;
        }
    }
}
