// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace PetImages.Middleware
{
    public class RequestIdMiddleware
    {
        private readonly RequestDelegate next;

        public RequestIdMiddleware(RequestDelegate next)
        {
            this.next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            var requestId = Guid.NewGuid().ToString().Replace("-", string.Empty);
            Logger.AsyncLocalRequestId.Value = requestId;

            var requestDescription = $"{httpContext.Request.Method} {httpContext.Request.Path}";
            Logger.WriteLine($"Starting HTTP request {requestDescription}");

            try
            {
                await next(httpContext);
            }
            finally
            {
                Logger.WriteLine($"Finishing HTTP request {requestDescription}");
            }
        }
    }
}
