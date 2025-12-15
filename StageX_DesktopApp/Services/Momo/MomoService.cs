using Newtonsoft.Json.Linq;
using StageX_DesktopApp.Services.Momo;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;
using System.Drawing;
using System.Drawing.Imaging;

namespace StageX_DesktopApp.Services.Momo
{
    /// <summary>
    /// Service xử lý các nghiệp vụ liên quan đến cổng thanh toán MoMo (Môi trường Test/Sandbox).
    /// Bao gồm: Tạo yêu cầu thanh toán, tạo mã QR (Online/Offline), và kiểm tra trạng thái giao dịch.
    /// </summary>
    public class MomoService
    {
        // --- CẤU HÌNH TÀI KHOẢN MOMO TEST (SANDBOX) ---
        private const string PARTNER_CODE = "MOMOCGNF20251214_TEST";
        private const string ACCESS_KEY = "IRXQJccoUNyM2E2x";
        private const string SECRET_KEY = "BGCAJqk7dhpO3unXBD8yNs15moSuY6HJ";

        // Khởi tạo đối tượng bảo mật để ký dữ liệu
        private readonly MoMoSecurity _security = new MoMoSecurity();

        // Tạo yêu cầu thanh toán gửi lên MoMo API.
        public async Task<(BitmapImage? Image, string OrderId, string RequestId)> GeneratePaymentAsync(decimal amount, string orderInfo)
        {
            // Endpoint API tạo đơn hàng của môi trường Test
            string endpoint = "https://test-payment.momo.vn/v2/gateway/api/create";

            // Tạo mã định danh duy nhất cho giao dịch này (sử dụng GUID)
            string orderId = Guid.NewGuid().ToString("N");
            string requestId = Guid.NewGuid().ToString("N");

            // IPN Url dùng để Server MoMo gọi lại báo kết quả. Dùng webhook.site để test khi không có Server thực.
            string redirectUrl = "https://momo.vn";

            // IPN Url dùng để Server MoMo gọi lại báo kết quả. Dùng webhook.site để test khi không có Server thực.
            string ipnUrl = "https://webhook.site/8095cf34-d952-448d-b231-550802c23eb5";
            string extraData = string.Empty;
            string requestType = "captureWallet";

            // 1. TẠO CHỮ KÝ (SIGNATURE)
            // Chuỗi rawHash phải tuân thủ đúng thứ tự alphabet của tên tham số:
            // accessKey -> amount -> extraData -> ipnUrl -> orderId -> orderInfo -> partnerCode -> redirectUrl -> requestId -> requestType
            string rawHash =
                $"accessKey={ACCESS_KEY}&amount={Convert.ToInt64(amount)}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={PARTNER_CODE}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}";

            // Ký chuỗi rawHash bằng SecretKey
            string signature = _security.SignSha256(rawHash, SECRET_KEY);

            // 2. TẠO JSON PAYLOAD ĐỂ GỬI ĐI
            var message = new JObject
            {
                { "partnerCode", PARTNER_CODE },
                { "partnerName", "StageX" },
                { "storeId", "StageX01" },
                { "requestId", requestId },
                { "amount", Convert.ToInt64(amount) },
                { "orderId", orderId },
                { "orderInfo", orderInfo },
                { "redirectUrl", redirectUrl },
                { "ipnUrl", ipnUrl },
                { "lang", "vi" },
                { "extraData", extraData },
                { "requestType", requestType },
                { "signature", signature }
            };

            // 3. GỬI REQUEST HTTP POST
            string response = await PaymentRequest.SendPaymentRequestAsync(endpoint, message.ToString(Newtonsoft.Json.Formatting.None));

            // 4. XỬ LÝ PHẢN HỒI (RESPONSE)
            if (!string.IsNullOrWhiteSpace(response) && response.TrimStart().StartsWith("{"))
            {
                try
                {
                    JObject json = JObject.Parse(response);
                    // MoMo trả về resultCode = 0 khi thành công
                    if (json["resultCode"]?.ToString() == "0")
                    {
                        // Ưu tiên dùng qrCodeUrl. Nếu không có, dùng payUrl để tự tạo QR
                        string qrUrl = json["qrCodeUrl"]?.ToString() ?? string.Empty;
                        string payUrl = json["payUrl"]?.ToString() ?? string.Empty;
                        if (string.IsNullOrEmpty(qrUrl) && !string.IsNullOrEmpty(payUrl))
                        {
                            // Lấy trực tiếp URL thanh toán để mã hóa; không gọi API bên thứ ba vì môi trường offline
                            // Sử dụng ZXing để tạo QR code offline từ payUrl
                            var localImage = GenerateLocalQrImage(payUrl);
                            return (localImage, orderId, requestId);
                        }
                        if (!string.IsNullOrEmpty(qrUrl))
                        {
                            // Thử tải ảnh QR trực tiếp từ MoMo (nếu được phép trong môi trường chạy)
                            try
                            {
                                using var webClient = new WebClient();
                                webClient.Headers.Add("User-Agent", "Mozilla/5.0");
                                byte[] data = await webClient.DownloadDataTaskAsync(qrUrl);
                                // Chuyển đổi byte[] thành BitmapImage để hiển thị lên WPF
                                using var ms = new MemoryStream(data);
                                var bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.StreamSource = ms;
                                bitmap.EndInit();
                                bitmap.Freeze();
                                return (bitmap, orderId, requestId);
                            }
                            catch
                            {
                                // Nếu tải trực tiếp thất bại, tạo QR offline từ payUrl (nếu có)
                                if (!string.IsNullOrEmpty(payUrl))
                                {
                                    var localImage = GenerateLocalQrImage(payUrl);
                                    return (localImage, orderId, requestId);
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
            }

            // Nếu đến đây nghĩa là không thể lấy QR từ API MoMo (lỗi mạng hoặc dữ liệu không hợp lệ)
            // Tạo mã QR cục bộ đơn giản với thông tin đơn hàng để có gì đó hiển thị
            var fallbackData = $"orderId={orderId};amount={amount:N0};info={orderInfo}";
            var fallbackImage = GenerateLocalQrImage(fallbackData);
            return (fallbackImage, orderId, requestId);
        }
        // Tạo hình ảnh QR code cục bộ từ một chuỗi dữ liệu sử dụng thư viện ZXing
        private static BitmapImage? GenerateLocalQrImage(string content)
        {
            try
            {
                // Sử dụng ZXing để tạo mã QR. BarcodeWriter sẽ trả về đối tượng Bitmap của System.Drawing.
                var writer = new BarcodeWriter
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new EncodingOptions
                    {
                        Height = 250,
                        Width = 250,
                        Margin = 1
                    }
                };
                // Tạo Bitmap từ nội dung
                using (Bitmap bitmap = writer.Write(content))
                {
                    // Chuyển Bitmap (System.Drawing) sang BitmapImage (WPF)
                    using (var ms = new MemoryStream())
                    {
                        bitmap.Save(ms, ImageFormat.Png);
                        ms.Position = 0;
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = ms;
                        image.EndInit();
                        image.Freeze();
                        return image;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        // Kiểm tra trạng thái thanh toán của một đơn hàng (Query Status).
        public async Task<bool> QueryPaymentAsync(string orderId, string requestId)
        {
            string endpoint = "https://test-payment.momo.vn/v2/gateway/api/query";

            // 1. Tạo chữ ký cho request query
            // Raw string format: accessKey={}&orderId={}&partnerCode={}&requestId={}
            string rawHash = $"accessKey={ACCESS_KEY}&orderId={orderId}&partnerCode={PARTNER_CODE}&requestId={requestId}";
            string signature = _security.SignSha256(rawHash, SECRET_KEY);

            // 2. Tạo JSON Payload
            var message = new JObject
            {
                { "partnerCode", PARTNER_CODE },
                { "requestId", requestId },
                { "orderId", orderId },
                { "signature", signature },
                { "lang", "vi" }
            };

            // 3. Gửi Request
            string response = await PaymentRequest.SendPaymentRequestAsync(endpoint, message.ToString(Newtonsoft.Json.Formatting.None));

            // 4. Xử lý kết quả
            if (string.IsNullOrWhiteSpace(response) || !response.TrimStart().StartsWith("{")) return false;
            try
            {
                var json = JObject.Parse(response);
                return json["resultCode"]?.ToString() == "0";
            }
            catch
            {
                return false;
            }
        }
    }
}