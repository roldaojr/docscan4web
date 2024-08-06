using System;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

namespace DocScanForWeb
{
    internal class CertUtils
    {
        internal static void InstallCertificate()
        {
            var certificate = CreateSelfSignedCertificate("docscan4web.localhost.direct");
            string thumbprint = certificate.Thumbprint;
            InstallCertificate(certificate, true);
        }
        private static X509Certificate2 CreateSelfSignedCertificate(string subjectName)
        {
            using (RSA rsa = RSA.Create(2048))
            {
                var certificateRequest = new CertificateRequest(
                    subjectName,
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                // Set certificate validity period
                DateTimeOffset notBefore = DateTimeOffset.UtcNow;
                DateTimeOffset notAfter = notBefore.AddYears(5);

                // Create the self-signed certificate
                var certificate = certificateRequest.CreateSelfSigned(notBefore, notAfter);

                // Export the certificate including the private key
                return new X509Certificate2(certificate.Export(X509ContentType.Pfx, "password"), "password", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            }
        }

        private static void checStore(bool user = false)
        {
            var location = (user) ? StoreLocation.CurrentUser : StoreLocation.LocalMachine;
            using (var store = new X509Store(StoreName.Root, location))
            {
                foreach (var cert in store.Certificates)
                {

                }
            }
        }

        private static void InstallCertificate(X509Certificate2 certificate, bool user = false)
        {
            var location = (user) ? StoreLocation.CurrentUser : StoreLocation.LocalMachine;
            using (var store = new X509Store(StoreName.Root, location))
            {
                store.Open(OpenFlags.ReadWrite);
                store.Add(certificate);
                store.Close();
            }
        }
    }
}
