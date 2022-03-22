﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PetImages.Contracts;
using System;

namespace PetImages.Entities
{
    public class ImageRecordItem : DbItem
    {
        public override string PartitionKey => Id;

        public string ContentType { get; set; }

        public string BlobName { get; set; }

        public string ThumbnailBlobName { get; set; }

        public string[] Tags { get; set; }

        public string State { get; set; }

        public DateTime LastModifiedTimestamp { get; set; }

        public ImageRecord ToImageRecord()
        {
            return new ImageRecord()
            {
                Name = Id,
                ContentType = ContentType,
                Tags = Tags,
                State = State,
                LastModifiedTimestamp = LastModifiedTimestamp
            };
        }
    }
}