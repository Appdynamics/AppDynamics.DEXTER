using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace IssueCertificate
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Not enough parameters passed");
                return;
            }

            string certificatePrivateAndPublicFilePath = args[0];
            string certificatePublicKeyFilePath = args[1];

            var oldCertificate = CreateCertificate();
            var oldCertificateBytes = oldCertificate.Export(X509ContentType.Pfx, "");
            var newCertificate = new X509Certificate2(oldCertificateBytes, "",
                X509KeyStorageFlags.Exportable |
                X509KeyStorageFlags.MachineKeySet |
                X509KeyStorageFlags.PersistKeySet);

            LogCertificate(oldCertificate, "old certificate"); // this fails
            LogCertificate(newCertificate, "new certificate"); // works only on Win10

            SaveCertificatePublicAndPrivate(newCertificate, certificatePrivateAndPublicFilePath);
            SaveCertificatePublicOnly(newCertificate, certificatePublicKeyFilePath);
        }

        private static X509Certificate2 CreateCertificate()
        {
            var keyParams = new CngKeyCreationParameters();
            keyParams.KeyUsage = CngKeyUsages.Signing;
            keyParams.Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider;
            keyParams.ExportPolicy = CngExportPolicies.AllowExport; // here I don't have AllowPlaintextExport
            // keyParams.ExportPolicy = CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport; // here I don't have AllowPlaintextExport
            keyParams.Parameters.Add(new CngProperty("Length", BitConverter.GetBytes(2048), CngPropertyOptions.None));
            var cngKey = CngKey.Create(CngAlgorithm.Rsa, Guid.NewGuid().ToString(), keyParams);
            var rsaKey = new RSACng(cngKey);
            var req = new CertificateRequest("cn=AppDynamics DEXTER Licensing", rsaKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pss); // requires .net 4.7.2
            var cert = req.CreateSelfSigned(DateTimeOffset.Now, new DateTime(2030, 12, 31, 23, 59, 59, DateTimeKind.Utc));
            return cert;
        }

        private static void LogCertificate(X509Certificate2 certificate, string name)
        {
            Console.WriteLine("----- Testing " + name + " ------");
            Console.WriteLine(certificate);

            try
            {
                var rsaPrivateKey = certificate.GetRSAPrivateKey();
                var parameters = rsaPrivateKey.ExportParameters(true);
                Console.WriteLine("Certificate private key RSA parameters were successfully exported.");

                var privateKey = certificate.PrivateKey;
                Console.WriteLine("Certificate private key is accessible.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void SaveCertificatePublicAndPrivate(X509Certificate2 certificate, string certificateFilePath)
        {
            try
            {
                Console.WriteLine("Specify password for {0}", certificateFilePath);
                String password = ReadPassword('*');
                SecureString securePassword = new NetworkCredential("", password).SecurePassword;

                byte[] certBytes = certificate.Export(X509ContentType.Pfx, securePassword);

                File.WriteAllBytes(certificateFilePath, certBytes);

                Console.WriteLine("Wrote {0} bytes to {1}", certBytes.Length, certificateFilePath);
            }
            catch (Exception e)
            { 
                Console.WriteLine(e.ToString());
            }
        }

        private static void SaveCertificatePublicOnly(X509Certificate2 certificate, string certificateFilePath)
        {
            try
            {
                byte[] certBytes = certificate.Export(X509ContentType.Cert);

                File.WriteAllBytes(certificateFilePath, certBytes);

                Console.WriteLine("Wrote {0} bytes to {1}", certBytes.Length, certificateFilePath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static string ReadPassword(char mask)
        {
            const int ENTER = 13, BACKSP = 8, CTRLBACKSP = 127;
            int[] FILTERED = { 0, 27, 9, 10 /*, 32 space, if you care */ }; // const

            var pass = new Stack<char>();
            char chr = (char)0;

            while ((chr = System.Console.ReadKey(true).KeyChar) != ENTER)
            {
                if (chr == BACKSP)
                {
                    if (pass.Count > 0)
                    {
                        System.Console.Write("\b \b");
                        pass.Pop();
                    }
                }
                else if (chr == CTRLBACKSP)
                {
                    while (pass.Count > 0)
                    {
                        System.Console.Write("\b \b");
                        pass.Pop();
                    }
                }
                else if (FILTERED.Count(x => chr == x) > 0) { }
                else
                {
                    pass.Push((char)chr);
                    System.Console.Write(mask);
                }
            }

            return new string(pass.Reverse().ToArray());
        }
    }
}