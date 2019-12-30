﻿
// Copyright 2019 Nikita Fediuchin (QuantumBranch)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using OpenSharedLibrary.Collections.Bytes;
using OpenSharedLibrary.Entities;
using System.IO;
using System.IO.Compression;

namespace OpenSharedLibrary.Databases
{
    /// <summary>
    /// Disk database class
    /// </summary>
    public class DiskDatabase<TKey, TValue, TFactory> : IDiskDatabase<TKey, TValue, TFactory>
        where TValue : IByteArray, IIdentifiable<TKey>
        where TFactory : IByteArrayFactory<TValue>
    {
        /// <summary>
        /// Database item count
        /// </summary>
        protected int count;

        /// <summary>
        /// Database locker
        /// </summary>
        protected readonly object locker;
        /// <summary>
        /// Database directory path
        /// </summary>
        protected readonly string path;
        /// <summary>
        /// Use GZip file compression
        /// </summary>
        protected readonly bool useCompression;

        /// <summary>
        /// Database item count
        /// </summary>
        public int Count => count;
        /// <summary>
        /// Database directory path
        /// </summary>
        public string Path => path;
        /// <summary>
        /// Use GZip file compression
        /// </summary>
        public bool UseCompression => useCompression;

        /// <summary>
        /// Creates a new disk database class instance
        /// </summary>
        public DiskDatabase(string path, bool useCompression)
        {
            locker = new object();
            this.path = path;
            this.useCompression = useCompression;
        }

        /// <summary>
        /// Loads concurrent database data
        /// </summary>
        public void Load()
        {
            lock (locker)
            {
                if (Directory.Exists(path))
                    count = Directory.GetFiles(path).Length;
                else
                    Directory.CreateDirectory(path);
            }
        }
        /// <summary>
        /// Unloads concurrent database data
        /// </summary>
        public void Unload() { }

        /// <summary>
        /// Returns true if the database contains key
        /// </summary>
        public bool ContainsKey(TKey key)
        {
            lock (locker)
                return File.Exists($"{path}{key}");
        }

        /// <summary>
        /// Adds a new item to the database
        /// </summary>
        public bool TryAdd(TValue value)
        {
            var array = new byte[value.ByteArraySize];

            using (var memoryStream = new MemoryStream(array))
            {
                using(var binaryWriter = new BinaryWriter(memoryStream))
                {
                    value.ToBytes(binaryWriter);
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    lock (locker)
                    {
                        if (TryWriteStream(value, FileMode.CreateNew, memoryStream))
                        {
                            count++;
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Updates an existing item in the database
        /// </summary>
        public bool TryUpdate(TValue value)
        {
            var array = new byte[value.ByteArraySize];

            using (var memoryStream = new MemoryStream(array))
            {
                using (var binaryWriter = new BinaryWriter(memoryStream))
                {
                    value.ToBytes(binaryWriter);
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    lock (locker)
                        return TryWriteStream(value, FileMode.Create, memoryStream);
                }
            }
        }
        /// <summary>
        /// Adds new or updates existing item in the database
        /// </summary>
        public bool AddOrUpdate(TValue value)
        {
            var array = new byte[value.ByteArraySize];

            using (var memoryStream = new MemoryStream(array))
            {
                using (var binaryWriter = new BinaryWriter(memoryStream))
                {
                    value.ToBytes(binaryWriter);
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    lock (locker)
                    {
                        if (TryWriteStream(value, FileMode.CreateNew, memoryStream))
                        {
                            count++;
                            return true;
                        }
                        else
                        {
                            return TryWriteStream(value, FileMode.Create, memoryStream);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Removes an existing item from the database
        /// </summary>
        public bool TryRemove(TKey key)
        {
            lock (locker)
            {
                try
                {
                    File.Delete($"{path}{key}");
                    count--;
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
        /// <summary>
        /// Removes an existing item from the database
        /// </summary>
        public bool TryRemove(TKey key, TFactory factory, out TValue value)
        {
            lock (locker)
            {
                if (TryGetValue(key, factory, out value))
                {
                    try
                    {
                        File.Delete($"{path}{key}");
                        count--;
                        return true;
                    }
                    catch
                    {
                        value = default;
                        return false;
                    }
                }
                else
                {
                    value = default;
                    return false;
                }
            }
        }
        /// <summary>
        /// Returns an existing item from the database
        /// </summary>
        public bool TryGetValue(TKey key, TFactory factory, out TValue value)
        {
            var array = new byte[factory.ByteArraySize];

            using (var memoryStream = new MemoryStream(array))
            {
                bool result;

                lock (locker)
                    result = TryReadStream(key, memoryStream);

                if (result)
                {
                    using (var binaryReader = new BinaryReader(memoryStream))
                    {
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        value = factory.Create(binaryReader);
                        return true;
                    }
                }
                else
                {
                    value = default;
                    return false;
                }
            }
        }

        /// <summary>
        /// Removes all items from the database
        /// </summary>
        public void Clear()
        {
            lock (locker)
            {
                var files = Directory.GetFiles(path);

                foreach(var file in files)
                    File.Delete(file);
            }
        }

        /// <summary>
        /// Writes item to the database stream
        /// </summary>
        protected bool TryWriteStream(TValue value, FileMode fileMode, MemoryStream memoryStream)
        {
            try
            {
                using (var fileStream = new FileStream($"{path}{value.ID}", fileMode, FileAccess.Write))
                {
                    if (useCompression)
                    {
                        using (var gzipStream = new GZipStream(fileStream, CompressionMode.Compress))
                            memoryStream.CopyTo(gzipStream);
                    }
                    else
                    {
                        memoryStream.CopyTo(fileStream);
                    }

                    return true;
                }   
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// Reads item from the database stream
        /// </summary>
        protected bool TryReadStream(TKey key, MemoryStream memoryStream)
        {
            try
            {
                using (var fileStream = new FileStream($"{path}{key}", FileMode.Open, FileAccess.Read))
                {
                    if (useCompression)
                    {
                        using (var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
                            gzipStream.CopyTo(memoryStream);
                    }
                    else
                    {
                        fileStream.CopyTo(memoryStream);
                    }

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}