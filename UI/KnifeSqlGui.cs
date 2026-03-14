using DevToys.Api;
using KnifeSQLExtension.UI.Views;
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
    private readonly IView _connectionView;
    private readonly IView _generationView;

    // ADD GENERATION, ANALYTICS AND RELATIONAL DIAGRAMS VIEWS

    // Wrap each view in a named container so we can Show()/Hide() it
    private readonly IUIStack _connectionPanel;
    private readonly IUIStack _generationPanel;

    public KnifeSqlGui()
    {
        var session = new SqlSession();
        _connectionView = new ConnectionView(session);
        _generationView = new GenerationView(session);

        _connectionPanel = Stack().Vertical().WithChildren(_connectionView.View);
        _generationPanel = Stack().Vertical().WithChildren(_generationView.View).Hide();
        // _analyticsPanel  = Stack().Vertical().WithChildren(_analyticsView.View).Hide();
    }

    public UIToolView View =>
        new UIToolView(
            Stack()
                .Vertical()
                .WithChildren(
                    // Navigation bar
                    Stack()
                        .Horizontal()
                        .WithChildren(
                            Button().Text("Connection").OnClick(ShowConnection),
                            Button().Text("Generation").OnClick(ShowGeneration)
                        ),
                    // View panels
                    _connectionPanel,
                    _generationPanel
                // _analyticsPanel
                )
        );

    private void ShowConnection()
    {
        _connectionPanel.Show();
        _generationPanel.Hide();
        // _analyticsPanel.Hide();
    }

    private void ShowGeneration()
    {
        _generationPanel.Show();
        _connectionPanel.Hide();
    }

    // private void ShowAnalytics()
    // {
    //     _connectionPanel.Hide();
    //     _analyticsPanel.Show();
    // }

    public void OnDataReceived(string dataTypeName, object? parsedData) { }
}
