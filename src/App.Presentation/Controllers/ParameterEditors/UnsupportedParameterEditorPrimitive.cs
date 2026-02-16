using Avalonia.Controls;
using Editor.Domain.Graph;

namespace App.Presentation.Controllers;

internal sealed class UnsupportedParameterEditorPrimitive : IParameterEditorPrimitive
{
    public string Id => "unsupported";

    public bool CanCreate(NodeParameterDefinition definition, ParameterValue currentValue)
    {
        return true;
    }

    public Control Create(ParameterEditorPrimitiveContext context)
    {
        return new TextBlock
        {
            Classes = { "hint-text" },
            Text = $"Unsupported parameter kind '{context.Definition.Kind}'."
        };
    }
}
