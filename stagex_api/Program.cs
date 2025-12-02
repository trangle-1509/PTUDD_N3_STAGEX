using Microsoft.EntityFrameworkCore;
using Stagex.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// --- Đăng ký các dịch vụ (Services) ---

// 1. Đăng ký Controller
builder.Services.AddControllers();
// 2. Cấu hình Swagger (Tài liệu API tự động)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// 3. Đăng ký kết nối MySQL
// Lấy chuỗi kết nối từ appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// Sử dụng Pomelo.EntityFrameworkCore.MySql
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

var app = builder.Build();

// Cấu hình đường dẫn yêu cầu HTTP. Này đề phòng lỗi thôi, cứ cho chạy
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
