using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class EncryptionUtility
{
    // secret key as a string
    private static readonly string EncryptionKey = "Makka pakka akka wakka mikka makka moo!";

    // Generate a 256-bit key from the string using SHA256
    private static byte[] GetKey()
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(EncryptionKey));
        }
    }

    // Encrypt a string using AES encryption with random IV
    public static string Encrypt(string plainText)
    {
        byte[] key = GetKey();

        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = key;
            aesAlg.GenerateIV(); // Create a random IV
            byte[] iv = aesAlg.IV;

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                // Write IV first
                msEncrypt.Write(iv, 0, iv.Length);

                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }

                return Convert.ToBase64String(msEncrypt.ToArray());
            }
        }
    }

    // Decrypt an AES encrypted string
    public static string Decrypt(string cipherText)
    {
        byte[] key = GetKey();
        byte[] fullCipher = Convert.FromBase64String(cipherText);

        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = key;

            // Extract IV from the first 16 bytes
            byte[] iv = new byte[16];
            Array.Copy(fullCipher, 0, iv, 0, iv.Length);
            aesAlg.IV = iv;

            // Extract actual encrypted bytes
            int cipherStart = iv.Length;
            int cipherLength = fullCipher.Length - cipherStart;
            byte[] cipherBytes = new byte[cipherLength];
            Array.Copy(fullCipher, cipherStart, cipherBytes, 0, cipherLength);

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msDecrypt = new MemoryStream(cipherBytes))
            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
            {
                return srDecrypt.ReadToEnd();
            }
        }
    }
}