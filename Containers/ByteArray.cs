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

using System;

namespace QuantumBranch.OpenSharedLibrary
{
    /// <summary>
    /// Byte array container class
    /// </summary>
    public sealed class ByteArray : IByteArray
    {
        /// <summary>
        /// Byte array instance
        /// </summary>
        private readonly byte[] value;

        /// <summary>
        /// Class data size in bytes
        /// </summary>
        public int ByteSize => value.Length;

        /// <summary>
        /// Byte array value
        /// </summary>
        public byte this[int index]
        {
            get { return value[index]; }
            set { this.value[index] = value; }
        }

        /// <summary>
        /// Creates a new byte array container class instance
        /// </summary>
        public ByteArray(byte[] value)
        {
            this.value = value;
        }

        /// <summary>
        /// Returns true byte array instance is equal to the object
        /// </summary>
        public override bool Equals(object obj)
        {
            return value.Equals(obj);
        }
        /// <summary>
        /// Returns byte array instance hash code
        /// </summary>
        public override int GetHashCode()
        {
            return value.GetHashCode();
        }
        /// <summary>
        /// Returns byte array instance string value
        /// </summary>
        public override string ToString()
        {
            return value.ToString();
        }

        /// <summary>
        /// Converts class data to the byte array
        /// </summary>
        public void ToBytes(byte[] array, int index)
        {
            Buffer.BlockCopy(value, 0, array, index, value.Length);
        }
        /// <summary>
        /// Converts class data to the byte array
        /// </summary>
        public byte[] ToBytes()
        {
            return value;
        }

        /// <summary>
        /// Converts class data to the byte array
        /// </summary>
        public static void ToBytes(IByteArray byteArray, byte[] array, int index)
        {
            byteArray.ToBytes(array, index);
        }
        /// <summary>
        /// Converts class data to the byte array
        /// </summary>
        public static byte[] ToBytes(IByteArray byteArray)
        {
            return byteArray.ToBytes();
        }

        /// <summary>
        /// Converts byte array to the class instance
        /// </summary>
        public static implicit operator byte[](ByteArray v) { return v.ToBytes(); }
        /// <summary>
        /// Converts class instance to the byte array
        /// </summary>
        public static implicit operator ByteArray(byte[] v) { return new ByteArray(v); }
    }
}