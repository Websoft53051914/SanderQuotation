using System;
using System.Security.Cryptography;
using System.Text;

namespace backend.Common
{
    public static class FileHashHelper
    {
        /// <summary>
        /// 將 Base64 圖檔轉為 SHA256 FileHash
        /// </summary>
        public static string ComputeSha256FromBase64(string base64Image)
        {
            if (string.IsNullOrWhiteSpace(base64Image))
                return null;

            // 若為 data:image/...;base64,xxxx 先移除 header
            if (base64Image.Contains(","))
                base64Image = base64Image.Substring(base64Image.IndexOf(",") + 1);

            byte[] fileBytes = Convert.FromBase64String(base64Image);

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(fileBytes);

                // 轉成 Hex 字串 (小寫，與 SQL 常用格式一致)
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                    sb.Append(b.ToString("x2"));

                return sb.ToString();
            }
        }
    }

}
