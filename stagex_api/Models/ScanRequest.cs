namespace Stagex.Api.Models
{
    public class ScanRequest
    {
        // Class đại diện cho dữ liệu JSON gửi lên từ Client (Desktop App)
        public string? Barcode { get; set; }
        public string? TicketCode { get; set; }
        public string? ticket_code { get; set; }
        public string? code { get; set; }
    }
}