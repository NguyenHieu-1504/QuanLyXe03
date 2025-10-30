using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace QuanLyXe03.Services
{
    public class PlateRecognitionService
    {
        private static readonly string apiUrl = "http://127.0.0.1:8000/predict";
        private readonly HttpClient _httpClient;

        public PlateRecognitionService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        /// <summary>
        /// Gửi ảnh đến API nhận diện biển số
        /// </summary>
        /// <param name="imageBytes">Byte array của ảnh</param>
        /// <returns>Tuple (plateText, vehicleClass, success)</returns>
        public async Task<(string plateText, string vehicleClass, bool success, string errorMessage)> RecognizePlate(byte[] imageBytes)
        {
            try
            {
                using var content = new MultipartFormDataContent();

                var imageContent = new ByteArrayContent(imageBytes);
                imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
                content.Add(imageContent, "file", "image.jpg");

                var response = await _httpClient.PostAsync(apiUrl, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var plates = JsonSerializer.Deserialize<JsonElement>(responseString);

                        if (plates.ValueKind == JsonValueKind.Array)
                        {
                            var enumerator = plates.EnumerateArray();
                            if (enumerator.MoveNext())
                            {
                                var firstPlate = enumerator.Current;

                                var plateText = firstPlate.TryGetProperty("plate_text", out var p)
                                    ? p.GetString() ?? "N/A"
                                    : "N/A";

                                var vehicleClass = firstPlate.TryGetProperty("vehicle_class", out var c)
                                    ? c.GetString() ?? "unknown"
                                    : "unknown";

                                return (plateText, vehicleClass, true, string.Empty);
                            }
                            else
                            {
                                return ("N/A", "unknown", false, "Không phát hiện biển số");
                            }
                        }

                        return ("N/A", "unknown", false, "Phản hồi API không đúng định dạng");
                    }
                    catch (JsonException ex)
                    {
                        return ("N/A", "unknown", false, $"Lỗi parse JSON: {ex.Message}");
                    }
                }
                else
                {
                    return ("N/A", "unknown", false, $"Lỗi API {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                return ("N/A", "unknown", false, $"Không thể kết nối API: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                return ("N/A", "unknown", false, "API timeout - server phản hồi quá chậm");
            }
            catch (Exception ex)
            {
                return ("N/A", "unknown", false, $"Lỗi không xác định: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}