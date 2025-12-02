using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StageX_DesktopApp.Services;
using StageX_DesktopApp.Utilities;
using System.Windows;
using System.Windows.Controls;

namespace StageX_DesktopApp.ViewModels
{
    public partial class ChangePasswordViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;

        public ChangePasswordViewModel()
        {
            _dbService = new DatabaseService();
        }

        // Command xử lý đổi mật khẩu
        // Parameter nhận vào là mảng các đối tượng (object[]) do MultiBinding gửi tới
        [RelayCommand]
        private async void SavePassword(object parameter)
        {
            // Kiểm tra tham số có phải là mảng và đủ 4 phần tử không
            if (parameter is object[] controls && controls.Length == 4)
            {
                // Ép kiểu các thành phần giao diện
                var currentBox = controls[0] as PasswordBox;
                var newBox = controls[1] as PasswordBox;
                var confirmBox = controls[2] as PasswordBox;
                var window = controls[3] as Window;
                // Lấy chuỗi mật khẩu từ các ô
                string currentPass = currentBox?.Password;
                string newPass = newBox?.Password;
                string confirmPass = confirmBox?.Password;

                // 1. Validate: Kiểm tra rỗng
                if (string.IsNullOrEmpty(currentPass) || string.IsNullOrEmpty(newPass) || string.IsNullOrEmpty(confirmPass))
                {
                    MessageBox.Show("Vui lòng nhập đủ thông tin!"); return;
                }
                // 2. Validate: Độ dài mật khẩu mới
                if (newPass.Length < 3)
                {
                    MessageBox.Show("Mật khẩu mới quá ngắn!"); return;
                }
                // 3. Validate: Khớp mật khẩu xác nhận
                if (newPass != confirmPass)
                {
                    MessageBox.Show("Mật khẩu xác nhận không khớp!"); return;
                }
                // 4. Kiểm tra Mật khẩu hiện tại có đúng không (So sánh với Hash trong Session)
                if (!BCrypt.Net.BCrypt.Verify(currentPass, AuthSession.CurrentUser.PasswordHash))
                {
                    MessageBox.Show("Mật khẩu hiện tại không đúng!"); return;
                }
                // 5. Mã hóa mật khẩu mới trước khi lưu (Bảo mật)
                string newHash = BCrypt.Net.BCrypt.HashPassword(newPass);
                // 6. Gọi Service cập nhật xuống Database
                await _dbService.ChangePasswordAsync(AuthSession.CurrentUser.UserId, newHash);
                // 7. Cập nhật lại thông tin trong Session hiện tại (để không cần đăng nhập lại)
                AuthSession.CurrentUser.PasswordHash = newHash;
                MessageBox.Show("Đổi mật khẩu thành công!");

                window?.Close();
            }
        }
    }
}