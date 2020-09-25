using Newtonsoft.Json.Linq;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Text;
using System.Security;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

namespace LicenseSign
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("path_to_license_file path_to_private_certificate password_of_private_certificate");
                Console.WriteLine("Not enough parameters passed");
                return;
            }

            string licenseFilePath = args[0];
            //string certificateFilePath = args[1];
            string certificatePath = args[1];
            string certificatePassword = args[2];

            JObject licenseFile = JObject.Parse(File.ReadAllText(licenseFilePath));
            JObject licensedFeatures = (JObject)licenseFile["LicensedFeatures"];

            //string dataToSign = licensedFeatures.ToString(Formatting.Indented);
            string dataToSign = licensedFeatures.ToString(Formatting.None);
            Console.WriteLine("Signing {0}", dataToSign);

            var bytesToSign = Encoding.UTF8.GetBytes(dataToSign);
            Console.WriteLine("Data to sign array length: {0}", dataToSign.Length);

            // Can't get this certificate to load the private key when it is loaded from local file system pfx
            //Console.WriteLine("Specify password for {0}", certificateFilePath);
            //String password = ReadPassword('*');
            //SecureString securePassword = new NetworkCredential("", password).SecurePassword;

            //X509Certificate2 privateCert = new X509Certificate2(certificateFilePath, securePassword);

            // Instead it needs to be present in the MyStore
            X509Certificate2 privateCert = new X509Certificate2(certificatePath, certificatePassword, X509KeyStorageFlags.Exportable);

            Console.WriteLine("Certificate: {0}", privateCert);

            var bytesSigned = SignData(privateCert, bytesToSign);
            Console.WriteLine("Signed data array length: {0}", bytesSigned.Length);

            bool validated = ValidateData(privateCert, bytesSigned, bytesToSign);
            Console.WriteLine("Is Validated: {0}", validated);

            string signedString = Convert.ToBase64String(bytesSigned);
            Console.WriteLine("Signature {0}", signedString);

            licenseFile["Signature"] = signedString;

            Console.WriteLine("Saving {0}", licenseFile);
            using (StreamWriter sw = File.CreateText(licenseFilePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Include;
                serializer.Formatting = Newtonsoft.Json.Formatting.Indented;
                serializer.Serialize(sw, licenseFile);
            }
        }

        public static byte[] SignData(X509Certificate2 certificate, byte[] dataToSign)
        {
            var rsaPrivateKey = certificate.GetRSAPrivateKey();
            var parameters = rsaPrivateKey.ExportParameters(true);
            Console.WriteLine("Certificate private key RSA parameters were successfully exported.");

            var privateKey = certificate.PrivateKey;
            Console.WriteLine("Certificate private key is accessible.");

            // generate new private key in correct format
            var cspParams = new CspParameters()
            {
                ProviderType = 24,
                ProviderName = "Microsoft Enhanced RSA and AES Cryptographic Provider"
            };
            var rsaCryptoServiceProvider = new RSACryptoServiceProvider(cspParams);
            rsaCryptoServiceProvider.ImportParameters(parameters);

            // sign data
            var signedBytes = rsaCryptoServiceProvider.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return signedBytes;
        }

        public static bool ValidateData(X509Certificate2 certificate, byte[] signature, byte[] dataToValidate)
        {
            var rsaPublicKey = certificate.GetRSAPublicKey();

            bool validationResult = rsaPublicKey.VerifyData(dataToValidate, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            return validationResult;
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
