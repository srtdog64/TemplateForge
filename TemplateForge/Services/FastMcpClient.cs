using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TemplateForge.Services
{
    public sealed class FastMcpClient : IDisposable
    {
        private readonly HttpClient http;
        private readonly string baseUrl;
        private bool disposed;

        public FastMcpClient(string baseUrl)
        {
            this.baseUrl = baseUrl;
            this.http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        }

        public async Task<GenerateProjectResponse> generateProjectAsync(
            string yamlContent, 
            string outputPath, 
            CancellationToken ct = default)
        {
            var request = new GenerateProjectRequest
            {
                yaml_content = yamlContent,
                output_path = outputPath
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await this.http.PostAsync($"{this.baseUrl}/api/generate", content, ct);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<GenerateProjectResponse>(responseJson);
        }

        public async Task<PreviewStructureResponse> previewStructureAsync(
            string yamlContent, 
            CancellationToken ct = default)
        {
            var request = new PreviewStructureRequest
            {
                yaml_content = yamlContent
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await this.http.PostAsync($"{this.baseUrl}/api/preview", content, ct);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<PreviewStructureResponse>(responseJson);
        }

        public async Task<ValidateYamlResponse> validateYamlAsync(
            string yamlContent, 
            CancellationToken ct = default)
        {
            var request = new ValidateYamlRequest
            {
                yaml_content = yamlContent
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await this.http.PostAsync($"{this.baseUrl}/api/validate", content, ct);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ValidateYamlResponse>(responseJson);
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }
            this.disposed = true;
            this.http.Dispose();
        }
    }

    // DTO 클래스들
    public class GenerateProjectRequest
    {
        public string yaml_content { get; set; }
        public string output_path { get; set; }
    }

    public class GenerateProjectResponse
    {
        public bool success { get; set; }
        public string module_name { get; set; }
        public string[] created_folders { get; set; }
        public ProjectStructure structure { get; set; }
    }

    public class PreviewStructureRequest
    {
        public string yaml_content { get; set; }
    }

    public class PreviewStructureResponse
    {
        public bool success { get; set; }
        public ProjectStructure structure { get; set; }
    }

    public class ValidateYamlRequest
    {
        public string yaml_content { get; set; }
    }

    public class ValidateYamlResponse
    {
        public bool valid { get; set; }
        public string[] errors { get; set; }
    }

    public class ProjectStructure
    {
        public string[] folders { get; set; }
        public string[] files { get; set; }
        public string module_name { get; set; }
    }
}
