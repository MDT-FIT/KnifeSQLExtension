using DevToys.Api;
using KnifeSQLExtension.Core;
using KnifeSQLExtension.Core.Services.Database.Interfaces;
using KnifeSQLExtension.UI.Views;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading.Tasks;
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
    private readonly SqlSession _session;
    private readonly ConnectionView _connectionView;

    public KnifeSqlGui()
    {
        _session = new SqlSession();
        _connectionView = new ConnectionView(_session);
    }

    public UIToolView View =>
        new UIToolView(
            Stack()
                .Vertical()
                .WithChildren(
                    _connectionView.View
                )
        );

    public void OnDataReceived(string dataTypeName, object? parsedData) { }
}
