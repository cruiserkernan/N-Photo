using Editor.Domain.Graph;

namespace Editor.Domain.Tests;

public class DomainAssemblyBoundaryTests
{
    [Fact]
    public void DomainAssembly_DoesNotReferenceAvaloniaOrSkia()
    {
        var references = typeof(NodeGraph)
            .Assembly
            .GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(references, name => name.StartsWith("Avalonia", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(references, name => name.StartsWith("Skia", StringComparison.OrdinalIgnoreCase));
    }
}
