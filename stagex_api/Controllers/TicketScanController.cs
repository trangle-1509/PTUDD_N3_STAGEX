using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stagex.Api.Data;
using Stagex.Api.Models;

namespace Stagex.Api.Controllers
{
    // Định nghĩa đây là một API Controller
    // Route mặc định sẽ là: api/TicketScan
    [ApiController]
    [Route("api/[controller]")]
    public class TicketScanController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        // Constructor: Inject DbContext để truy cập cơ sở dữ liệu
        public TicketScanController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        /// Hàm này nhận vào một request chứa JSON mã vé,
        /// Trả về một kết quả(thường là object hoặc JSON) nói rõ vé đó accepted or rejected.</returns>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ScanRequest request)
        {
            // Lấy mã vé từ thuộc tính duy nhất "code".
            string? code = request.code;
            // 1. Kiểm tra payload: nếu không có hoặc rỗng -> lỗi BAD_REQUEST
            if (string.IsNullOrWhiteSpace(code))
            {
                var message = "Sai định dạng";
                return BadRequest(new
                {
                    status = "BAD_REQUEST",
                    message,
                    code = "BAD_REQUEST",
                    codevalue = message
                });
            }
            // 2. Mã phải là số và nên có 13 chữ số (theo chuẩn vé). Nếu không hợp lệ -> lỗi BAD_REQUEST
            if (!long.TryParse(code, out var numericCode) || code.Length != 13)
            {
                var message = "Sai định dạng";
                return BadRequest(new
                {
                    status = "BAD_REQUEST",
                    message,
                    code = "BAD_REQUEST",
                    codevalue = message
                });
            }

            // 3. Truy vấn cơ sở dữ liệu xem vé có tồn tại không
            var ticket = await _dbContext.Tickets.FirstOrDefaultAsync(t => t.TicketCode == numericCode);
            if (ticket == null)
            {
                var message = "Không tồn tại";
                return NotFound(new
                {
                    status = "NOT_FOUND",
                    message,
                    code = "NOT_FOUND",
                    codevalue = message
                });
            }

            // 4. Xử lý theo trạng thái vé
            // Nếu vé còn hợp lệ (trạng thái "Hợp lệ" hoặc gặp lỗi chính tả như "Hơp lệ") -> đánh dấu đã sử dụng
            if (ticket.Status == "Hợp lệ" || ticket.Status == "Hơp lệ")
            {
                ticket.Status = "Đã sử dụng";
                ticket.UpdatedAt = DateTime.Now;
                await _dbContext.SaveChangesAsync();

                var message = $"Vé hợp lệ. Đã cập nhật trạng thái vé {code}.";
                return Ok(new
                {
                    status = "VALID",
                    message,
                    code = "VALID",
                    codevalue = message
                });
            }

            // Các trạng thái còn lại (Đã sử dụng, Đã hủy, Đang chờ, hoặc bất kỳ trạng thái khác) đều xem như đã dùng
            var msg = "Vé đã sử dụng";
            return Ok(new
            {
                status = "USED",
                message = msg,
                code = "USED",
                codevalue = msg
            });
        }
    }
}