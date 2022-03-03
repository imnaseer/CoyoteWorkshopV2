// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PetImages.Contracts;
using System;

namespace PetImages.Entities
{
    public class ImageItem : DbItem
    {
        public override string PartitionKey => Id;

        public string StorageName { get; set; }

        public string ImageType { get; set; }

        public string[] Tags { get; set; }

        public DateTime LastModifiedTimestamp { get; set; }

        public Image ToImage()
        {
            return new Image()
            {
                Name = Id,
                ImageType = ImageType,
                Tags = Tags,
                LastModifiedTimestamp = LastModifiedTimestamp
            };
        }
    }
}
