using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Editor.Application;
using Editor.Domain.Graph;

namespace App.Presentation.Controllers;

public sealed class PropertiesPanelController
{
    private readonly IEditorSession _editorSession;
    private readonly Func<NodeId, string, Task> _executeNodeActionAsync;
    private readonly Func<NodeId, string, string?> _resolveNodeActionDisplayText;
    private readonly Action _refreshGraphBindings;
    private readonly Action<string> _setStatus;

    public PropertiesPanelController(
        IEditorSession editorSession,
        Func<NodeId, string, Task> executeNodeActionAsync,
        Func<NodeId, string, string?> resolveNodeActionDisplayText,
        Action refreshGraphBindings,
        Action<string> setStatus)
    {
        _editorSession = editorSession;
        _executeNodeActionAsync = executeNodeActionAsync;
        _resolveNodeActionDisplayText = resolveNodeActionDisplayText;
        _refreshGraphBindings = refreshGraphBindings;
        _setStatus = setStatus;
    }

    public void Refresh(
        StackPanel propertyEditorHost,
        TextBlock selectedNodeText,
        NodeId? selectedNodeId,
        IReadOnlyDictionary<NodeId, Node> nodeLookup)
    {
        propertyEditorHost.Children.Clear();

        if (selectedNodeId is not NodeId nodeId ||
            !nodeLookup.TryGetValue(nodeId, out var node))
        {
            selectedNodeText.Text = "None";
            propertyEditorHost.Children.Add(
                new TextBlock
                {
                    Classes = { "hint-text" },
                    Text = "Select a node on the graph to edit its properties.",
                    TextWrapping = TextWrapping.Wrap
                });
            return;
        }

        selectedNodeText.Text = $"{node.Type} ({node.Id.ToString()[..8]})";

        var nodeTypeDefinition = _editorSession.GetNodeTypeDefinition(node.Type);

        foreach (var action in nodeTypeDefinition.Actions.OrderBy(definition => definition.Label, StringComparer.Ordinal))
        {
            propertyEditorHost.Children.Add(CreateNodeActionEditor(node.Id, action));
        }

        var definitions = nodeTypeDefinition
            .Parameters
            .Values
            .OrderBy(definition => definition.Name, StringComparer.Ordinal)
            .ToArray();

        foreach (var definition in definitions)
        {
            propertyEditorHost.Children.Add(
                CreateParameterEditor(
                    node.Id,
                    node.Type,
                    definition,
                    node.GetParameter(definition.Name)));
        }

        if (definitions.Length == 0 && nodeTypeDefinition.Actions.Count == 0)
        {
            propertyEditorHost.Children.Add(
                new TextBlock
                {
                    Classes = { "hint-text" },
                    Text = "This node has no editable parameters.",
                    TextWrapping = TextWrapping.Wrap
                });
        }
    }

    private Border CreateNodeActionEditor(NodeId nodeId, NodeActionDefinition action)
    {
        var displayText = _resolveNodeActionDisplayText(nodeId, action.Id);

        var valueText = new TextBlock
        {
            Classes = { "hint-text" },
            TextWrapping = TextWrapping.Wrap,
            Text = string.IsNullOrWhiteSpace(displayText)
                ? action.EmptyDisplayText
                : displayText
        };

        var actionButton = new Button
        {
            Classes = { "action-button", "action-button-primary" },
            Content = action.ButtonText
        };

        actionButton.Click += async (_, _) =>
        {
            try
            {
                await _executeNodeActionAsync(nodeId, action.Id);
            }
            catch (Exception exception)
            {
                _setStatus($"Action failed: {exception.Message}");
            }
        };

        return new Border
        {
            Classes = { "hint-shell" },
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new TextBlock
                    {
                        Classes = { "section-label" },
                        Text = action.Label
                    },
                    valueText,
                    actionButton
                }
            }
        };
    }

    private Control CreateParameterEditor(
        NodeId nodeId,
        string nodeType,
        NodeParameterDefinition definition,
        ParameterValue currentValue)
    {
        return definition.Kind switch
        {
            ParameterValueKind.Boolean => CreateBooleanParameterEditor(nodeId, nodeType, definition, currentValue),
            ParameterValueKind.Enum => CreateEnumParameterEditor(nodeId, nodeType, definition, currentValue),
            ParameterValueKind.Float or ParameterValueKind.Integer =>
                CreateTextParameterEditor(nodeId, nodeType, definition, currentValue),
            _ => new TextBlock
            {
                Classes = { "hint-text" },
                Text = $"Unsupported parameter kind '{definition.Kind}'."
            }
        };
    }

    private Control CreateTextParameterEditor(
        NodeId nodeId,
        string nodeType,
        NodeParameterDefinition definition,
        ParameterValue currentValue)
    {
        var textBox = new TextBox
        {
            Text = NodeParameterEditorController.FormatParameterValue(currentValue),
            Watermark = NodeParameterEditorController.GetParameterHint(definition)
        };

        var applyButton = new Button
        {
            Content = "Apply"
        };

        applyButton.Click += (_, _) =>
        {
            try
            {
                var parsedValue = NodeParameterEditorController.ParseTextParameterValue(definition, textBox.Text ?? string.Empty);
                ApplyParameterUpdate(nodeId, nodeType, definition, parsedValue);
            }
            catch (Exception exception)
            {
                _setStatus($"Set parameter failed: {exception.Message}");
            }
        };

        textBox.KeyDown += (_, keyEventArgs) =>
        {
            if (keyEventArgs.Key != Key.Enter)
            {
                return;
            }

            applyButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            keyEventArgs.Handled = true;
        };

        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 8
        };
        row.Children.Add(textBox);
        row.Children.Add(applyButton);
        Grid.SetColumn(applyButton, 1);

        return new StackPanel
        {
            Spacing = 4,
            Children =
            {
                new TextBlock
                {
                    Classes = { "section-label" },
                    Text = NodeParameterEditorController.GetParameterLabel(definition)
                },
                row
            }
        };
    }

    private Control CreateBooleanParameterEditor(
        NodeId nodeId,
        string nodeType,
        NodeParameterDefinition definition,
        ParameterValue currentValue)
    {
        var checkBox = new CheckBox
        {
            Content = NodeParameterEditorController.GetParameterLabel(definition),
            IsChecked = currentValue.AsBoolean()
        };

        checkBox.IsCheckedChanged += (_, _) =>
            ApplyParameterUpdate(nodeId, nodeType, definition, ParameterValue.Boolean(checkBox.IsChecked ?? false));

        return checkBox;
    }

    private Control CreateEnumParameterEditor(
        NodeId nodeId,
        string nodeType,
        NodeParameterDefinition definition,
        ParameterValue currentValue)
    {
        var comboBox = new ComboBox
        {
            ItemsSource = definition.EnumValues ?? Array.Empty<string>(),
            SelectedItem = currentValue.AsEnum()
        };

        comboBox.SelectionChanged += (_, _) =>
        {
            if (comboBox.SelectedItem is not string selected)
            {
                return;
            }

            ApplyParameterUpdate(nodeId, nodeType, definition, ParameterValue.Enum(selected));
        };

        return new StackPanel
        {
            Spacing = 4,
            Children =
            {
                new TextBlock
                {
                    Classes = { "section-label" },
                    Text = NodeParameterEditorController.GetParameterLabel(definition)
                },
                comboBox
            }
        };
    }

    private void ApplyParameterUpdate(
        NodeId nodeId,
        string nodeType,
        NodeParameterDefinition definition,
        ParameterValue value)
    {
        try
        {
            _editorSession.SetParameter(nodeId, definition.Name, value);
            _refreshGraphBindings();
            _setStatus($"Updated {nodeType}.{definition.Name}");
        }
        catch (Exception exception)
        {
            _setStatus($"Set parameter failed: {exception.Message}");
        }
    }
}
