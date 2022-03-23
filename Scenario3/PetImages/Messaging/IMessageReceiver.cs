// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace PetImages.Messaging
{
    public interface IMessageReceiver
    {
        Task<Message> ReadMessage();
    }
}
