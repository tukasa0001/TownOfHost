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
        public HashAuth(string hashValue, string salt = null, HashAlgorithm algorithm = null)
        {
            HashValue = hashValue;
            this.salt = salt;
            this.algorithm = algorithm ?? SHA256.Create(); // 引数のalgorithmがnullの場合のみ新しく作る
        }

        public bool CheckString(string value)
        {
            var hash = CalculateHash(value);
            return hash == value;
        }
        private string CalculateHash(string source)
            => HashAuth.CalculateHash(source, salt, algorithm);

        public static string CalculateHash(string source, string salt = null, HashAlgorithm algorithm = null)
        {
            // 0.algorithmの初期化
            algorithm ??= SHA256.Create();

            // 1.saltの適用
            if (salt != null) source += salt;

            // 2.sourceをbyte配列に変換
            var sourceBytes = Encoding.UTF8.GetBytes(source);

            // 3.sourceBytesをハッシュ化
            var hashBytes = algorithm.ComputeHash(sourceBytes);

            // 4.hashBytesを文字列化
            var sb = new StringBuilder();
            foreach (var b in hashBytes)
                sb.Append(b.ToString("x2")); //1byteずつ2桁の16進法表記に変換する

            return sb.ToString();
        }
    }
}