using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cyan.Tests
{
    [TestClass]
    public class FromConnectionString
    {
        static string randomAccountKey = Convert.ToBase64String(Encoding.UTF8
            .GetBytes(Guid.NewGuid().ToString()));

        [TestMethod, TestCategory("Connection strings")]
        public void DevelopmentStorage()
        {
            var client = CyanClient.FromConnectionString("UseDevelopmentStorage=true");
            Assert.IsTrue(client.IsDevelopmentStorage);
        }

        [TestMethod, TestCategory("Connection strings")]
        [ExpectedException(typeof(NotSupportedException))]
        public void DevelopmentStorageProxy()
        {
            var client = CyanClient.FromConnectionString("UseDevelopmentStorage=true;DevelopmentStorageProxyUri=http://myProxyUri");
        }

        [TestMethod, TestCategory("Connection strings")]
        public void AccountNameAndKey()
        {
            var client = CyanClient.FromConnectionString("AccountName=accountName;AccountKey=" + randomAccountKey);

            Assert.AreEqual("accountName", client.AccountName);
        }

        [TestMethod, TestCategory("Connection strings")]
        [ExpectedException(typeof(ArgumentException))]
        public void InvalidNoAccountName()
        {
            var client = CyanClient.FromConnectionString("AccountKey=" + randomAccountKey);
        }

        [TestMethod, TestCategory("Connection strings")]
        [ExpectedException(typeof(ArgumentException))]
        public void InvalidNoAccountKey()
        {
            var client = CyanClient.FromConnectionString("AccountName=accountName");
        }

        [TestMethod, TestCategory("Connection strings")]
        [ExpectedException(typeof(ArgumentException))]
        public void InvalidDevelopmentAndAccountName()
        {
            var client = CyanClient.FromConnectionString("UseDevelopmentStorage=true;AccountName=accountName");
        }

        [TestMethod, TestCategory("Connection strings")]
        public void Protocol()
        {
            Assert.IsTrue(CyanClient
                .FromConnectionString("DefaultEndpointsProtocol=https;AccountName=accountName;AccountKey=" + randomAccountKey)
                .UseSsl);

            Assert.IsFalse(CyanClient
                .FromConnectionString("DefaultEndpointsProtocol=http;AccountName=accountName;AccountKey=" + randomAccountKey)
                .UseSsl);
        }

        [TestMethod, TestCategory("Connection strings")]
        [ExpectedException(typeof(NotSupportedException))]
        public void InvalidProtocol()
        {
            CyanClient.FromConnectionString("DefaultEndpointsProtocol=something;AccountName=accountName;AccountKey=" + randomAccountKey);
        }

        [TestMethod, TestCategory("Connection strings")]
        [ExpectedException(typeof(NotSupportedException))]
        public void CustomEndpoint()
        {
            CyanClient.FromConnectionString("TableEndpoint=endpoint;AccountName=accountName;AccountKey=" + randomAccountKey);
        }
    }
}
