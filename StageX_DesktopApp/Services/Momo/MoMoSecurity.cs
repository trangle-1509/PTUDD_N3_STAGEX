using System;
using System.Security.Cryptography;
using System.Text;

namespace StageX_DesktopApp.Services.Momo
{
    /// <summary>
    /// Lớp tiện ích dùng để tạo chữ ký số (Signature) bằng thuật toán HMAC-SHA256.
    /// MoMo yêu cầu mọi request gửi đi phải có chữ ký này để xác thực người gửi là đúng đối tác.
    /// </summary>
    public class MoMoSecurity
    {
        public string SignSha256(string message, string key)
        {
            // 1. Chuyển đổi khóa bí mật và thông điệp sang mảng byte (UTF-8)
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            // 2. Sử dụng thuật toán HMACSHA256 để băm dữ liệu
            using (var hmac = new HMACSHA256(keyBytes))
            {
                byte[] hash = hmac.ComputeHash(messageBytes);
                // 3. Chuyển đổi mảng byte kết quả sang chuỗi Hexadecimal
                // BitConverter trả về dạng "XX-YY-ZZ", ta cần bỏ dấu "-" và chuyển về chữ thường
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }
    }
}