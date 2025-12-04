using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using QuanLyXe03.Helpers;

namespace QuanLyXe03.Services
{
    public class PlateRecognitionService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;

        public PlateRecognitionService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            //  Đọc URL từ appsettings.json
            _apiUrl = SettingsManager.Settings.PlateRecognition?.ApiUrl ?? "http://127.0.0.1:8000/predict";

            Console.WriteLine($"🔍 PlateRecognitionService initialized");
            Console.WriteLine($"   API URL: {_apiUrl}");
        }

        /// <summary>
        /// Gửi ảnh đến API nhận diện biển số
        /// </summary>
        public async Task<(string plateText, string vehicleClass, bool success, string errorMessage)> RecognizePlateAsync(byte[] imageBytes)
        {
            try
            {
                using var content = new MultipartFormDataContent();

                var imageContent = new ByteArrayContent(imageBytes);
                imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
                content.Add(imageContent, "file", "image.jpg");

                var response = await _httpClient.PostAsync(_apiUrl, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var plates = JsonSerializer.Deserialize<JsonElement>(responseString);

                    if (plates.ValueKind == JsonValueKind.Array && plates.GetArrayLength() > 0)
                    {
                        var firstPlate = plates[0];
                        var plateText = firstPlate.TryGetProperty("plate_text", out var p) ? p.GetString() ?? "N/A" : "N/A";
                        var vehicleClass = firstPlate.TryGetProperty("vehicle_class", out var c) ? c.GetString() ?? "unknown" : "unknown";
                        return (plateText, vehicleClass, true, string.Empty);
                    }
                    return ("N/A", "unknown", false, "Không phát hiện biển số");
                }
                else
                {
                    return ("N/A", "unknown", false, $"Lỗi API {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                return ("N/A", "unknown", false, $"Không kết nối được API: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                return ("N/A", "unknown", false, "API timeout - server chậm");
            }
            catch (Exception ex)
            {
                return ("N/A", "unknown", false, $"Lỗi: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}