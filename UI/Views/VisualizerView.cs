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

        // Services
        private VisualizerService _service;
        private TableService _tableService;

        private readonly IUIWebView _webView = WebView();
        private LocalServer _server;
        private string _serverUrl = "http://127.0.0.1:8080/";

        // Table action buttons
        private readonly IUIButton _refreshButton = Button().Text("Refresh").AccentAppearance();

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
            _logger.LogInformation("Initializing VisualizerView");
            _tableService = new TableService(_session.GetDbClient(), _loggerFactory.CreateLogger<TableService>());
            _service = new VisualizerService(_tableService);

            Dictionary<string, Features.SqlVisualizer.Models.TableNode> nodes = await _service.CreateNodes();

            _server.RegisterEndpoint("/api/schema", () => _service.GetSerializedSchema(nodes));

            _server.Start();

            _webView.NavigateToUri(new Uri(_serverUrl));

            _logger.LogInformation("VisualizerView initialized successfully");
        }

        private IUIElement BuildUI()
        {

            return Stack()
                .LargeSpacing()
                .Vertical()
                .WithChildren(
                    _webView
                ).AlignHorizontally(UIHorizontalAlignment.Stretch)
                .AlignVertically(UIVerticalAlignment.Stretch);
        }

        private void OnRefreshClicked()
        {
            if (!_session.IsConnected || _session.GetDbClient() is null)
            {
                return;
            }

            Task.Run(() =>
            {
                RefreshTables(true);
            });
        }

        private void RefreshTables(bool forceRefresh = false)
        {
            if (!_session.IsConnected || _session.GetDbClient() is null)
            {
                return;
            }

        }
    }
}