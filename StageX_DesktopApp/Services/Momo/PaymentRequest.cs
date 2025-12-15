using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace StageX_DesktopApp.Services.Momo
{
    public static class PaymentRequest
    {
        private static readonly HttpClient _client = new HttpClient();

        // Gửi dữ liệu JSON đến một URL cụ thể thông qua phương thức POST.
        public static async Task<string> SendPaymentRequestAsync(string endpoint, string json)
        {
            // Tạo đối tượng request với method POST
            using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
            {
                // Đóng gói nội dung JSON, mã hóa UTF8, loại media là application/json
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                try
                {
                    // Gửi request và chờ phản hồi (Asynchronous)   
                    using (var response = await _client.SendAsync(request))
                    {
                        // Đọc nội dung trả về thành chuỗi string
                        string content = await response.Content.ReadAsStringAsync();
                        return content;
                    }
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
        }
    }
}