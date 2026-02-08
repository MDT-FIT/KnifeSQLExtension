using DevToys.Api;
using System.ComponentModel.Composition;

namespace KnifeSQLExtension
{
  
    [Export(typeof(IResourceAssemblyIdentifier))]
    [Name(nameof(KnifeSqlAssemblyIdentifier))]
    internal sealed class KnifeSqlAssemblyIdentifier : IResourceAssemblyIdentifier
    {
        public ValueTask<FontDefinition[]> GetFontDefinitionsAsync()
        {
            return new ValueTask<FontDefinition[]>(Array.Empty<FontDefinition>());
        }
    }
}
