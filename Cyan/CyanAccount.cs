using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Cyan
{
    internal class CyanAccount
    {
        private readonly object _hasherSync = new object();
        private readonly HMACSHA256 _signatureHasher;

        public CyanAccount(string accountName, string accountSecret)
        {
            Name = accountName;
            AccountSecret = accountSecret;

            var key = Convert.FromBase64String(AccountSecret);
            _signatureHasher = new HMACSHA256(key);
        }

        public string Name { get; private set; }
        public string AccountSecret { get; private set; }

        public void Sign(HttpWebRequest request)
        {
            var method = request.Method;
            var path = request.RequestUri.AbsolutePath;
            var date = request.Headers["x-ms-date"];
            var contentMd5 = request.Headers["Content-MD5"] ?? "";
            var contentType = request.ContentType ?? "";

            var signature = GenerateSignature(method, path, date, contentMd5, contentType);

            var authorizationHeader = string.Format("SharedKey {0}:{1}", Name, signature);

            request.Headers.Add("Authorization", authorizationHeader);
        }

        private string GenerateSignature(string method, string resource, string xMsDate, string contentMd5 = "",
            string contentType = "")
        {
            var canonicalizedResource = string.Format("/{0}{1}", Name, resource);

            // format: method\ncontentMD5\ncontentType\nxMsDate\ncanonicalizedResource
            var signature = string.Join("\n", method, contentMd5, contentType, xMsDate, canonicalizedResource);

            var signatureBytes = Encoding.UTF8.GetBytes(signature);

            byte[] hash;
            lock (_hasherSync)
                hash = _signatureHasher.ComputeHash(signatureBytes);

            return Convert.ToBase64String(hash);
        }
    }
}