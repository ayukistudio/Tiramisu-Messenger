using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace TiramisuClient
{
    public class EndToEndCrypto
    {
        private string _privateKeyPath;
        private string _publicKeyPath;
        private AsymmetricCipherKeyPair _keyPair;
        private readonly byte[] _aesKey = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 };

        public EndToEndCrypto(string publicKeyPath = "./keys/key.pub.pgk", string privateKeyPath = "./keys/key.pgk")
        {
            _publicKeyPath = publicKeyPath;
            _privateKeyPath = privateKeyPath;

            if (File.Exists(_privateKeyPath))
            {
                _keyPair = ReadPrivateKeyPair(_privateKeyPath);
                if (!File.Exists(_publicKeyPath))
                {
                    SaveKeyPair(_publicKeyPath, _keyPair.Public);
                }
            }
            else
            {
                if (File.Exists(_publicKeyPath)) File.Delete(_publicKeyPath);
                if (File.Exists(_privateKeyPath)) File.Delete(_privateKeyPath);

                _keyPair = GenerateECKeyPair();
                SaveKeyPair(_publicKeyPath, _keyPair.Public);
                SaveKeyPair(_privateKeyPath, _keyPair.Private);
            }
        }

        public byte[] Encrypt(CryptoUser user, string message)
        {
            var sharedSecret = DeriveSharedSecret(_keyPair.Private, user.PublicKey);
            return Encrypt(Srt2Bytes(message), sharedSecret);
        }

        public string Decrypt(CryptoUser user, byte[] bytes)
        {
            var sharedSecret = DeriveSharedSecret(_keyPair.Private, user.PublicKey);
            return Bytes2Str(Decrypt(bytes, sharedSecret));
        }

        public AsymmetricKeyParameter GetPublicKey()
        {
            return _keyPair.Public;
        }

        public string ExportPublicKey()
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var writer = new StreamWriter(memoryStream))
                {
                    var pemWriter = new PemWriter(writer);
                    pemWriter.WriteObject(_keyPair.Public);
                    writer.Flush();
                }
                byte[] bytes = memoryStream.ToArray();
                return Bytes2Str(bytes);
            }
        }

        public override string ToString()
        {
            return ExportPublicKey();
        }

        public static byte[] Srt2Bytes(string text)
        {
            return Encoding.UTF8.GetBytes(text);
        }

        public static string Bytes2Str(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        public static string DisplayBytes(byte[] bytes)
        {
            return string.Join(" ", bytes.Select(b => b.ToString("X2")));
        }

        public static byte[] GetRange(byte[] bytes, int start, int end)
        {
            return bytes.Skip(start).Take(end - start).ToArray();
        }

        public static AsymmetricKeyParameter ImportPublicKey(string publicKey)
        {
            using (MemoryStream memoryStream = new MemoryStream(Srt2Bytes(publicKey)))
            {
                using (var reader = new StreamReader(memoryStream))
                {
                    var pemReader = new PemReader(reader);
                    return (AsymmetricKeyParameter)pemReader.ReadObject();
                }
            }
        }

        private void SaveKeyPair(string filePath, AsymmetricKeyParameter key)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    using (var writer = new StreamWriter(memoryStream))
                    {
                        var pemWriter = new PemWriter(writer);
                        pemWriter.WriteObject(key);
                        writer.Flush();
                    }
                    byte[] keyBytes = memoryStream.ToArray();
                    Console.WriteLine($"Saving key to {filePath}, key length: {keyBytes.Length} bytes");

                    using (var aes = Aes.Create())
                    {
                        aes.Key = _aesKey;
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;
                        aes.GenerateIV();
                        byte[] iv = aes.IV;

                        Console.WriteLine($"AES key: {BitConverter.ToString(_aesKey).Replace("-", "")}, IV: {BitConverter.ToString(iv).Replace("-", "")}");

                        using (var ms = new MemoryStream())
                        {
                            ms.Write(iv, 0, iv.Length);
                            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                            {
                                cs.Write(keyBytes, 0, keyBytes.Length);
                                cs.FlushFinalBlock();
                            }
                            byte[] encryptedData = ms.ToArray();
                            File.WriteAllBytes(filePath, encryptedData);
                            Console.WriteLine($"Saved encrypted key to {filePath}, length: {encryptedData.Length} bytes");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SaveKeyPair error for {filePath}: {ex.Message}");
                throw;
            }
        }

        private AsymmetricKeyParameter ReadPublicKeyPair(string filePath)
        {
            try
            {
                byte[] encryptedData = File.ReadAllBytes(filePath);
                Console.WriteLine($"Loaded encrypted key file from {filePath}, length: {encryptedData.Length} bytes");

                using (var aes = Aes.Create())
                {
                    aes.Key = _aesKey;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    byte[] iv = new byte[16];
                    Array.Copy(encryptedData, 0, iv, 0, 16);
                    aes.IV = iv;

                    Console.WriteLine($"AES key: {BitConverter.ToString(_aesKey).Replace("-", "")}, IV: {BitConverter.ToString(iv).Replace("-", "")}");

                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(encryptedData, 16, encryptedData.Length - 16);
                            cs.FlushFinalBlock();
                        }
                        byte[] decryptedData = ms.ToArray();
                        using (var memoryStream = new MemoryStream(decryptedData))
                        {
                            using (var reader = new StreamReader(memoryStream))
                            {
                                var pemReader = new PemReader(reader);
                                return (AsymmetricKeyParameter)pemReader.ReadObject();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReadPublicKeyPair error for {filePath}: {ex.Message}");
                throw;
            }
        }

        private AsymmetricCipherKeyPair ReadPrivateKeyPair(string filePath)
        {
            try
            {
                byte[] encryptedData = File.ReadAllBytes(filePath);
                Console.WriteLine($"Loaded encrypted key file from {filePath}, length: {encryptedData.Length} bytes");

                using (var aes = Aes.Create())
                {
                    aes.Key = _aesKey;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    byte[] iv = new byte[16];
                    Array.Copy(encryptedData, 0, iv, 0, 16);
                    aes.IV = iv;

                    Console.WriteLine($"AES key: {BitConverter.ToString(_aesKey).Replace("-", "")}, IV: {BitConverter.ToString(iv).Replace("-", "")}");

                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(encryptedData, 16, encryptedData.Length - 16);
                            cs.FlushFinalBlock();
                        }
                        byte[] decryptedData = ms.ToArray();
                        using (var memoryStream = new MemoryStream(decryptedData))
                        {
                            using (var reader = new StreamReader(memoryStream))
                            {
                                var pemReader = new PemReader(reader);
                                return (AsymmetricCipherKeyPair)pemReader.ReadObject();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReadPrivateKeyPair error for {filePath}: {ex.Message}");
                throw;
            }
        }

        private static AsymmetricCipherKeyPair GenerateECKeyPair()
        {
            var gen = new ECKeyPairGenerator();
            var ecParams = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp256r1");
            var domainParams = new ECDomainParameters(ecParams.Curve, ecParams.G, ecParams.N, ecParams.H);
            gen.Init(new ECKeyGenerationParameters(domainParams, new SecureRandom()));
            return gen.GenerateKeyPair();
        }

        private byte[] DeriveSharedSecret(AsymmetricKeyParameter privateKey, AsymmetricKeyParameter publicKey)
        {
            var agreement = new ECDHBasicAgreement();
            agreement.Init(privateKey);
            var sharedSecret = agreement.CalculateAgreement(publicKey);
            return sharedSecret.ToByteArrayUnsigned();
        }

        private static byte[] Encrypt(byte[] bytes, byte[] key)
        {
            var cipher = new PaddedBufferedBlockCipher(new CbcBlockCipher(new AesEngine()));
            cipher.Init(true, new ParametersWithIV(new KeyParameter(GetRange(key, 0, 16)), new byte[16]));
            var output = new byte[cipher.GetOutputSize(bytes.Length)];
            var len = cipher.ProcessBytes(bytes, 0, bytes.Length, output, 0);
            cipher.DoFinal(output, len);
            return output;
        }

        private static byte[] Decrypt(byte[] cipherText, byte[] key)
        {
            var cipher = new PaddedBufferedBlockCipher(new CbcBlockCipher(new AesEngine()));
            cipher.Init(false, new ParametersWithIV(new KeyParameter(GetRange(key, 0, 16)), new byte[16]));
            var output = new byte[cipher.GetOutputSize(cipherText.Length)];
            var len = cipher.ProcessBytes(cipherText, 0, cipherText.Length, output, 0);
            cipher.DoFinal(output, len);
            return output;
        }
    }

    public struct CryptoUser
    {
        public long Id { get; private set; }
        public AsymmetricKeyParameter PublicKey { get; private set; }

        public CryptoUser(long id, AsymmetricKeyParameter publicKey)
        {
            Id = id;
            PublicKey = publicKey;
        }

        public CryptoUser(long id, string publicKey)
        {
            Id = id;
            PublicKey = EndToEndCrypto.ImportPublicKey(publicKey);
        }
    }
}