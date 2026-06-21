using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace TrumpTile.GameMain.Data
{
    public static class DataEncryptor
    {
        private const string GUID_KEY ="UserId";
        private static readonly string mBaseSeed = "acidsoft20260621";
        private static string mCachedGUID = null;
        private static byte[] mCachedKey = null;
        private static byte[] mCachedHmacKey = null;

        public static string Encrypt(string text)
        {
            if(text == null)
            {
                Debug.LogWarning($"[DataEncryptor] 암호화 실패: 저장할 데이터가 비어있습니다.");
                return null;
            }
            using(Aes aes = Aes.Create())
            {
                aes.Key = GetKey();
                aes.GenerateIV();
                byte[] iv = aes.IV;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, iv);

                byte[] cipherBytes;

                using (MemoryStream ms = new MemoryStream())
                {
                    using(CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using(StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(text);
                        }
                    }
                    cipherBytes = ms.ToArray();
                }
                byte[] ivAndCipher = new byte[iv.Length + cipherBytes.Length];
                Array.Copy(iv, 0, ivAndCipher, 0, iv.Length);
                Array.Copy(cipherBytes, 0, ivAndCipher, iv.Length, cipherBytes.Length);

                byte[] hmac = ComputeHmac(ivAndCipher);

                byte[] result = new byte[hmac.Length + ivAndCipher.Length];
                Array.Copy(hmac, 0, result, 0, hmac.Length);
                Array.Copy(ivAndCipher, 0, result, hmac.Length, ivAndCipher.Length);

                return Convert.ToBase64String(result);
            }
        }
        public static bool TryDecrypt(string text, out string result)
        {
            try
            {
                result = Decrypt(text);
                return true;
            }
            catch(Exception e)
            {
                Debug.LogWarning($"[DataEncryptor] 복호화 실패: {e.Message}");
                result = null;
                return false;
            }
        }
        private static string Decrypt(string text)
        {
            byte[] fullBytes = Convert.FromBase64String(text);

            byte[] storedHmac = new byte[32];
            Array.Copy(fullBytes, 0, storedHmac, 0, 32);

            byte[] ivAndCipher = new byte[fullBytes.Length - 32];
            Array.Copy(fullBytes, 32, ivAndCipher, 0, ivAndCipher.Length);

            byte[] computedHmac = ComputeHmac(ivAndCipher);
            if(!CompareBytes(storedHmac, computedHmac))
            {
                throw new CryptographicException("데이터 무결성 검증 실패 (변조 의심)");
            }

            byte[] iv = new byte[16];
            Array.Copy(ivAndCipher, 0, iv, 0, 16);

            using (Aes aes = Aes.Create())
            {
                aes.Key = GetKey();
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, iv);

                using (MemoryStream ms = new MemoryStream(ivAndCipher, 16, ivAndCipher.Length - 16))
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(cs))
                        {
                            return sr.ReadToEnd();
                        }   
                    }
                }
            }
        }
        private static string GetGUID()
        {
            if(mCachedGUID != null)
            {
                return mCachedGUID;
            }

            string id = PlayerPrefs.GetString(GUID_KEY, "");
            if(string.IsNullOrEmpty(id))
            {
                id = System.Guid.NewGuid().ToString();
                PlayerPrefs.SetString(GUID_KEY, id);
                PlayerPrefs.Save();
            }

            mCachedGUID = id;
            return mCachedGUID;
        }
        private static byte[] GetKey()
        {
            if(mCachedKey != null)
            {
                return mCachedKey;
            }
            string combined = mBaseSeed + GetGUID();
            using(SHA256 sha = SHA256.Create())
            {
                mCachedKey = sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
                return mCachedKey;
            }
        }
        private static byte[] GetHmacKey()
        {
            if(mCachedHmacKey != null)
            {
                return mCachedHmacKey;
            }
            string combined = mBaseSeed + "_hmac_" + GetGUID();
            using(SHA256 sha = SHA256.Create())
            {
                mCachedHmacKey = sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
                return mCachedHmacKey;
            }
        }
        private static byte[] ComputeHmac(byte[] data)
        {
            using(HMACSHA256 hmac = new HMACSHA256(GetHmacKey()))
            {
                return hmac.ComputeHash(data);
            }
        }
        private static bool CompareBytes(byte[] a, byte[] b)
        {
            if(a.Length != b.Length) return false;
            int diff = 0;
            for(int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }
    }   
}
