using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DentalManagement.Services
{
    public interface IS3UploadService
    {
        Task<bool> UploadToS3Async(object data);
    }

    public class S3UploadService : IS3UploadService
    {
        private readonly HttpClient _httpClient;

        public S3UploadService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> UploadToS3Async(object data)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://gtayzldnkc.execute-api.us-east-1.amazonaws.com/prod_/UploadToS3", content);

            return response.IsSuccessStatusCode;
        }
    }
} 