using DevToys.Api;
using KnifeSQLExtension.UI.Views;
using Microsoft.Extensions.Logging;
using System.ComponentModel.Composition;
using static DevToys.Api.GUI;

namespace KnifeSQLExtension.UI;

[Export(typeof(IGuiTool))]
[Name("KnifeSQLExtension")]
[ToolDisplayInformation(
    IconFontName = "FluentSystemIcons",
    IconGlyph = '\uE670',
    GroupName = PredefinedCommonToolGroupNames.Converters,
    ResourceManagerAssemblyIdentifier = nameof(KnifeSqlAssemblyIdentifier),
    ResourceManagerBaseName = "KnifeSQLExtension.KnifeSqlResources",
    ShortDisplayTitleResourceName = nameof(KnifeSqlResources.ShortDisplayTitle),
    LongDisplayTitleResourceName = nameof(KnifeSqlResources.LongDisplayTitle),
    DescriptionResourceName = nameof(KnifeSqlResources.Description),
    AccessibleNameResourceName = nameof(KnifeSqlResources.AccessibleName))]
internal sealed class KnifeSqlGui : IGuiTool
{
    private readonly IUIElement _connectionPanel;
    private readonly IUIElement _generationPanel;
    private readonly IUIElement _visualizerPanel;
    private readonly IUIProgressRing _progressRing = ProgressRing();

    private readonly IUIButton _connectionButton = Button().Text("Connection");
    private readonly IUIButton _generationButton = Button().Text("Generation");
    private readonly IUIButton _viewButton = Button().Text("Schema Visualizer");

    private readonly List<IUIElement> _allPanels;


    public KnifeSqlGui()
    {
        ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        SqlSession session = new SqlSession();

        ConnectionView connectionView = new ConnectionView(session, loggerFactory.CreateLogger<ConnectionView>(), loggerFactory);
        GenerationView generationView = new GenerationView(session, loggerFactory.CreateLogger<GenerationView>(), loggerFactory);
        VisualizerView visualizerView = new VisualizerView(session, loggerFactory.CreateLogger<VisualizerView>(), loggerFactory);

        _connectionPanel = connectionView.View;
        _generationPanel = generationView.View.Hide();
        _visualizerPanel = visualizerView.View.Hide();

        _allPanels = new List<IUIElement> { _connectionPanel, _generationPanel, _visualizerPanel };

        session.ConnectionStateChanged += async (isConnected) =>
        {
            if (isConnected)
            {
                _progressRing.StartIndeterminateProgress();
                _connectionPanel.Disable();
                await generationView.Init();
                await visualizerView.Init();
                _connectionPanel.Enable();
                _progressRing.StopIndeterminateProgress();
                _generationButton.Enable();
                _viewButton.Enable();
                return;
            }

            _generationButton.Disable();
            _viewButton.Disable();
            SwitchToPanel(_connectionPanel);
        };
    }

    private enum GridRows { TopButtons, MainContent }
    private enum GridColumns { Main }

    public UIToolView View => new UIToolView(
        isScrollable: false,
        Grid()
        .Rows(
            (GridRows.TopButtons, Auto),
            (GridRows.MainContent, new UIGridLength(1, UIGridUnitType.Fraction))
        )
        .Columns(
            (new UIGridLength(1, UIGridUnitType.Fraction))
        )
        .Cells(
            Cell(GridRows.TopButtons, GridColumns.Main,
                Stack().Horizontal().WithChildren(
                    _connectionButton.OnClick(() => SwitchToPanel(_connectionPanel)),
                    _generationButton.OnClick(() => SwitchToPanel(_generationPanel)).Disable(),
                    _viewButton.OnClick(() => SwitchToPanel(_visualizerPanel)).Disable(),
                    _progressRing
                )
            ),
            Cell(GridRows.MainContent, GridColumns.Main, _connectionPanel),
            Cell(GridRows.MainContent, GridColumns.Main, _generationPanel),
            Cell(GridRows.MainContent, GridColumns.Main, _visualizerPanel)
        )
    );

    private void SwitchToPanel(IUIElement panelToShow)
    {
        foreach (IUIElement panel in _allPanels)
        {
            if (panel == panelToShow)
                panel.Show();
            else
                panel.Hide();
        }
    }

    public void OnDataReceived(string dataTypeName, object? parsedData)
    {
    }
}