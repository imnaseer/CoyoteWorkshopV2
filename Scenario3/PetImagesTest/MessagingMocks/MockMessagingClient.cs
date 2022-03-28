// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using PetImages.Messaging;
using PetImages.Messaging.Worker;
using PetImages.Storage;
using PetImages.Worker;

namespace PetImagesTest.MessagingMocks
{
    public class MockMessagingClient : IMessagingClient
    {
        private readonly IWorker GenerateThumbnailWorker;

        public MockMessagingClient(IAccountContainer accountContainer, IImageContainer imageContainer, IStorageAccount blobContainer)
        {
            this.GenerateThumbnailWorker = new GenerateThumbnailWorker(accountContainer, imageContainer, blobContainer);
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
                        var workerResult = await this.RunThumbnailWorkerWithRetry(clonedMessage);
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
                catch (Exception)
                {
                    Specification.Assert(false, "Uncaught exception in worker");
                }
            });

            return Task.CompletedTask;
        }

        private async Task<WorkerResult> RunThumbnailWorkerWithRetry(GenerateThumbnailMessage message)
        {
            WorkerResult workerResult;
            do
            {
                workerResult = await this.GenerateThumbnailWorker.ProcessMessage(message);
            }
            while(workerResult == null || workerResult.ResultCode == WorkerResultCode.Enabled);

            return workerResult;
        }
    }
}
