// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PetImages.Messaging;
using PetImages.Messaging.Worker;
using System.Threading.Tasks;

namespace PetImages.Worker
{
    public interface IWorker
    {
        Task<WorkerResult> ProcessMessage(Message message);
    }
}
