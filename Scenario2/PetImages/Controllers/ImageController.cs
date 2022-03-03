// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using PetImages.Contracts;
using PetImages.Entities;
using PetImages.Exceptions;
using PetImages.Storage;
using System.Threading.Tasks;

namespace PetImages.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly ICosmosContainer AccountContainer;
        private readonly ICosmosContainer ImageContainer;

        public ImageController(ICosmosContainer accountContainer, ICosmosContainer imageContainer)
        {
            this.AccountContainer = accountContainer;
            this.ImageContainer = imageContainer;
        }

        /// <summary>
        /// Create or update an image (as long as it has a newer last modified timestamp)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Image>> CreateOrUpdateImageAsync(string accountName, Image image)
        {
            if (!await StorageHelper.DoesItemExist<AccountItem>(this.AccountContainer, partitionKey: accountName, id: accountName))
            {
                return this.NotFound();
            }

            // TODO: Implement the logic to only create/update an image if its
            // last modified timestamp is greater than the existing imag (if one exists)
            var imageItem = image.ToItem();
            imageItem = await this.ImageContainer.UpsertItem(imageItem);

            return this.Ok(imageItem.ToImage());
        }

        [HttpGet]
        public async Task<ActionResult<Image>> GetImageAsync(string accountName, string imageName)
        {
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

    }
}
