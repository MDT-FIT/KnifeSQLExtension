using DevToys.Api;
using KnifeSQLExtension.Core.Services;
using KnifeSQLExtension.Features.RandomDataGeneration.Services;
using KnifeSQLExtension.Features.SqlVisualizer.Models;
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

        private IUIWebView _webView = WebView(Guid.NewGuid().ToString());
        private LocalServer _server;
        private string _serverUrl = "";
        private Dictionary<string, TableNode> _currentNodes = [];

        public VisualizerView(SqlSession session, ILogger<VisualizerView> logger, ILoggerFactory loggerFactory)
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string assemblyDirectory = Path.GetDirectoryName(assemblyPath)!;
            string distPath = Path.Combine(assemblyDirectory, "dist");

            _session = session;
            _logger = logger;
            _loggerFactory = loggerFactory;
            _server = new LocalServer(distPath, logger);
            _serverUrl = $"http://localhost:{_server.GetCurrentPort()}/";
            _webView.SourceChanged += (s, e) => _logger.LogInformation($"WebView navigated to: {_webView.Source}");
        }

        public IUIElement View => BuildUI();

        public async Task Init()
        {
            _logger.LogInformation("Initializing Schema Visualizer");

            _tableService = new TableService(_session.GetDbClient(), _loggerFactory.CreateLogger<TableService>());
            _service = new VisualizerService(_tableService);

            _currentNodes = await _service.CreateNodes();

            _server.RegisterEndpoint("/api/schema", () => _service.GetSerializedSchema(_currentNodes));

            if (!_server.isActive())
            {
                _server.Start();
            }

            _webView.NavigateToUri(new Uri($"{_serverUrl}?update={Guid.NewGuid()}"));

            _logger.LogInformation("Schema Visualizer initialized successfully");
        }

        private IUIElement BuildUI()
        {
            return _webView.AlignVertically(UIVerticalAlignment.Stretch).AlignHorizontally(UIHorizontalAlignment.Stretch);
        }
    }
}