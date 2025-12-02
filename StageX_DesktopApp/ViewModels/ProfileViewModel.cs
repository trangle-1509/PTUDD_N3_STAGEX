using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StageX_DesktopApp.Models;
using StageX_DesktopApp.Services;
using StageX_DesktopApp.Utilities;
using StageX_DesktopApp.Views; // Để gọi Window đổi pass
using System;
using System.Threading.Tasks;
using System.Windows;

namespace StageX_DesktopApp.ViewModels
{
    public partial class ProfileViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;
        // --- CÁC BIẾN HIỂN THỊ (READ-ONLY) ---
        // Tên tài khoản và Email lấy từ bảng Users, chỉ hiển thị, không sửa ở form này
        [ObservableProperty] private string _accountName = "Loading...";
        [ObservableProperty] private string _email = "...";
        // Chữ cái đầu của tên để làm Avatar (VD: "Nguyen" -> "N")
        [ObservableProperty] private string _initial = "U";
        // --- CÁC BIẾN NHẬP LIỆU (EDITABLE) ---
        // Các thông tin này nằm trong bảng UserDetail
        [ObservableProperty] private string _fullName;
        [ObservableProperty] private string _address;
        [ObservableProperty] private string _phone;
        [ObservableProperty] private DateTime _dateOfBirth = DateTime.Now;

        public ProfileViewModel()
        {
            _dbService = new DatabaseService();
            // Tự động tải thông tin khi mở màn hình
            LoadProfileCommand.Execute(null);
        }
        // Command: Tải thông tin người dùng đang đăng nhập
        [RelayCommand]
        private async Task LoadProfile()
        {
            // Kiểm tra phiên đăng nhập
            if (AuthSession.CurrentUser == null) return;
            // Gọi DB lấy thông tin User + UserDetail (Join bảng)
            var user = await _dbService.GetUserWithDetailAsync(AuthSession.CurrentUser.UserId);
            if (user == null) return;
            // Gán thông tin cơ bản
            AccountName = user.AccountName;
            Email = user.Email;
            // Lấy chữ cái đầu tiên, viết hoa
            Initial = !string.IsNullOrEmpty(user.AccountName) ? user.AccountName[0].ToString().ToUpper() : "U";
            // Gán thông tin chi tiết
            if (user.UserDetail != null)
            {
                FullName = user.UserDetail.FullName;
                Address = user.UserDetail.Address;
                Phone = user.UserDetail.Phone;
                // Nếu ngày sinh null thì lấy ngày hiện tại
                DateOfBirth = user.UserDetail.DateOfBirth ?? DateTime.Now;
            }
        }
        // Command: Lưu thông tin chi tiết
        [RelayCommand]
        private async Task SaveInfo()
        {
            if (AuthSession.CurrentUser == null) return;
            try
            {
                // Gọi Service để lưu hoặc cập nhật (Upsert) vào bảng user_detail
                await _dbService.SaveUserDetailAsync(AuthSession.CurrentUser.UserId, FullName, Address, Phone, DateOfBirth);
                MessageBox.Show("Cập nhật hồ sơ thành công!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }
        // Command: Mở cửa sổ đổi mật khẩu
        [RelayCommand]
        private void ChangePassword()
        {
            {
                // Tạo và hiển thị cửa sổ ChangePasswordView dưới dạng Dialog (Modal)
                // Người dùng phải đóng cửa sổ này mới thao tác tiếp được màn hình chính
                var win = new ChangePasswordView();
                win.ShowDialog();
            }
        }
    }
}