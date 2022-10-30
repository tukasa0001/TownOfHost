using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TownOfHost
{
    class HashAuth
    {
        public readonly string HashValue;

        private readonly string salt;
        private HashAlgorithm algorithm;
        public HashAuth(string hashValue)
        {
            HashValue = hashValue;
            salt = null;
            algorithm = SHA256.Create();
        }
        public HashAuth(string hashValue, string salt)
        {
            HashValue = hashValue;
            this.salt = salt;
            algorithm = SHA256.Create();
        }
    }
}