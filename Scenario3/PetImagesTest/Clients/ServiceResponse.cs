// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using PetImages.Contracts;

namespace PetImagesTest.Clients
{
    public class ServiceResponse<T>
    {
        public HttpStatusCode? StatusCode { get; set; }

        public T Resource { get; set; }

        public Error Error { get; set; }
    }
}