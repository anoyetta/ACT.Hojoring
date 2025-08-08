using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FFXIV.Framework.Common
{
    public class Crypter
    {
        /// <summary>
        /// 初期Salt
        /// </summary>
        private const string DefaultSalt = "L+DCZF4pmD8SVAv.$m5sYv/3$,x$wXAW";

        /// <summary>
        /// 初期パスワード
        /// </summary>
        private const string DefaultPassword = "ym%#EPcY5g(WN4CRTE|mv5c/rvqVXA7m";

        /// <summary>
        /// 文字列を暗号化する
        /// </summary>
        /// <param name="sourceString">暗号化する文字列</param>
        /// <param name="password">暗号化に使用するパスワード</param>
        /// <returns>暗号化された文字列</returns>
        public static string EncryptString(
            string sourceString,
            string password = DefaultPassword)
        {
            var encryptedString = string.Empty;

            using (var rijndael = new RijndaelManaged())
            {
                byte[] key, iv;
                Crypter.GenerateKeyFromPassword(password, rijndael.KeySize, out key, rijndael.BlockSize, out iv);
                rijndael.Key = key;
                rijndael.IV = iv;

                var strBytes = Encoding.UTF8.GetBytes(sourceString);

                using (var encryptor = rijndael.CreateEncryptor())
                {
                    var encBytes = encryptor.TransformFinalBlock(strBytes, 0, strBytes.Length);
                    encryptedString = Convert.ToBase64String(encBytes);
                }
            }

            return encryptedString;
        }

        /// <summary>
        /// 暗号化された文字列を復号化する
        /// </summary>
        /// <param name="sourceString">暗号化された文字列</param>
        /// <param name="password">暗号化に使用したパスワード</param>
        /// <returns>復号化された文字列</returns>
        public static string DecryptString(
            string sourceString,
            string password = DefaultPassword)
        {
            var decryptedString = string.Empty;

            using (var rijndael = new RijndaelManaged())
            {
                byte[] key, iv;
                Crypter.GenerateKeyFromPassword(password, rijndael.KeySize, out key, rijndael.BlockSize, out iv);
                rijndael.Key = key;
                rijndael.IV = iv;

                var strBytes = Convert.FromBase64String(sourceString);

                using (var decryptor = rijndael.CreateDecryptor())
                {
                    var decBytes = decryptor.TransformFinalBlock(strBytes, 0, strBytes.Length);
                    decryptedString = Encoding.UTF8.GetString(decBytes);
                }
            }

            return decryptedString;
        }

        public static bool IsMatchHash(
            string file1,
            string file2)
        {
            if (!File.Exists(file1) ||
                !File.Exists(file2))
            {
                return false;
            }

            var r = false;

            using (var md5 = MD5.Create())
            {
                using (var fs1 = new FileStream(file1, FileMode.Open, FileAccess.Read))
                using (var fs2 = new FileStream(file2, FileMode.Open, FileAccess.Read))
                {
                    var hash1 = md5.ComputeHash(fs1);
                    var hash2 = md5.ComputeHash(fs2);

                    r = string.Equals(
                        BitConverter.ToString(hash1),
                        BitConverter.ToString(hash2),
                        StringComparison.OrdinalIgnoreCase);
                }
            }

            return r;
        }

        /// <summary>
        /// パスワードから共有キーと初期化ベクタを生成する
        /// </summary>
        /// <param name="password">基になるパスワード</param>
        /// <param name="keySize">共有キーのサイズ（ビット）</param>
        /// <param name="key">作成された共有キー</param>
        /// <param name="blockSize">初期化ベクタのサイズ（ビット）</param>
        /// <param name="iv">作成された初期化ベクタ</param>
        private static void GenerateKeyFromPassword(
            string password,
            int keySize,
            out byte[] key,
            int blockSize,
            out byte[] iv)
        {
            // パスワードから共有キーと初期化ベクタを作成する
            // saltを決める
            var salt = Encoding.UTF8.GetBytes(DefaultSalt);

            // Rfc2898DeriveBytesオブジェクトを作成する
            var deriveBytes = new Rfc2898DeriveBytes(password, salt);

            // 反復処理回数を指定する
            deriveBytes.IterationCount = 1000;

            // 共有キーと初期化ベクタを生成する
            key = deriveBytes.GetBytes(keySize / 8);
            iv = deriveBytes.GetBytes(blockSize / 8);
        }
    }
}
