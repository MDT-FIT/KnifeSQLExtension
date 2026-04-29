using DevToys.Api;
using KnifeSQLExtension.Features.RandomDataGeneration.Services;
using KnifeSQLExtension.Features.SqlVisualizer.Services;
using static DevToys.Api.GUI;
using Microsoft.Extensions.Logging;

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

        // Table action buttons
        private readonly IUIButton _refreshButton = Button().Text("Refresh").AccentAppearance();

        public VisualizerView(SqlSession session, ILogger<VisualizerView> logger, ILoggerFactory loggerFactory)
        {
            _session = session;
            _logger = logger;
            _loggerFactory = loggerFactory;

            string assemblyDirectory = Path.GetDirectoryName(typeof(VisualizerView).Assembly.Location)!;

            // Construct the path to the React dist folder (cross-platform)
            string distPath = Path.Combine(assemblyDirectory, "dist");
            string htmlPath = Path.Combine(distPath, "index.html");

            if (File.Exists(htmlPath))
            {
                // Convert to proper file:// URI (works on Windows, macOS, Linux)
                Uri fileUri = new Uri(htmlPath, UriKind.Absolute);
                _logger.LogInformation("Loading React app from: {Path}", fileUri);
                _webView.NavigateToUri(fileUri);
            }
            else
            {
                _logger.LogError("Could not find index.html at {Path}", htmlPath);
            }
        }
        
        public IUIElement View => BuildUI();


        public async Task Init()
        {
            _logger.LogInformation("Initializing VisualizerView");
            _tableService = new TableService(_session.GetDbClient(), _loggerFactory.CreateLogger<TableService>());
            _service = new VisualizerService(_tableService);
            
            // var nodes = await _service.CreateNodes();
            // var html = _service.GenerateSvgNodes(nodes);

            // Get the directory where the assembly is loaded
            // string assemblyDirectory = Path.GetDirectoryName(typeof(VisualizerView).Assembly.Location)!;

            // Construct the path to the React dist folder (cross-platform)
            // string distPath = Path.Combine(assemblyDirectory, "dist");
            // string htmlPath = Path.Combine(distPath, "index.html");

            // if (File.Exists(htmlPath))
            // {
            //     // Convert to proper file:// URI (works on Windows, macOS, Linux)
            //     Uri fileUri = new Uri(htmlPath, UriKind.Absolute);
            //     _logger.LogInformation("Loading React app from: {Path}", fileUri);
            //     _webView.NavigateToUri(fileUri);
            // }
            // else
            // {
            //     _logger.LogError("Could not find index.html at {Path}", htmlPath);
            // }

            // await RefreshTables();
            _logger.LogInformation("VisualizerView initialized successfully");
        }



        private IUIElement BuildUI()
        {

            return Stack()
                .LargeSpacing()
                .Vertical()
                .WithChildren(
                    _webView
                );
        }

        private void OnRefreshClicked()
        {
            if (!_session.IsConnected || _session.GetDbClient() is null)
            {
                return;
            }

            Task.Run(async () =>
            {
                await RefreshTables(true);
            });
        }

        private async Task RefreshTables(bool forceRefresh = false)
        {
            if (!_session.IsConnected || _session.GetDbClient() is null)
            {
                return;
            }

        }
    }
}