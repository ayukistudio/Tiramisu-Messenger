using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;

namespace TiramisuClient
{
    public class ECCEncryption
    {
        private readonly EndToEndCrypto crypto;
        private readonly string keysDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "keys");
        private readonly byte[] aesKey = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 };

        public ECCEncryption()
        {
            if (!Directory.Exists(keysDir))
                Directory.CreateDirectory(keysDir);

            crypto = new EndToEndCrypto(
                Path.Combine(keysDir, "key.pub"),
                Path.Combine(keysDir, "key")
            );
        }

        public (string encryptedMessage, string encryptedKey) EncryptMessage(string message, string sharedKey, string password)
        {
            try
            {
                AsymmetricKeyParameter publicKey = EndToEndCrypto.ImportPublicKey(sharedKey);
                var user = new CryptoUser(0, publicKey);
                byte[] encryptedBytes = crypto.Encrypt(user, message);
                string encryptedMessage = Convert.ToBase64String(encryptedBytes);
                return (encryptedMessage, sharedKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Encryption error: {ex.Message}");
                throw;
            }
        }

        public string DecryptMessage(string encryptedMessage, string encryptedKey, string password)
        {
            try
            {
                AsymmetricKeyParameter publicKey = EndToEndCrypto.ImportPublicKey(encryptedKey);
                var user = new CryptoUser(0, publicKey);
                byte[] encryptedBytes = Convert.FromBase64String(encryptedMessage);
                return crypto.Decrypt(user, encryptedBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Decryption error: {ex.Message}");
                throw;
            }
        }

        public string GenerateSharedKey()
        {
            return crypto.ExportPublicKey();
        }

        public void SaveEncryptedKey(int dialogId, string sharedKey)
        {
            if (string.IsNullOrEmpty(sharedKey))
                throw new ArgumentNullException(nameof(sharedKey), "Shared key cannot be null or empty");

            string filePath = Path.Combine(keysDir, $"{dialogId}.pgk");
            byte[] keyBytes = Encoding.UTF8.GetBytes(sharedKey);

            using (var aes = Aes.Create())
            {
                aes.Key = aesKey;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV();

                using (var ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(keyBytes, 0, keyBytes.Length);
                        cs.FlushFinalBlock();
                    }
                    File.WriteAllBytes(filePath, ms.ToArray());
                }
            }
        }

        public string LoadEncryptedKey(int dialogId)
        {
            string filePath = Path.Combine(keysDir, $"{dialogId}.pgk");
            if (!File.Exists(filePath))
                return "";

            byte[] encryptedData = File.ReadAllBytes(filePath);

            using (var aes = Aes.Create())
            {
                aes.Key = aesKey;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                byte[] iv = new byte[16];
                Array.Copy(encryptedData, 0, iv, 0, 16);
                aes.IV = iv;

                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(encryptedData, 16, encryptedData.Length - 16);
                        cs.FlushFinalBlock();
                    }
                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
        }
    }
}