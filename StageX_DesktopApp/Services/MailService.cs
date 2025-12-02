using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using System.Threading.Tasks;
using System;

namespace StageX_DesktopApp.Services
{
    public class MailService
    {
        // Cấu hình SMTP Server (Gmail)
        private const string Host = "smtp.gmail.com";
        private const int Port = 587;
        // Email dùng để gửi (Cần bật 2-Step Verification và tạo App Password trên Google)
        private const string Username = "dtngoc.video@gmail.com";
        private const string Password = "yfdcojadkfblargt";
        private const string FromEmail = "no-reply@stagex.local";
        private const string FromName = "StageX";

        // Hàm gửi email thông báo tài khoản mới
        // Trả về true nếu gửi thành công, false nếu thất bại
        public async Task<bool> SendNewAccountEmailAsync(string toEmail, string accountName, string plainPassword)
        {
            try
            {
                // 1. Tạo nội dung email
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(FromName, FromEmail));
                message.To.Add(new MailboxAddress(string.Empty, toEmail));
                message.Subject = "Thông báo tài khoản mới";
                // Nội dung text thuần (có thể đổi sang HTML nếu muốn đẹp hơn)
                message.Body = new TextPart("plain")
                {
                    Text = $"Bạn đã được StageX cung cấp tài khoản mới,\nTên tài khoản là: {accountName}\nMật khẩu là: {plainPassword}"
                };
                // 2. Kết nối SMTP và gửi
                using var client = new SmtpClient();
                // Kết nối đến server Gmail qua cổng 587 (TLS)
                await client.ConnectAsync(Host, Port, SecureSocketOptions.StartTls);
                // Đăng nhập vào tài khoản gửi
                await client.AuthenticateAsync(Username, Password);
                // Gửi thư
                await client.SendAsync(message);
                // Ngắt kết nối
                await client.DisconnectAsync(true);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}