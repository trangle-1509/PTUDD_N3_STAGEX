using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using StageX_DesktopApp.Models;
using System.Collections.Generic;
using System.Linq;

namespace StageX_DesktopApp.Services
{
    public class RevenueForecastingService
    {
        private MLContext _mlContext;

        public RevenueForecastingService()
        {
            _mlContext = new MLContext(seed: 0); // Seed cố định để kết quả nhất quán
        }

        /// <summary>
        /// Dự báo doanh thu cho 'horizon' ngày tiếp theo
        /// </summary>
        /// <param name="historyData">Danh sách doanh thu lịch sử</param>
        /// <param name="horizon">Số ngày muốn dự báo (ví dụ: 7 ngày)</param>
        public RevenueForecast Predict(List<RevenueInput> historyData, int horizon = 7)
        {
            // 1. Chuyển dữ liệu List -> IDataView
            var dataView = _mlContext.Data.LoadFromEnumerable(historyData);

            var forecastingPipeline = _mlContext.Forecasting.ForecastBySsa(
                outputColumnName: nameof(RevenueForecast.ForecastedRevenue),
                inputColumnName: nameof(RevenueInput.TotalRevenue),
                windowSize: 2,       // Kích thước cửa sổ phân tích (7 ngày 1 tuần)
                seriesLength: 6,    // Độ dài chuỗi tối thiểu để học (30 ngày)
                trainSize: historyData.Count,
                horizon: horizon,
                confidenceLevel: 0.95f,
                confidenceLowerBoundColumn: nameof(RevenueForecast.LowerBound),
                confidenceUpperBoundColumn: nameof(RevenueForecast.UpperBound));

            // 3. Train model (Fit)
            var model = forecastingPipeline.Fit(dataView);

            // 4. Tạo engine dự báo
            var forecastingEngine = model.CreateTimeSeriesEngine<RevenueInput, RevenueForecast>(_mlContext);

            // 5. Thực hiện dự báo
            var forecast = forecastingEngine.Predict();

            return forecast;
        }
    }
}