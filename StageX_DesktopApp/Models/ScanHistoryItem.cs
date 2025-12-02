using System;

namespace StageX_DesktopApp.Models
{
    // Class đại diện cho 1 dòng trong bảng Lịch sử quét
    public class ScanHistoryItem
    {
        public DateTime Timestamp { get; set; }
        public string TicketCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}