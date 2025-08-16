using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TemplateForge.Services
{
    public sealed class FastMcpProcessManager : IDisposable
    {
        private Process process;
        private readonly int port;
        private readonly string exePath;
        private readonly HttpClient http;
        private bool disposed;

        public FastMcpProcessManager(int port)
        {
            this.port = port;
            this.exePath = resolveExePath();
            this.http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        }

        public bool getIsRunning()
        {
            if (this.process == null)
            {
                return false;
            }
            if (this.process.HasExited)
            {
                return false;
            }
            return true;
        }

        public int getPort()
        {
            return this.port;
        }

        public string getBaseUrl()
        {
            return $"http://127.0.0.1:{this.port}";
        }

        public async Task<bool> startAsync(CancellationToken ct)
        {
            if (this.getIsRunning())
            {
                return true;
            }

            if (!File.Exists(this.exePath))
            {
                throw new FileNotFoundException("fastmcp.exe not found", this.exePath);
            }

            var psi = new ProcessStartInfo
            {
                FileName = this.exePath,
                Arguments = $"--port {this.port} --host 127.0.0.1",
                WorkingDirectory = Path.GetDirectoryName(this.exePath),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            this.process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            this.process.OutputDataReceived += (_, e) => 
            { 
                if (!string.IsNullOrWhiteSpace(e.Data)) 
                { 
                    Debug.WriteLine("[FastMCP] " + e.Data); 
                } 
            };
            this.process.ErrorDataReceived += (_, e) => 
            { 
                if (!string.IsNullOrWhiteSpace(e.Data)) 
                { 
                    Debug.WriteLine("[FastMCP:ERR] " + e.Data); 
                } 
            };

            if (!this.process.Start())
            {
                return false;
            }
            
            this.process.BeginOutputReadLine();
            this.process.BeginErrorReadLine();

            // 헬스체크: 최대 15초 대기
            DateTime due = DateTime.UtcNow.AddSeconds(15);
            while (DateTime.UtcNow < due && !ct.IsCancellationRequested)
            {
                try
                {
                    if (await isHealthyAsync(ct))
                    {
                        Debug.WriteLine($"[FastMCP] Server started successfully on port {this.port}");
                        return true;
                    }
                }
                catch
                {
                    // ignore and retry
                }
                await Task.Delay(500, ct);
            }

            // 실패 시 정리
            this.tryKill();
            return false;
        }

        public async Task<bool> isHealthyAsync(CancellationToken ct)
        {
            try
            {
                string url = $"{this.getBaseUrl()}/health";
                var response = await this.http.GetAsync(url, ct);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public void stop()
        {
            this.tryKill();
        }

        private void tryKill()
        {
            try
            {
                if (this.process != null && !this.process.HasExited)
                {
                    this.process.Kill();
                    this.process.WaitForExit(3000);
                }
            }
            catch
            {
                // ignore
            }
        }

        private static string resolveExePath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string path = Path.Combine(baseDir, "Runtime", "fastmcp", "fastmcp.exe");
            return path;
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }
            this.disposed = true;
            this.http.Dispose();
            this.stop();
            this.process?.Dispose();
        }
    }
}
