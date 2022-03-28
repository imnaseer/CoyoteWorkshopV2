// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using PetImages.Messaging;
using PetImages.Messaging.Worker;

namespace PetImages.Worker
{
    public interface IWorker
    {
        Task<WorkerResult> ProcessMessage(Message message);
    }
}
