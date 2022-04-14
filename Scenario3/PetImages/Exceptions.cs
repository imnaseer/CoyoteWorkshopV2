// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace PetImages.Exceptions
{
    public class DatabaseException : Exception
    {
        public DatabaseException()
        {
        }

        public DatabaseException(string message) : base(message)
        {
        }
    }

    public class DatabaseContainerAlreadyExists : DatabaseException
    {
    }

    public class DatabaseContainerDoesNotExist : DatabaseException
    {
    }

    public class DatabaseItemAlreadyExistsException : DatabaseException
    {
    }

    public class DatabaseItemDoesNotExistException : DatabaseException
    {
    }

    public class DatabasePreconditionFailedException : DatabaseException
    {
    }

    public class StorageException : Exception
    {
    }

    public class StorageContainerAlreadyExistsException : StorageException
    {
    }

    public class StorageContainerBeingDeletedException : StorageException
    {
    }

    public class StorageContainerDoesNotExistException : StorageException
    {
    }

    public class BlobDoesNotExistException : StorageException
    {
    }
}
