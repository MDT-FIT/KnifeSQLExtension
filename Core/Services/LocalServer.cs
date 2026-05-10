using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;

namespace KnifeSQLExtension.Core.Services
{
    internal class LocalServer
    {
        private HttpListener? _listener;
        private readonly string? _baseFolder;
        private readonly int _port;
        private readonly ILogger _logger;
        private readonly Dictionary<string, Func<string>> _apiRoutes = new();

        public LocalServer(string distFolder, ILogger logger, int port = 8080)
        {
            _baseFolder = distFolder;
            _port = port;
            _logger = logger;
        }

        public void RegisterEndpoint(string path, Func<string> action)
        {
            _apiRoutes[path.ToLower()] = action;
        }

        public void Start()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{_port}/");
            _listener.Start();

            Task.Run(async () =>
            {
                while (_listener.IsListening)
                {
                    HttpListenerContext context = await _listener.GetContextAsync();
                    ProcessRequest(context);
                }
            });
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            string path = context.Request.Url.AbsolutePath.ToLower();

            if (_apiRoutes.TryGetValue(path, out Func<string>? handler))
            {
                SendResponse(context, handler(), "application/json");
            }
            else
            {
                ServeFile(context, path);
            }
        }

        private void ServeFile(HttpListenerContext context, string path)
        {
            string relativePath = path == "/" ? "index.html" : path.TrimStart('/');
            relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);

            string fullPath = Path.Combine(_baseFolder, relativePath);

            if (File.Exists(fullPath))
            {
                byte[] content = File.ReadAllBytes(fullPath);
                context.Response.ContentType = GetMimeType(fullPath);
                context.Response.StatusCode = 200;
                context.Response.OutputStream.Write(content, 0, content.Length);
            }
            else
            {
                context.Response.StatusCode = 404;
                _logger.LogWarning("File not found: {Path}", fullPath);
            }
            context.Response.OutputStream.Close();
        }

        private static void SendResponse(HttpListenerContext context, string content, string contentType)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            context.Response.ContentType = contentType;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }

        private static string GetMimeType(string path)
        {
            return Path.GetExtension(path) switch
            {
                ".html" => "text/html",
                ".js" => "application/javascript",
                ".css" => "text/css",
                _ => "application/octet-stream"
            };
        }
    }
}
