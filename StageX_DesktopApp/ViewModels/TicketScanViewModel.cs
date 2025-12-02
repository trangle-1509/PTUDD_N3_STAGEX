using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StageX_DesktopApp.Data;
using StageX_DesktopApp.Models;
using StageX_DesktopApp.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace StageX_DesktopApp.ViewModels
{
    // Kế thừa ObservableObject để hỗ trợ thông báo thay đổi dữ liệu (INotifyPropertyChanged)
    public partial class TicketScanViewModel : ObservableObject
    {
        // Kế thừa ObservableObject để hỗ trợ thông báo thay đổi dữ liệu (INotifyPropertyChanged)
        private readonly TicketScanService _scanService;

        // Biến chứa mã vé đang được nhập/quét (Binding hai chiều với TextBox)
        [ObservableProperty]
        private string ticketCode = string.Empty;

        // Danh sách lịch sử các lần quét (Binding ra DataGrid trên giao diện)
        // ObservableCollection giúp giao diện tự cập nhật khi thêm/xóa phần tử
        [ObservableProperty]
        private ObservableCollection<Models.ScanHistoryItem> scanHistory = new();

        
        /// Tập hợp các vé đã được quét và đánh dấu là đã sử dụng. Chế độ xem
        /// liên kết với tập hợp này để hiển thị danh sách các vé đã sử dụng. Nó được
        /// làm mới định kỳ và sau mỗi lần quét thành công để phản ánh
        /// trạng thái hiện tại của cơ sở dữ liệu.
        [ObservableProperty]
        private ObservableCollection<Ticket> usedTickets = new();

        // Timer dùng để tự động làm mới danh sách vé đã sử dụng định kỳ (nếu cần realtime)
        private readonly DispatcherTimer _refreshTimer;

        public TicketScanViewModel()
        {
            // Khởi tạo Service với địa chỉ API cục bộ (Localhost)
            // Cần đảm bảo API Server đang chạy ở cổng này
            _scanService = new TicketScanService("http://localhost:5000/");

            // Tải danh sách vé đã sử dụng ngay khi mở màn hình
            _ = LoadUsedTicketsAsync();

            // Thiết lập bộ hẹn giờ để làm mới danh sách vé đã sử dụng theo định kỳ 5s
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            // Đăng ký sự kiện: Mỗi khi hết giờ sẽ gọi hàm LoadUsedTicketsAsync
            _refreshTimer.Tick += async (_, __) => await LoadUsedTicketsAsync();
            _refreshTimer.Start();
        }

        // Hàm tải danh sách các vé có trạng thái "Đã soát vé" từ Database
        private async Task LoadUsedTicketsAsync()
        {
            using var context = new AppDbContext();

            List<Ticket> tickets;
            try
            {
                // Truy vấn bảng Tickets, lấy các vé có Status là 'Đã soát vé'
                // Sắp xếp giảm dần theo ID (mới nhất lên đầu)
                tickets = await context.Tickets
                    .Where(t => t.Status == "Đã sử dụng")
                    .OrderByDescending(t => t.UpdatedAt)
                    .ToListAsync();
            }
            catch
            {
                // Không log, không throw — nếu lỗi DB thì giữ nguyên danh sách cũ
                return;
            }

            // Chỉ cập nhật UI nếu query thành công
            UsedTickets.Clear();
            foreach (var t in tickets)
            {
                UsedTickets.Add(t);
            }
        }

        // Command: Xử lý logic khi người dùng nhấn nút "Quét" hoặc nhấn Enter
        // async Task: Xử lý bất đồng bộ để không làm treo giao diện khi chờ API
        [RelayCommand]
        private async Task ScanAsync()
        {
            if (string.IsNullOrWhiteSpace(TicketCode))
            {
                return;
            }
            // Validate (Kiểm tra dữ liệu đầu vào)
            // Loại bỏ khoảng trắng thừa đầu/cuối
            string trimmedCode = TicketCode.Trim();
            string message;

            // Xác thực mã có chính xác 13 chữ số và nằm trong phạm vi cho phép.
            bool isNumeric = long.TryParse(trimmedCode, out long numericCode);
            // Điều kiện hợp lệ:
            // - Phải là số
            // - Độ dài đúng 13 ký tự (theo quy chuẩn EAN-13 hoặc quy định riêng của rạp)
            // - Nằm trong khoảng giá trị hợp lý (13 chữ số)
            if (!isNumeric || trimmedCode.Length != 13 || numericCode < 1000000000000L || numericCode > 9999999999999L)
            {
                message = $"Mã vé không hợp lệ: {trimmedCode}. Mã vé phải gồm 13 chữ số";
                // Ghi lại nỗ lực quét không hợp lệ trong lịch sử
                ScanHistory.Add(new Models.ScanHistoryItem
                {
                    Timestamp = DateTime.Now,
                    TicketCode = trimmedCode,
                    Message = message
                });
                TicketCode = string.Empty;
                return;
            }
            // Gọi API kiểm tra vé
            try
            {
                // Gọi hàm ScanTicketAsync từ Service, hàm này sẽ trả về thông báo từ Server
                message = await _scanService.ScanTicketAsync(trimmedCode);
            }
            catch (Exception ex)
            {
                message = $"Lỗi: {ex.Message}";
            }

            // Thêm kết quả vào lịch sử quét để lưu giữ hồ sơ.
            ScanHistory.Add(new Models.ScanHistoryItem
            {
                Timestamp = DateTime.Now,
                TicketCode = trimmedCode,
                Message = message
            });

            // Xóa đầu vào cho lần quét tiếp theo
            TicketCode = string.Empty;

            // Làm mới danh sách vé đã sử dụng sau khi quét, vì khi quét thành công
            // có thể trạng thái của vé đã được cập nhật trong cơ sở dữ liệu.
            await LoadUsedTicketsAsync();
        }
    }
}