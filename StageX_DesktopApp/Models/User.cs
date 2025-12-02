using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StageX_DesktopApp.Models
{
    // Ánh xạ class này với bảng 'users' trong MySQL
    [Table("users")]
    public class User
    {
        // Khóa chính của bảng (Primary Key)
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("account_name")]
        public string? AccountName { get; set; }

        // Lưu chuỗi mã hóa (Hash) của mật khẩu, KHÔNG lưu mật khẩu gốc
        [Column("password")]
        public string PasswordHash { get; set; }

        // Vai trò: 'Admin', 'Nhân viên'
        [Column("user_type")]
        public string Role { get; set; }
        // Trạng thái tài khoản: 'hoạt động' hoặc 'khóa'
        [Column("status")]
        public string? Status { get; set; }

        // GHI CHÚ: Thêm thuộc tính này để sửa lỗi 'IsVerified'
        [Column("is_verified")]
        public bool? IsVerified { get; set; }

        // Mối quan hệ 1-1 (cho trang Hồ sơ)
        public virtual UserDetail UserDetail { get; set; }
    }
}