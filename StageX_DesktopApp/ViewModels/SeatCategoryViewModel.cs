using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StageX_DesktopApp.Models;
using StageX_DesktopApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace StageX_DesktopApp.ViewModels
{
    public partial class SeatCategoryViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;

        [ObservableProperty] private List<SeatCategory> _categories = new();

        [ObservableProperty] private int _categoryId;
        [ObservableProperty] private string _categoryName = "";
        [ObservableProperty] private string _basePriceStr = "";
        [ObservableProperty] private string _saveBtnContent = "Thêm";
        [ObservableProperty] private bool _isEditing = false;

        // === DANH SÁCH MÀU + TRẠNG THÁI ===
        private static readonly string[] PresetColors =
        {
            "#E74C3C", "#C0392B", "#E67E22", "#D35400", "#F39C12", "#F1C40F",
            "#27AE60", "#16A085", "#1ABC9C", "#3498DB", "#2980B9", "#9B59B6",
            "#8E44AD", "#EC407A", "#FF5722", "#FF9800", "#FFC107", "#4CAF50",
            "#009688", "#00BCD4", "#03A9F4", "#2196F3", "#3F51B5", "#9C27B0",
            "#E91E63", "#FFEB3B", "#CDDC39", "#795548", "#607D8B"
        };

        private static readonly HashSet<string> UsedColors = new();
        private static readonly Random Random = new();
        private static readonly object LockObject = new();

        public SeatCategoryViewModel()
        {
            _dbService = new DatabaseService();
            LoadCategoriesCommand.Execute(null);
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        public class SeatCategoryChangedMessage
        {
            public string Value { get; }
            public SeatCategoryChangedMessage(string value) => Value = value;
        }

        [RelayCommand]
        private async Task LoadCategories()
        {
            Categories = await _dbService.GetSeatCategoriesAsync();

            // Đồng bộ UsedColors từ DB mỗi khi load
            lock (LockObject)
            {
                UsedColors.Clear();
                foreach (var cat in Categories)
                {
                    if (!string.IsNullOrWhiteSpace(cat.ColorClass) && cat.ColorClass.StartsWith("#"))
                    {
                        UsedColors.Add(cat.ColorClass.Trim().ToUpperInvariant());
                    }
                }
            }
        }

        [RelayCommand]
        private void Edit(SeatCategory cat)
        {
            if (cat == null) return;
            CategoryId = cat.CategoryId;
            CategoryName = cat.CategoryName;
            BasePriceStr = cat.BasePrice.ToString("F0");
            SaveBtnContent = "Lưu";
            IsEditing = true;
        }

        [RelayCommand]
        private void Cancel()
        {
            CategoryId = 0;
            CategoryName = "";
            BasePriceStr = "";
            SaveBtnContent = "Thêm";
            IsEditing = false;
        }

        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(CategoryName) || CategoryName.Contains("Tên hạng"))
            {
                MessageBox.Show("Vui lòng nhập tên hạng ghế hợp lệ!");
                return;
            }

            if (!decimal.TryParse(BasePriceStr, out decimal price) || price < 0)
            {
                MessageBox.Show("Vui lòng nhập giá hợp lệ!");
                return;
            }

            try
            {
                var cat = new SeatCategory
                {
                    CategoryId = CategoryId,
                    CategoryName = CategoryName.Trim(),
                    BasePrice = price
                };

                // CHỈ KHI THÊM MỚI MỚI RANDOM MÀU
                if (CategoryId == 0)
                {
                    cat.ColorClass = GetRandomVibrantColor();
                }
                else
                {
                    // Khi sửa: giữ nguyên màu cũ
                    cat.ColorClass = Categories.FirstOrDefault(c => c.CategoryId == CategoryId)?.ColorClass;
                }

                // Đảm bảo không bao giờ null
                cat.ColorClass ??= "#95A5A6"; // fallback cuối cùng (xám đậm)

                await _dbService.SaveSeatCategoryAsync(cat);

                MessageBox.Show(CategoryId > 0 ? "Cập nhật thành công!" : "Thêm mới thành công!");

                Cancel();
                WeakReferenceMessenger.Default.Send(new SeatCategoryChangedMessage("Updated"));
                await LoadCategories();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }

        [RelayCommand]
        private async Task Delete(SeatCategory cat)
        {
            if (cat == null) return;

            bool isInUse = await _dbService.IsSeatCategoryInUseAsync(cat.CategoryId);
            if (isInUse)
            {
                MessageBox.Show($"Không thể xóa hạng ghế '{cat.CategoryName}'!\n\nLý do: Hạng ghế này đang được dùng trong rạp.",
                    "Không thể xóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Xóa hạng ghế '{cat.CategoryName}'?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question)
                == MessageBoxResult.Yes)
            {
                await _dbService.DeleteSeatCategoryAsync(cat.CategoryId);

                // Giải phóng màu khi xóa
                lock (LockObject)
                {
                    if (!string.IsNullOrWhiteSpace(cat.ColorClass))
                        UsedColors.Remove(cat.ColorClass.Trim().ToUpperInvariant());
                }

                MessageBox.Show("Đã xóa thành công!");
                await LoadCategories();

                if (CategoryId == cat.CategoryId) Cancel();
            }
        }

        // ========================== HÀM RANDOM MÀU KHÔNG TRÙNG ==========================
        private string GetRandomVibrantColor()
        {
            lock (LockObject)
            {
                var available = PresetColors
                    .Select(c => c.ToUpperInvariant())
                    .Except(UsedColors)
                    .ToList();

                if (available.Any())
                {
                    string color = available[Random.Next(available.Count)];
                    UsedColors.Add(color);
                    return color; // Trả về dạng #FF5733
                }

                // Hết màu cố định → sinh màu mới đẹp
                string newColor;
                int attempts = 0;
                do
                {
                    newColor = GenerateVibrantColor();
                    attempts++;
                } while (UsedColors.Contains(newColor.ToUpperInvariant()) && attempts < 50);

                UsedColors.Add(newColor.ToUpperInvariant());
                return newColor;
            }
        }

        // Hàm sinh màu đẹp (đã thêm lại!)
        private string GenerateVibrantColor()
        {
            double h = Random.NextDouble();                    // Hue: 0-1
            double s = 0.75 + Random.NextDouble() * 0.25;     // Saturation: 75-100%
            double l = 0.55 + Random.NextDouble() * 0.25;     // Lightness: 55-80%

            var (r, g, b) = HslToRgb(h, s, l);
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        private (int R, int G, int B) HslToRgb(double h, double s, double l)
        {
            double r = 0, g = 0, b = 0;

            if (s == 0)
            {
                r = g = b = l;
            }
            else
            {
                double temp2 = l < 0.5 ? l * (1 + s) : l + s - l * s;
                double temp1 = 2 * l - temp2;

                r = HueToRgb(temp1, temp2, h + 1.0 / 3.0);
                g = HueToRgb(temp1, temp2, h);
                b = HueToRgb(temp1, temp2, h - 1.0 / 3.0);
            }

            return ((int)(r * 255), (int)(g * 255), (int)(b * 255));
        }

        private double HueToRgb(double t1, double t2, double h)
        {
            if (h < 0) h += 1;
            if (h > 1) h -= 1;
            if (6 * h < 1) return t1 + (t2 - t1) * 6 * h;
            if (2 * h < 1) return t2;
            if (3 * h < 2) return t1 + (t2 - t1) * (2.0 / 3.0 - h) * 6;
            return t1;
        }
    }
}