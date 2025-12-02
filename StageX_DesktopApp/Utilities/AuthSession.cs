using StageX_DesktopApp.Models;

namespace StageX_DesktopApp.Utilities
{
    public static class AuthSession
    {
        // Biến lưu thông tin người dùng hiện tại (User đang đăng nhập)
        public static User? CurrentUser { get; private set; }
        // Hàm đăng nhập: Lưu thông tin user vào phiên làm việc
        public static void Login(User user)
        {
            CurrentUser = user;
        }
        // Hàm đăng xuất: Xóa thông tin user khỏi bộ nhớ
        public static void Logout()
        {
            CurrentUser = null;
        }
        // Hàm kiểm tra xem có ai đang đăng nhập không
        public static bool IsLoggedIn => CurrentUser != null;
    }
}



