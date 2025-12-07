using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StageX_DesktopApp.Models;
using StageX_DesktopApp.Services;
using StageX_DesktopApp.Utilities;
using StageX_DesktopApp.Views;
using System.Windows;

namespace StageX_DesktopApp.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // Biến chứa View hiện tại (UserControl) để hiển thị bên phải
        private object _currentView;
        // Tiêu đề cửa sổ (thay đổi theo người đăng nhập)
        public object CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        // Tiêu đề cửa sổ, sẽ thay đổi linh động để hiển thị tên người đang đăng nhập.
        private string _windowTitle;
        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        // Biến kiểm soát việc hiển thị Menu dành cho Admin.
        // Visibility.Visible: Hiện | Visibility.Collapsed: Ẩn hoàn toàn (không chiếm chỗ).
        private Visibility _adminVisibility = Visibility.Collapsed;
        public Visibility AdminVisibility
        {
            get => _adminVisibility;
            set => SetProperty(ref _adminVisibility, value);
        }

        // Biến kiểm soát việc hiển thị Menu dành cho Nhân viên.
        private Visibility _staffVisibility = Visibility.Collapsed;
        public Visibility StaffVisibility
        {
            get => _staffVisibility;
            set => SetProperty(ref _staffVisibility, value);
        }

        // Lưu tên của menu đang được chọn (VD: "Dashboard", "SellTicket").
        // Dùng để tô màu (Highlight) nút menu đang active trên giao diện.
        private string _selectedMenu;
        public string SelectedMenu
        {
            get => _selectedMenu;
            set => SetProperty(ref _selectedMenu, value);
        }

        public MainViewModel()
        {
            LoadUserInfo();
        }

        // Hàm Xác định ai đang đăng nhập và phân quyền hiển thị.
        private void LoadUserInfo()
        {
            // Lấy User từ AuthSession (đã lưu lúc Login)
            var user = AuthSession.CurrentUser;
            if (user == null)
            {
                WindowTitle = "StageX";
                return;
            }

            // Cập nhật tiêu đề cửa sổ
            WindowTitle = $"StageX - Đã đăng nhập: {user.AccountName} ({user.Role})";
            // Phân quyền hiển thị Menu
            if (user.Role == "Admin")
            {
                // Nếu là Admin: Hiện menu Admin, Ẩn menu Nhân viên.
                AdminVisibility = Visibility.Visible;
                StaffVisibility = Visibility.Collapsed;
                NavigateDashboard();
            }
            else
            {
                AdminVisibility = Visibility.Collapsed;
                StaffVisibility = Visibility.Visible;
                NavigateSellTicket();
            }
        }

        // Hàm chung để chuyển trang
        private void NavigateTo(object view, string menuName)
        {
            CurrentView = view; // Đổi nội dung bên phải
            SelectedMenu = menuName; // Cập nhật trạng thái nút menu (tô màu)
            SoundManager.PlayClick();
        }

        // --- CÁC COMMAND ĐIỀU HƯỚNG (Gắn vào nút Menu) ---

        [RelayCommand]
        private void NavigateDashboard() => NavigateTo(new DashboardView(), "Dashboard");

        [RelayCommand]
        private void NavigatePerformance() => NavigateTo(new PerformanceView(), "Performance");

        [RelayCommand]
        private void NavigateShow() => NavigateTo(new ShowManagementView(), "Show");

        [RelayCommand]
        private void NavigateTheater() => NavigateTo(new TheaterSeatView(), "Theater");

        [RelayCommand]
        private void NavigateActor() => NavigateTo(new ActorManagementView(), "Actor");

        [RelayCommand]
        private void NavigateGenre() => NavigateTo(new GenreManagementView(), "Genre");

        [RelayCommand]
        private void NavigateAccount() => NavigateTo(new AccountView(), "Account");

        [RelayCommand]
        private void NavigateSellTicket() => NavigateTo(new SellTicketView(), "SellTicket");

        [RelayCommand]
        private void NavigateBooking() => NavigateTo(new BookingManagementView(), "Booking");

        [RelayCommand]
        private void NavigateTicketScan() => NavigateTo(new TicketScanView(), "TicketScan");

        [RelayCommand]
        private void NavigateProfile() => NavigateTo(new ProfileView(), "Profile");

        // Xử lý Đăng xuất: Xóa session, đóng cửa sổ chính và mở lại màn hình đăng nhập.
        [RelayCommand]
        private void Logout()
        {
            SoundManager.PlayLogout();
            // Xóa thông tin user trong bộ nhớ tạm
            AuthSession.Logout();

            // Mở lại cửa sổ đăng nhập
            var loginWindow = new LoginView();
            loginWindow.Show();

            // Tìm và đóng cửa sổ chính (MainWindow) hiện tại
            foreach (Window window in Application.Current.Windows)
            {
                if (window is MainWindow)
                {
                    window.Close();
                    break;
                }
            }
        }
    }
}