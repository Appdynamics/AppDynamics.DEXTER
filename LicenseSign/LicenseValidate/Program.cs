using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace LicenseValidate
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("path_to_license_file path_to_certificate'");
                Console.WriteLine("Not enough parameters passed");
                return;
            }

            string licenseFilePath = args[0];
            string certificateFilePath = args[1];
            //string certificateName = args[1];

            X509Certificate2 publicCert = new X509Certificate2(certificateFilePath);
            Console.WriteLine("Certificate: {0}", publicCert);

            //X509Store store = new X509Store(StoreLocation.CurrentUser);
            //X509Certificate2 privateCert = null;
            //store.Open(OpenFlags.ReadOnly);
            //X509Certificate2Collection cers = store.Certificates.Find(X509FindType.FindBySubjectName, "AppDynamics DEXTER Licensing", false);
            //if (cers.Count > 0)
            //{
            //    privateCert = cers[0];
            //};
            //store.Close();
            //Console.WriteLine("Certificate: {0}", privateCert);

            JObject licenseFile = JObject.Parse(File.ReadAllText(licenseFilePath));
            JObject licensedFeatures = (JObject)licenseFile["LicensedFeatures"];

            //string dataToSign = licensedFeatures.ToString(Formatting.Indented);
            string dataToSign = licensedFeatures.ToString(Formatting.None);
            Console.WriteLine("Signing {0}", dataToSign);

            var bytesSigned = Encoding.UTF8.GetBytes(dataToSign);
            Console.WriteLine("Data to sign array length: {0}", dataToSign.Length);

            string signatureString = licenseFile["Signature"].ToString();
            Console.WriteLine("Signature: {0}", signatureString);

            byte[] signatureBytes = Convert.FromBase64String(signatureString);
            Console.WriteLine("Signature array length: {0}", signatureBytes.Length);

            if (ValidateData(publicCert, signatureBytes, bytesSigned) == true)
            {
                Console.WriteLine("Signature validated");
            }
            else
            { 
                Console.WriteLine("Signature invalid");
            }

            //if (ValidateData(privateCert, signatureBytes, bytesSigned) == true)
            //{
            //    Console.WriteLine("Signature validated");
            //}
            //else
            //{
            //    Console.WriteLine("Signature invalid");
            //}
        }

        public static bool ValidateData(X509Certificate2 certificate, byte[] signature, byte[] dataToValidate)
        {
            var rsaPublicKey = certificate.GetRSAPublicKey();

            bool validationResult = rsaPublicKey.VerifyData(dataToValidate, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            return validationResult;
        }
    }
}
