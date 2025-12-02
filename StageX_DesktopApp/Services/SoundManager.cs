using System;
using System.IO;
using System.Windows.Media; // Quan trọng để chơi MP3

namespace StageX_DesktopApp.Services
{
    public static class SoundManager
    {
        // Đối tượng MediaPlayer để phát nhạc
        private static MediaPlayer _player = new MediaPlayer();
        // Đường dẫn gốc của ứng dụng (nơi file .exe chạy)
        private static string _basePath = AppDomain.CurrentDomain.BaseDirectory;

        // --- CÁC HÀM GỌI NHANH ---
        public static void PlaySuccess() => PlaySound("success.mp3");
        public static void PlayError() => PlaySound("error.mp3");
        public static void PlayLogout() => PlaySound("log out.mp3");
        public static void PlayClick() => PlaySound("click.mp3");

        // Hàm xử lý logic phát file
        private static void PlaySound(string fileName)
        {
            try
            {
                // Tìm file trong thư mục 'Sounds'
                string fullPath = Path.Combine(_basePath, "Sounds", fileName);

                if (File.Exists(fullPath))
                {
                    _player.Open(new Uri(fullPath));
                    _player.Volume = 1.0;
                    _player.Play();
                }
                else
                {
                }
            }
            catch
            {
            }
        }
    }
}