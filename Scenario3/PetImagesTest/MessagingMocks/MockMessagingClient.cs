// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using PetImages.Messaging;
using PetImages.Messaging.Worker;
using PetImages.Storage;
using PetImages.Worker;
using PetImagesTest.Exceptions;
using System;
using System.Threading.Tasks;

namespace PetImagesTest.MessagingMocks
{
    public class MockMessagingClient : IMessagingClient
    {
        private readonly IWorker GenerateThumbnailWorker;

        public MockMessagingClient(
            IAccountContainer accountContainer,
            IImageContainer imageContainer,
            IStorageAccount storageAccount)
        {
            this.GenerateThumbnailWorker = new GenerateThumbnailWorker(accountContainer, imageContainer, storageAccount);
        }

        public Task SubmitMessage(Message message)
        {
            // Fire-and-forget the task to model sending an asynchronous message over the network.
            _ = Task.Run(async () =>
            {
                try
                {
                    if (message.Type == Message.GenerateThumbnailMessageType)
                    {
                        var clonedMessage = TestHelper.Clone((GenerateThumbnailMessage)message);
                        var workerResult = await this.RunThumbnailWorkerWithRetryAsync(clonedMessage);
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
                catch (Exception ex)
                {
                    Specification.Assert(false, $"Uncaught exception in worker: {ex}");
                }
            });

            return Task.CompletedTask;
        }

        private async Task<WorkerResult> RunThumbnailWorkerWithRetryAsync(GenerateThumbnailMessage message)
        {
            WorkerResult workerResult = null;
            do
            {
                try
                {
                    workerResult = await this.GenerateThumbnailWorker.ProcessMessage(message);
                }
                catch (SimulatedRandomFaultException)
                {
                }
            }
            while (workerResult == null || workerResult.ResultCode == WorkerResultCode.Retry);

            return workerResult;
        }
    }
}
