﻿using System.Security.Cryptography;
using System.Text;

namespace Vibechat.BusinessLogic.Services.Hashing
{
    public class Sha1Service : IHexHashingService
    {
        private static readonly uint[] _lookup32 = CreateLookup32();

        public Sha1Service()
        {
            HashAlgorithm = SHA1.Create();
        }

        public HashAlgorithm HashAlgorithm { get; set; }

        public string Hash(byte[] value)
        {
            return ByteArrayToHexViaLookup32(HashAlgorithm.ComputeHash(value));
        }

        public string Hash(string value)
        {
            return ByteArrayToHexViaLookup32(HashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(value)));
        }

        private static uint[] CreateLookup32()
        {
            var result = new uint[256];
            for (var i = 0; i < 256; i++)
            {
                var s = i.ToString("X2");
                result[i] = s[0] + ((uint) s[1] << 16);
            }

            return result;
        }

        private static string ByteArrayToHexViaLookup32(byte[] bytes)
        {
            var lookup32 = _lookup32;
            var result = new char[bytes.Length * 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                var val = lookup32[bytes[i]];
                result[2 * i] = (char) val;
                result[2 * i + 1] = (char) (val >> 16);
            }

            return new string(result);
        }
    }
}