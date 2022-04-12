// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using PetImages.Contracts;
using PetImages.Entities;
using PetImages.Exceptions;
using PetImages.Messaging;
using PetImages.Storage;
using System;
using System.Threading.Tasks;

namespace PetImages.Controllers
{
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly ICosmosContainer AccountContainer;
        private readonly ICosmosContainer ImageContainer;
        private readonly IStorageAccount StorageAccount;
        private readonly IMessagingClient MessagingClient;

        public ImageController(
            IAccountContainer accountContainer,
            IImageContainer imageContainer,
            IStorageAccount storageAccount,
            IMessagingClient messagingClient)
        {
            this.AccountContainer = accountContainer;
            this.ImageContainer = imageContainer;
            this.StorageAccount = storageAccount;
            this.MessagingClient = messagingClient;
        }

        /// <summary>
        /// ...
        /// </summary>
        [HttpPost]
        [Route(Routes.Images)]
        [NonAction]
        public async Task<ActionResult<Image>> CreateImageSecondScenarioAsync(
            [FromRoute] string accountName,
            [FromBody] Image image)
        {
            var maybeError = await ValidateImageAsync(accountName, image);
            if (maybeError != null)
            {
                return this.BadRequest(maybeError);
            }

            var imageItem = image.ToImageItem();

            var maybeExistingImageItem = await CosmosHelper.GetItemIfExists<ImageItem>(
                ImageContainer,
                imageItem.PartitionKey,
                imageItem.Id);

            if (maybeExistingImageItem == null)
            {
                try
                {
                    imageItem = await this.ImageContainer.CreateItem(imageItem);
                }
                catch (DatabaseItemAlreadyExistsException)
                {
                    return this.Conflict();
                }
            }
            else
            {
                if (imageItem.LastModifiedTimestamp < maybeExistingImageItem.LastModifiedTimestamp)
                {
                    return this.BadRequest(ErrorFactory.StaleLastModifiedTime(
                        imageItem.LastModifiedTimestamp,
                        maybeExistingImageItem.LastModifiedTimestamp));
                }

                try
                {
                    await this.ImageContainer.UpsertItem(imageItem, maybeExistingImageItem.ETag);
                }
                catch (DatabasePreconditionFailedException)
                {
                    return this.Conflict();
                }
            }

            return this.Ok(imageItem.ToImage());
        }

        /// <summary>
        /// ...
        /// </summary>
        [HttpPost]
        [Route(Routes.Images)]
        [NonAction]
        public async Task<ActionResult<Image>> CreateImageThirdScenarioBuggyAsync(string accountName, Image image)
        {
            var maybeError = await ValidateImageAsync(accountName, image);
            if (maybeError != null)
            {
                return this.BadRequest(maybeError);
            }

            // We upload the image to Azure Storage, before adding an entry to Cosmos DB
            // so that it is guaranteed to be there when user does a GET request.
            // Note: we're calling CreateOrUpdateBlobAsync because Azure Storage doesn't
            // have a create-only API.
            await StorageHelper.CreateContainerIfNotExists(this.StorageAccount, accountName);
            await this.StorageAccount.CreateOrUpdateBlockBlobAsync(accountName, image.Name, image.ContentType, image.Content);

            var imageItem = image.ToImageItem();
            imageItem.BlobName = image.Name; // TODO: Remove this line in workshop code
            var maybeExistingImageItem = await CosmosHelper.GetItemIfExists<ImageItem>(
                ImageContainer,
                imageItem.PartitionKey,
                imageItem.Id);

            if (maybeExistingImageItem == null)
            {
                try
                {
                    imageItem = await this.ImageContainer.CreateItem(imageItem);
                }
                catch (DatabaseItemAlreadyExistsException)
                {
                    return this.Conflict();
                }
            }
            else
            {
                if (imageItem.LastModifiedTimestamp < maybeExistingImageItem.LastModifiedTimestamp)
                {
                    return this.BadRequest(ErrorFactory.StaleLastModifiedTime(
                        imageItem.LastModifiedTimestamp,
                        maybeExistingImageItem.LastModifiedTimestamp));
                }

                try
                {
                    await this.ImageContainer.UpsertItem(imageItem, maybeExistingImageItem.ETag);
                }
                catch (DatabasePreconditionFailedException)
                {
                    return this.Conflict();
                }
            }

            return this.Ok(imageItem.ToImage());
        }

        /// <summary>
        /// ...
        /// </summary>
        [HttpPost]
        [Route(Routes.Images)]
        [NonAction]
        public async Task<ActionResult<Image>> CreateImageThirdScenarioFixedAsync(string accountName, Image image)
        {
            var maybeError = await ValidateImageAsync(accountName, image);
            if (maybeError != null)
            {
                return this.BadRequest(maybeError);
            }

            var imageItem = image.ToImageItem(
                blobName: Guid.NewGuid().ToString());

            // We upload the image to Azure Storage, before adding an entry to Cosmos DB
            // so that it is guaranteed to be there when user does a GET request.
            // Note: we're calling CreateOrUpdateBlobAsync because Azure Storage doesn't
            // have a create-only API.
            await StorageHelper.CreateContainerIfNotExists(this.StorageAccount, accountName);
            await this.StorageAccount.CreateOrUpdateBlockBlobAsync(accountName, imageItem.BlobName, image.ContentType, image.Content);

            var maybeExistingImageItem = await CosmosHelper.GetItemIfExists<ImageItem>(
                ImageContainer,
                imageItem.PartitionKey,
                imageItem.Id);

            if (maybeExistingImageItem == null)
            {
                try
                {
                    imageItem = await this.ImageContainer.CreateItem(imageItem);
                }
                catch (DatabaseItemAlreadyExistsException)
                {
                    return this.Conflict();
                }
            }
            else
            {
                if (imageItem.LastModifiedTimestamp < maybeExistingImageItem.LastModifiedTimestamp)
                {
                    return this.BadRequest(ErrorFactory.StaleLastModifiedTime(
                        imageItem.LastModifiedTimestamp,
                        maybeExistingImageItem.LastModifiedTimestamp));
                }

                try
                {
                    await this.ImageContainer.UpsertItem(imageItem, maybeExistingImageItem.ETag);
                }
                catch (DatabasePreconditionFailedException)
                {
                    return this.Conflict();
                }
            }

            return this.Ok(imageItem.ToImage());
        }

        /// <summary>
        /// ...
        /// </summary>
        [HttpPost]
        [Route(Routes.Images)]
        public async Task<ActionResult<Image>> CreateImageFourthScenarioAsync(string accountName, Image image)
        {
            var maybeError = await ValidateImageAsync(accountName, image);
            if (maybeError != null)
            {
                return this.BadRequest(maybeError);
            }

            var requestId = Guid.NewGuid().ToString();

            await this.MessagingClient.SubmitMessage(new GenerateThumbnailMessage()
            {
                AccountName = accountName,
                ImageName = image.Name,
                RequestId = requestId
            });

            var imageItem = image.ToImageItem(blobName: Guid.NewGuid().ToString());
            imageItem.State = ImageState.Creating.ToString();
            imageItem.LastTouchedByRequestId = requestId;

            // We upload the image to Azure Storage, before adding an entry to Cosmos DB
            // so that it is guaranteed to be there when user does a GET request.
            // Note: we're calling CreateOrUpdateBlobAsync because Azure Storage doesn't
            // have a create-only API.
            await StorageHelper.CreateContainerIfNotExists(this.StorageAccount, accountName);
            await this.StorageAccount.CreateOrUpdateBlockBlobAsync(accountName, imageItem.BlobName, image.ContentType, image.Content);

            var maybeExistingImageItem = await CosmosHelper.GetItemIfExists<ImageItem>(
                ImageContainer,
                imageItem.PartitionKey,
                imageItem.Id);

            if (maybeExistingImageItem == null)
            {
                try
                {
                    imageItem = await this.ImageContainer.CreateItem(imageItem);
                }
                catch (DatabaseItemAlreadyExistsException)
                {
                    return this.Conflict();
                }
            }
            else
            {
                if (imageItem.LastModifiedTimestamp < maybeExistingImageItem.LastModifiedTimestamp)
                {
                    return this.BadRequest(ErrorFactory.StaleLastModifiedTime(
                        imageItem.LastModifiedTimestamp,
                        maybeExistingImageItem.LastModifiedTimestamp));
                }

                try
                {
                    await this.ImageContainer.UpsertItem(imageItem, maybeExistingImageItem.ETag);
                }
                catch (DatabasePreconditionFailedException)
                {
                    return this.Conflict();
                }
            }

            return this.Ok(imageItem.ToImage());
        }

        [HttpGet]
        [Route(Routes.ImageInstance)]
        public async Task<ActionResult<Image>> GetImage(
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
                var imageItem = await this.ImageContainer.GetItem<ImageItem>(partitionKey: imageName, id: imageName);
                return this.Ok(imageItem.ToImage());
            }
            catch (DatabaseItemDoesNotExistException)
            {
                return this.NotFound();
            }
        }

        [HttpDelete]
        [Route(Routes.ImageInstance)]
        public async Task<ActionResult> DeleteImage(string accountName, string imageName)
        {
            var maybeError = await ValidateAccountAsync(accountName);
            if (maybeError != null)
            {
                return this.BadRequest(maybeError);
            }

            try
            {
                var imageItem = await this.ImageContainer.GetItem<ImageItem>(partitionKey: imageName, id: imageName);

                await this.ImageContainer.DeleteItem(partitionKey: imageName, id: imageName);
                await StorageHelper.DeleteBlobIfExists(this.StorageAccount, accountName, imageItem.BlobName);

                return this.Ok();
            }
            catch (DatabaseItemDoesNotExistException)
            {
                return this.NoContent();
            }
        }

        // TODO: Fix this, its an action
        [HttpGet]
        [Route(Routes.ImageContentInstance)]
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
                var imageItem = await this.ImageContainer.GetItem<ImageItem>(partitionKey: imageName, id: imageName);
                var maybeBytes = await StorageHelper.GetBlobIfExists(this.StorageAccount, accountName, imageItem.BlobName);

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

        [HttpGet]
        [Route(Routes.ImageThumbnailInstance)]
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
                var imageItem = await this.ImageContainer.GetItem<ImageItem>(partitionKey: imageName, id: imageName);
                var maybeBytes = await StorageHelper.GetBlobIfExists(this.StorageAccount, accountName, imageItem.ThumbnailBlobName);

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

        private async Task<Error> ValidateImageAsync(string accountName, Image image)
        {
            var maybeError = await ValidateAccountAsync(accountName);
            if (maybeError != null)
            {
                return maybeError;
            }

            if (image == null)
            {
                return ErrorFactory.ParsingError(nameof(Image));
            }

            if (string.IsNullOrWhiteSpace(image.Name))
            {
                return ErrorFactory.InvalidParameterValueError(nameof(Image.Name), image.Name);
            }

            if (string.IsNullOrWhiteSpace(image.ContentType))
            {
                return ErrorFactory.InvalidParameterValueError(nameof(Image.ContentType), image.ContentType);
            }

            if (image.Content == null)
            {
                return ErrorFactory.InvalidParameterValueError(nameof(Image.Content), image.Content);
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
