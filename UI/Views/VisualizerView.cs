using DevToys.Api;
using KnifeSQLExtension.Core.Services;
using KnifeSQLExtension.Features.RandomDataGeneration.Services;
using KnifeSQLExtension.Features.SqlVisualizer.Services;
using Microsoft.Extensions.Logging;
using System.Reflection;
using static DevToys.Api.GUI;

namespace KnifeSQLExtension.UI.Views
{
    public class VisualizerView : IView
    {
        private readonly SqlSession _session;
        private readonly ILogger<VisualizerView> _logger;
        private readonly ILoggerFactory _loggerFactory;

        private VisualizerService _service;
        private TableService _tableService;

        private readonly IUIWebView _webView = WebView();
        private LocalServer _server;
        // For simplicity, we're hardcoding the server URL and port. In a production scenario, consider making this configurable or using dynamic port assignment.
        private string _serverUrl = "http://127.0.0.1:8080/";

        public VisualizerView(SqlSession session, ILogger<VisualizerView> logger, ILoggerFactory loggerFactory)
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string assemblyDirectory = Path.GetDirectoryName(assemblyPath)!;
            string distPath = Path.Combine(assemblyDirectory, "dist");

            _session = session;
            _logger = logger;
            _loggerFactory = loggerFactory;
            _server = new LocalServer(distPath, logger);
        }

        public IUIElement View => BuildUI();

        public async Task Init()
        {
            _logger.LogInformation("Initializing Schema Visualizer");

            _tableService = new TableService(_session.GetDbClient(), _loggerFactory.CreateLogger<TableService>());
            _service = new VisualizerService(_tableService);

            Dictionary<string, Features.SqlVisualizer.Models.TableNode> nodes = await _service.CreateNodes();

            _server.RegisterEndpoint("/api/schema", () => _service.GetSerializedSchema(nodes));

            _server.Start();

            _webView.NavigateToUri(new Uri(_serverUrl));

            _logger.LogInformation("Schema Visualizer initialized successfully");
        }

        private IUIElement BuildUI()
        {
            return _webView.AlignVertically(UIVerticalAlignment.Stretch).AlignHorizontally(UIHorizontalAlignment.Stretch);
        }
    }
}