using DevToys.Api;
using KnifeSQLExtension.UI.Views;
using System.ComponentModel.Composition;
using static DevToys.Api.GUI;
using Microsoft.Extensions.Logging;

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
    // ADD GENERATION, ANALYTICS AND RELATIONAL DIAGRAMS VIEWS

    // Wrap each view in a named container so we can Show()/Hide() it
    private readonly IUIStack _connectionPanel;
    private readonly IUIStack _generationPanel;
    private readonly IUIStack _visualizerPanel;

    private readonly IUIButton _connectionButton = Button().Text("Connection");
    private readonly IUIButton _generationButton = Button().Text("Generation");


    public KnifeSqlGui()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var session = new SqlSession();
        
        var connectionView = new ConnectionView(session, loggerFactory.CreateLogger<ConnectionView>(), loggerFactory);
        var generationView = new GenerationView(session, loggerFactory.CreateLogger<GenerationView>(), loggerFactory);
        var visualizerView = new VisualizerView(session, loggerFactory.CreateLogger<VisualizerView>(), loggerFactory);
        
        _connectionPanel = Stack().Vertical().WithChildren(connectionView.View);
        _generationPanel = Stack().Vertical().WithChildren(generationView.View).Hide();
        _visualizerPanel = Stack().Vertical().WithChildren(visualizerView.View);
        
        session.ConnectionStateChanged += async (isConnected) =>
        {
            if (isConnected)
            {
                _generationButton.Enable();
                await generationView.Init();
                await visualizerView.Init();
                ShowGeneration();
                _visualizerPanel.Show();
                return;
            }
        
            _generationButton.Disable();
            ShowConnection();
            _visualizerPanel.Hide();
        };
    }

    public UIToolView View =>
        new UIToolView(
            Stack()
                .Vertical()
                .WithChildren(
                    Stack()
                        .Horizontal()
                        .WithChildren(
                            _connectionButton.OnClick(ShowConnection),
                            _generationButton.OnClick(ShowGeneration).Disable()
                        ),
                    _connectionPanel,
                    _generationPanel,
                    _visualizerPanel
                )
        );

    private void ShowConnection()
    {
        _connectionPanel.Show();
        _generationPanel.Hide();
    }
    
    private void ShowGeneration()
    {
        _generationPanel.Show();
        _connectionPanel.Hide();
    }

    public void OnDataReceived(string dataTypeName, object? parsedData)
    {
    }
}