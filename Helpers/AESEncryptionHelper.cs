using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AppDynamics.Dexter
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.aes?view=netframework-4.7
    /// </summary>
    public class AESEncryptionHelper
    {
        /// <summary>
        /// This isn't particularly secure, but considering that the password is sent in basic auth it is good enough
        /// </summary>
        /// <returns></returns>
        private static byte[] getKey()
        {
            string val = "66CA267947CB46FDBA01AE938A564BEA";
            return Encoding.UTF8.GetBytes(val);
        }
        private static byte[] getVector()
        {
            string val = String.Format("3300EF7DE0DC4BDA");
            char[] array = val.ToCharArray();
            Array.Reverse(array);
            val = new String(array);
            return Encoding.UTF8.GetBytes(val);
        }

        public static string Encrypt(string plainText)
        {
            return Convert.ToBase64String(AESEncryptionHelper.EncryptStringToBytes_Aes(plainText, getKey(), getVector()));
        }

        public static string Decrypt(string cipherText)
        {
            try
            {
                return DecryptStringFromBytes_Aes(Convert.FromBase64String(cipherText), getKey(), getVector());
            }
            catch (CryptographicException ex)
            {
                if (ex.Message == "The input data is not a complete block.")
                {
                    // This is plaintext
                    return cipherText;
                }
                else
                {
                    return "";
                }
            }
        }

        public static bool IsCipherText(string cipherText)
        {
            try
            {
                DecryptStringFromBytes_Aes(Convert.FromBase64String(cipherText), getKey(), getVector());
                return true;
            }
            catch (CryptographicException ex)
            {
                if (ex.Message == "The input data is not a complete block.")
                {
                    // This is plaintext
                    return false;
                }
                else
                {
                    return false;
                }
            }
        }

        public static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted;
            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }


            // Return the encrypted bytes from the memory stream.
            return encrypted;
        }

        public static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting  stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }

            return plaintext;
        }
    }
}
