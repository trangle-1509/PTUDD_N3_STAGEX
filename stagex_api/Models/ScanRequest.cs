namespace Stagex.Api.Models
{
    public class ScanRequest
    {
        /// <summary>
        /// Mã vé được gửi từ client. Ứng dụng quét trên điện thoại hoặc desktop
        /// sẽ truyền mã vé thông qua trường này.
        /// </summary>
        public string? code { get; set; }
    }
}