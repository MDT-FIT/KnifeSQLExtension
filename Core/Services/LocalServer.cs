using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace KnifeSQLExtension.Core.Services
{
    internal class LocalServer
    {
        private HttpListener _listener;
        private readonly string? _baseFolder;
        private readonly int _port;
        private readonly ILogger _logger;
        private readonly Dictionary<string, Func<string>> _apiRoutes = [];

        public LocalServer(string distFolder, ILogger logger)
        {
            _baseFolder = distFolder;
            _logger = logger;
            _port = GetAvaiablePort();
            _listener = new HttpListener();
        }

        public void RegisterEndpoint(string path, Func<string> action)
        {
            _apiRoutes[path.ToLower()] = action;
        }
        public int GetCurrentPort()
        {
            return _port;
        }

        public bool isActive()
        {
            return _listener.IsListening;
        }

        public void Start()
        {
            if (_listener.IsListening)
            {
                return;
            }

            _listener.Prefixes.Add($"http://127.0.0.1:{_port}/");
            _listener.Prefixes.Add($"http://localhost:{_port}/");
            _listener.Prefixes.Add($"http://[::1]:{_port}/");
            _listener.Start();

            Task.Run(async () =>
            {
                while (_listener.IsListening)
                {
                    try
                    {
                        HttpListenerContext context = await _listener.GetContextAsync();
                        _ = Task.Run(() => ProcessRequest(context));
                    }
                    catch (HttpListenerException ex)
                    {
                        _logger.LogError("HttpListener stopped unexpectedly.", ex);
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }

                }
            });
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            try
            {
                if (HandleOptions(context)) return;

                string path = context.Request.Url?.AbsolutePath.ToLower() ?? "/";



                if (_apiRoutes.TryGetValue(path, out Func<string>? handler))
                {
                    //if (path.Contains("events"))
                    //{
                    //    StartEvent(context);
                    //}

                    SendResponse(context, handler(), "application/json");
                }
                else
                {
                    ServeFile(context, path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing request for {context.Request.Url}", ex);

                try
                {
                    context.Response.StatusCode = 500;
                    context.Response.Close();
                }
                catch { /* response may already be closed */ }
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
                byte[] body = Encoding.UTF8.GetBytes($"Not found: {relativePath}");
                context.Response.OutputStream.Write(body, 0, body.Length);
                _logger.LogWarning("File not found: {Path}", fullPath);
            }

            context.Response.Close();
        }

        private static void SendResponse(HttpListenerContext context, string content, string contentType)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            context.Response.ContentType = contentType;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }

        //private static void StartEvent(HttpListenerContext context)
        //{
        //    byte[] buffer = Encoding.UTF8.GetBytes(content);
        //    context.Response.ContentType = "text/event-stream";
        //    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        //    context.Response.OutputStream.Close();
        //}

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
        private static int GetAvaiablePort()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            return port;
        }

        private static bool HandleOptions(HttpListenerContext context)
        {
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            context.Response.Headers.Add("Cache-Control", "no-cache");
            context.Response.Headers.Add("Connection", "keep-alive");
            context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            if (context.Request.HttpMethod == "OPTIONS")
            {
                context.Response.StatusCode = 204;
                context.Response.OutputStream.Close();
                return true;
            }

            return false;
        }
    }
}
