using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using System;
using System.Text;

namespace Medimind
{
    public class Crypting : MonoBehaviour
    {
        public enum Mode
        {
            /// <summary>
            /// 암호화
            /// </summary>
            Encrypt,
            /// <summary>
            /// 복호화
            /// </summary>
            Decrypt
        }

        public byte[] EncryptData { get { return encryptData; } }
        /// <summary>
        /// decrypt Test를 위해 남겨 둠
        /// </summary>
        protected byte[] encryptData;

        /// <summary>
        /// 암호화
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public byte[] Encrypt(byte[] data, string clipName)
        {
            byte[] cryptedData = Crypt(data, Mode.Encrypt);

            encryptData = cryptedData;

            SaveDat(DataManager.TestDataPath, clipName, cryptedData);

            return cryptedData;
        }

        /// <summary>
        /// 암호화 데이터 저장(.dat)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="clipName"></param>
        /// <param name="encryptData"></param>
        public void SaveDat(string path, string clipName, byte[] encryptData)
        {
            File.WriteAllBytes($"{path}/{clipName}.dat", encryptData);
        }
        /// <summary>
        /// 복호화
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public byte[] Decrypt(byte[] data)
        {
            return Crypt(data, Mode.Decrypt);
        }
        /// <summary>
        /// 데이터 암복호화
        /// </summary>
        /// <param name="data">변경할 데이터</param>
        /// <param name="mode">변경할 모드</param>
        /// <returns></returns>
        public byte[] Crypt(byte[] data, Mode mode)
        {
            byte[] _key = Encoding.UTF8.GetBytes("abcd1Medimind1CheeUForestN1efghi");
            byte[] _iv  = Encoding.UTF8.GetBytes("J29iRCr0lw8zqYoY");

            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                aes.Key = _key;
                aes.IV = _iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform cryptor;

                switch (mode)
                {
                    case Mode.Encrypt: cryptor = aes.CreateEncryptor(); break;
                    case Mode.Decrypt: cryptor = aes.CreateDecryptor(); break;
                    default: goto case Mode.Encrypt;
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, cryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(data, 0, data.Length);
                        cs.FlushFinalBlock();

                        return ms.ToArray();
                    }
                }
            }
        }
    }
}