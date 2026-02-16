using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using App.Presentation.Controllers;
using Editor.Domain.Graph;

namespace App;

public partial class MainWindow
{
    private Border CreateNodeCard(Node node)
    {
        var nodeType = _editorSession.GetNodeTypeDefinition(node.Type);
        if (string.Equals(node.Type, NodeTypes.Elbow, StringComparison.Ordinal))
        {
            return CreateElbowNodeCard(node, nodeType);
        }

        var standardInputs = nodeType.Inputs.Where(input => !GraphPortLayoutController.IsMaskPort(input)).ToArray();
        var maskInputs = nodeType.Inputs.Where(GraphPortLayoutController.IsMaskPort).ToArray();

        var title = new TextBlock
        {
            Text = node.Type,
            FontWeight = FontWeight.SemiBold,
            Foreground = _nodeCardTitleForeground,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };
        var detail = new TextBlock
        {
            Text = node.Id.ToString()[..8],
            Foreground = _nodeCardDetailForeground,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        var details = new StackPanel
        {
            Spacing = 3,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        details.Children.Add(title);
        details.Children.Add(detail);

        var inputPorts = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Margin = new Thickness(14, -InputPortOutsideOffset, 14, 0)
        };
        foreach (var input in standardInputs)
        {
            inputPorts.Children.Add(CreatePortHandle(node.Id, input.Name, PortDirection.Input, input.Role));
        }

        var outputPorts = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom,
            Margin = new Thickness(14, 0, 14, -OutputPortOutsideOffset)
        };
        foreach (var output in nodeType.Outputs)
        {
            outputPorts.Children.Add(CreatePortHandle(node.Id, output.Name, PortDirection.Output, output.Role));
        }

        var maskPorts = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Vertical,
            Spacing = 8,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Margin = new Thickness(0, 14, -InputPortOutsideOffset, 14)
        };
        foreach (var maskInput in maskInputs)
        {
            maskPorts.Children.Add(CreatePortHandle(node.Id, maskInput.Name, PortDirection.Input, maskInput.Role));
        }

        var content = new Grid();
        content.Children.Add(details);
        if (inputPorts.Children.Count > 0)
        {
            content.Children.Add(inputPorts);
        }

        if (outputPorts.Children.Count > 0)
        {
            content.Children.Add(outputPorts);
        }

        if (maskPorts.Children.Count > 0)
        {
            content.Children.Add(maskPorts);
        }

        var card = new Border
        {
            Tag = node.Id,
            Width = NodeCardWidth,
            Height = NodeCardHeight,
            Padding = new Thickness(10, 9),
            Background = _nodeCardBackground,
            BorderBrush = _nodeCardBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            ClipToBounds = false,
            Cursor = new Cursor(StandardCursorType.SizeAll),
            Child = content
        };
        card.Classes.Add("node-card");

        card.PointerPressed += OnNodeCardPointerPressed;
        card.PointerMoved += OnNodeCardPointerMoved;
        card.PointerReleased += OnNodeCardPointerReleased;
        card.PointerCaptureLost += OnNodeCardPointerCaptureLost;
        return card;
    }

    private Border CreateElbowNodeCard(Node node, NodeTypeDefinition nodeType)
    {
        var host = new Grid();
        var hub = new Border
        {
            Width = ElbowNodeDiameter,
            Height = ElbowNodeDiameter,
            CornerRadius = new CornerRadius(ElbowNodeDiameter / 2),
            Background = ResolveBrush("Brush.NodeCard.Elbow.Background", "#B9894A"),
            BorderBrush = ResolveBrush("Brush.NodeCard.Elbow.Border", "#E0BE88"),
            BorderThickness = new Thickness(1),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        host.Children.Add(hub);

        var input = nodeType.Inputs.FirstOrDefault();
        if (input is not null)
        {
            var inputPorts = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                Margin = new Thickness(0, -InputPortOutsideOffset, 0, 0)
            };
            inputPorts.Children.Add(CreatePortHandle(node.Id, input.Name, PortDirection.Input, input.Role));
            host.Children.Add(inputPorts);
        }

        var output = nodeType.Outputs.FirstOrDefault();
        if (output is not null)
        {
            var outputPorts = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, -OutputPortOutsideOffset)
            };
            outputPorts.Children.Add(CreatePortHandle(node.Id, output.Name, PortDirection.Output, output.Role));
            host.Children.Add(outputPorts);
        }

        var card = new Border
        {
            Tag = node.Id,
            Width = ElbowNodeDiameter,
            Height = ElbowNodeDiameter,
            Background = Brushes.Transparent,
            BorderBrush = _nodeCardBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(ElbowNodeDiameter / 2),
            ClipToBounds = false,
            Cursor = new Cursor(StandardCursorType.SizeAll),
            Child = host
        };
        card.Classes.Add("node-card");
        card.Classes.Add("node-card-elbow");

        card.PointerPressed += OnNodeCardPointerPressed;
        card.PointerMoved += OnNodeCardPointerMoved;
        card.PointerReleased += OnNodeCardPointerReleased;
        card.PointerCaptureLost += OnNodeCardPointerCaptureLost;
        return card;
    }

    private Border CreatePortHandle(NodeId nodeId, string portName, PortDirection direction, NodePortRole role)
    {
        var key = new PortKey(nodeId, portName, direction);
        var side = ResolvePortSideForHandle(nodeId, portName, direction);
        var glyphGeometry = GraphWireGeometryController.ResolvePortGlyphGeometry(side, direction);
        var hitWidth = Math.Max(PortHandleHitSize, glyphGeometry.Width + (PortGlyphPadding * 2));
        var hitHeight = Math.Max(PortHandleHitSize, glyphGeometry.Height + (PortGlyphPadding * 2));
        var glyphOffsetX = (hitWidth - glyphGeometry.Width) / 2;
        var glyphOffsetY = (hitHeight - glyphGeometry.Height) / 2;
        var tipLocalPoint = new Point(
            glyphOffsetX + glyphGeometry.TipPoint.X,
            glyphOffsetY + glyphGeometry.TipPoint.Y);
        var edgeLocalPoint = new Point(
            glyphOffsetX + glyphGeometry.EdgePoint.X,
            glyphOffsetY + glyphGeometry.EdgePoint.Y);

        var handle = new Border
        {
            Tag = key,
            Width = hitWidth,
            Height = hitHeight,
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Hand),
            ZIndex = 20
        };
        ToolTip.SetTip(handle, role == NodePortRole.Mask ? $"Mask: {portName}" : $"{direction}: {portName}");
        handle.PointerPressed += OnPortHandlePressed;

        _portHandles[key] = new PortHandleVisual(handle, role, tipLocalPoint, edgeLocalPoint);
        return handle;
    }

    private void CompleteNodeCardDrag(Border card, NodeId nodeId)
    {
        card.BorderBrush = _selectedNodeId == nodeId ? _nodeCardSelectedBorder : _nodeCardBorder;
        _activeDragNodeId = null;
        OnPersistentStateMutated();

        if (_nodeLookup.TryGetValue(nodeId, out var node))
        {
            SetStatus($"Moved node '{node.Type}'.");
            return;
        }

        SetStatus("Moved node.");
    }

    private IBrush ResolvePortHandleStroke(PortDirection direction, NodePortRole role, bool connected)
    {
        if (direction == PortDirection.Output)
        {
            return connected ? _portOutputConnected : _portOutputUnconnected;
        }

        if (role == NodePortRole.Mask)
        {
            return connected ? _portMaskConnected : _portMaskUnconnected;
        }

        return connected ? _portInputConnected : _portInputUnconnected;
    }

    private GraphPortSide ResolvePortSideForHandle(NodeId nodeId, string portName, PortDirection direction)
    {
        if (_nodeLookup.TryGetValue(nodeId, out var node))
        {
            var nodeType = _editorSession.GetNodeTypeDefinition(node.Type);
            if (GraphPortLayoutController.TryResolveAnchorPlan(
                    nodeType,
                    portName,
                    direction,
                    out var plan))
            {
                return plan.Side;
            }
        }

        return direction == PortDirection.Output
            ? GraphPortSide.Bottom
            : GraphPortSide.Top;
    }

    private double GetCardHeight(NodeId nodeId)
    {
        if (!_nodeCards.TryGetValue(nodeId, out var card))
        {
            return NodeCardHeight;
        }

        return card.Bounds.Height > 0 ? card.Bounds.Height : NodeCardHeight;
    }

    private double GetCardWidth(NodeId nodeId)
    {
        if (!_nodeCards.TryGetValue(nodeId, out var card))
        {
            return NodeCardWidth;
        }

        return card.Bounds.Width > 0 ? card.Bounds.Width : NodeCardWidth;
    }
}
