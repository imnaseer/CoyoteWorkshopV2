// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PetImagesTest.Exceptions
{
    using System;
    using System.Net.Http;

    public class InternalServerErrorException : Exception
    {
        private readonly HttpResponseMessage responseMessage;

        public InternalServerErrorException(HttpResponseMessage responseMessage)
        {
            this.responseMessage = responseMessage;
        }
    }

    public class SimulatedRandomFaultException : Exception
    {
    }
}
