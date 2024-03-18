using System.Security.Cryptography;
using System.Text;

namespace EasyPayApi.Services
{
    public class RSAEncryptionService
    {
        private RSACryptoServiceProvider rsa;

        private readonly string publicKey = "";

        private readonly string privateKey = "";

        public RSAEncryptionService()
        {
            rsa = new RSACryptoServiceProvider();
        }

        /// <summary>
        /// Decrypt an ciphertext string.
        /// </summary>
        /// <param name="ciphertext"></param>
        /// <returns></returns>
        public string Decrypt(string ciphertext)
        {
            // Import private key
            rsa.FromXmlString(privateKey);

            // Decrypt data
            byte[] ciphertextBytes = Convert.FromBase64String(ciphertext);
            byte[] decryptedBytes = rsa.Decrypt(ciphertextBytes, false);
            return Encoding.UTF8.GetString(decryptedBytes);
        }

        /// <summary>
        /// Encrypt a plaintext string.
        /// </summary>
        /// <param name="plaintext"></param>
        /// <returns></returns>
        public string Encrypt(string plaintext)
        {
            // Import public key
            rsa.FromXmlString(publicKey);

            // Encrypt data
            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            byte[] encryptedBytes = rsa.Encrypt(plaintextBytes, false);
            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// Generate private/public keys for encryption and decryption.
        /// </summary>
        public void GenerateKeys()
        {
            rsa = new RSACryptoServiceProvider(2048);
        }

        /// <summary>
        /// Supplemental for generating a new private key.
        /// </summary>
        /// <returns></returns>
        public string GetPrivateKey()
        {
            // Export private key
            return rsa.ToXmlString(true);
        }

        /// <summary>
        /// Supplemental for generating a new public key.
        /// </summary>
        /// <returns></returns>
        public string GetPublicKey()
        {
            // Export public key
            return rsa.ToXmlString(true);
        }

    }
}
