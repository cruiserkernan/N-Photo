using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using App.Presentation.Controllers;
using Editor.Domain.Graph;

namespace App;

public partial class MainWindow
{
    private bool TryCommitDraggedConnection(Point pointerScreen)
    {
        if (_activeConnectionSource is not PortKey source)
        {
            return false;
        }

        if (!TryResolveConnectionDropTarget(source, pointerScreen, out var target, out _))
        {
            if (_activeConnectionDetachedEdge is Edge detachedToDisconnect)
            {
                try
                {
                    _editorSession.Disconnect(
                        detachedToDisconnect.FromNodeId,
                        detachedToDisconnect.FromPort,
                        detachedToDisconnect.ToNodeId,
                        detachedToDisconnect.ToPort);
                    RefreshGraphBindings();
                    SetStatus($"Disconnected {detachedToDisconnect.FromPort} -> {detachedToDisconnect.ToPort}");
                    return true;
                }
                catch (Exception exception)
                {
                    SetStatus($"Disconnect failed: {exception.Message}");
                    return false;
                }
            }

            SetStatus("Connection canceled.");
            return false;
        }

        var (fromPort, toPort) = ResolveConnectionOrder(source, target);
        var newEdge = new Edge(fromPort.NodeId, fromPort.PortName, toPort.NodeId, toPort.PortName);

        if (_activeConnectionDetachedEdge is Edge detachedEdge &&
            detachedEdge.Equals(newEdge))
        {
            SetStatus("Connection unchanged.");
            return true;
        }

        var detached = false;
        try
        {
            if (_activeConnectionDetachedEdge is Edge edgeToDetach)
            {
                _editorSession.Disconnect(
                    edgeToDetach.FromNodeId,
                    edgeToDetach.FromPort,
                    edgeToDetach.ToNodeId,
                    edgeToDetach.ToPort);
                detached = true;
            }

            _editorSession.Connect(newEdge.FromNodeId, newEdge.FromPort, newEdge.ToNodeId, newEdge.ToPort);
            RefreshGraphBindings();
            SetStatus($"Connected {fromPort.PortName} -> {toPort.PortName}");
            return true;
        }
        catch (Exception exception)
        {
            if (detached && _activeConnectionDetachedEdge is Edge edgeToRestore)
            {
                try
                {
                    _editorSession.Connect(
                        edgeToRestore.FromNodeId,
                        edgeToRestore.FromPort,
                        edgeToRestore.ToNodeId,
                        edgeToRestore.ToPort);
                    RefreshGraphBindings();
                }
                catch
                {
                    // If rollback fails, keep original connect error but refresh view from latest snapshot.
                    RefreshGraphBindings();
                }
            }

            SetStatus($"Connect failed: {exception.Message}");
            return false;
        }
    }

    private void ResetConnectionDragState()
    {
        _isConnectionDragging = false;
        _activeConnectionSource = null;
        _activeConnectionDetachedEdge = null;
        _hoverConnectionTarget = null;
        _hoverConnectionTargetAnchor = null;
        RenderPortVisuals(_edgeSnapshot);
    }

    private void BeginConnectionDrag(PortKey sourcePort, Point pointerScreen, Edge? detachedEdge = null)
    {
        _activeConnectionSource = sourcePort;
        _activeConnectionDetachedEdge = detachedEdge;
        if (_activeConnectionDetachedEdge is null &&
            sourcePort.Direction == PortDirection.Output &&
            TryResolveOutputDetachEdgeForDrag(sourcePort, pointerScreen, out var existingEdge))
        {
            _activeConnectionDetachedEdge = existingEdge;
        }

        _hoverConnectionTarget = null;
        _hoverConnectionTargetAnchor = null;
        _isConnectionDragging = true;
        _activeConnectionPointerWorld = ScreenToWorld(pointerScreen);
    }

    private bool TryFindNearestCompatiblePort(
        PortKey sourcePort,
        Point pointerScreen,
        out PortKey targetPort,
        out Point targetAnchor)
    {
        targetPort = default;
        targetAnchor = default;
        var found = false;
        var nearestDistance = double.MaxValue;
        var pointerWorld = ScreenToWorld(pointerScreen);
        var targetDirection = sourcePort.Direction == PortDirection.Output
            ? PortDirection.Input
            : PortDirection.Output;
        foreach (var node in _nodeLookup.Values)
        {
            var nodeType = _editorSession.GetNodeTypeDefinition(node.Type);
            var candidatePortNames = targetDirection == PortDirection.Input
                ? nodeType.Inputs.Select(port => port.Name)
                : nodeType.Outputs.Select(port => port.Name);

            foreach (var candidatePortName in candidatePortNames)
            {
                var candidate = new PortKey(node.Id, candidatePortName, targetDirection);
                var hasDynamicAnchor = TryResolveNodeBorderAnchor(candidate.NodeId, pointerWorld, out var anchor);
                if (!hasDynamicAnchor && !TryGetPortAnchor(candidate, out anchor))
                {
                    continue;
                }

                var anchorScreen = WorldToScreen(anchor);
                var dx = anchorScreen.X - pointerScreen.X;
                var dy = anchorScreen.Y - pointerScreen.Y;
                var distance = Math.Sqrt((dx * dx) + (dy * dy));
                if (distance > PortSnapDistancePixels || distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = distance;
                targetPort = candidate;
                targetAnchor = anchor;
                found = true;
            }
        }

        return found;
    }

    private bool TryResolveConnectionDropTarget(
        PortKey sourcePort,
        Point pointerScreen,
        out PortKey targetPort,
        out Point targetAnchor)
    {
        if (TryFindNearestCompatiblePort(sourcePort, pointerScreen, out targetPort, out targetAnchor))
        {
            return true;
        }

        return TryFindNodeBodyCompatiblePort(sourcePort, pointerScreen, out targetPort, out targetAnchor);
    }

    private bool TryFindNodeBodyCompatiblePort(
        PortKey sourcePort,
        Point pointerScreen,
        out PortKey targetPort,
        out Point targetAnchor)
    {
        targetPort = default;
        targetAnchor = default;

        var pointerWorld = ScreenToWorld(pointerScreen);
        var worldPadding = NodeBodyDropPaddingPixels / Math.Max(0.001, _zoomScale);
        var nearestDistance = double.MaxValue;
        var found = false;
        foreach (var node in _nodeLookup.Values)
        {
            if (node.Id == sourcePort.NodeId ||
                !TryResolveNodeBodyTargetPort(node, sourcePort, out var candidatePort))
            {
                continue;
            }

            if (!TryGetNodeBodyRect(node.Id, worldPadding, out var nodeRect) ||
                !nodeRect.Contains(pointerWorld))
            {
                continue;
            }

            if (!TryGetNodeCenter(node.Id, out var center))
            {
                continue;
            }

            var dx = center.X - pointerWorld.X;
            var dy = center.Y - pointerWorld.Y;
            var distance = Math.Sqrt((dx * dx) + (dy * dy));
            if (distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = distance;
            found = true;
            targetPort = candidatePort;
            if (!TryResolveNodeBorderAnchor(candidatePort.NodeId, pointerWorld, out targetAnchor) &&
                !TryGetPortAnchor(candidatePort, out targetAnchor))
            {
                targetAnchor = center;
            }
        }

        return found;
    }

    private bool TryResolveNodeBodyTargetPort(Node node, PortKey sourcePort, out PortKey targetPort)
    {
        targetPort = default;
        var nodeType = _editorSession.GetNodeTypeDefinition(node.Type);
        if (sourcePort.Direction == PortDirection.Output)
        {
            var defaultInput = nodeType.Inputs
                                   .FirstOrDefault(input => input.Role != NodePortRole.Mask)
                               ?? nodeType.Inputs.FirstOrDefault();
            if (defaultInput is null)
            {
                return false;
            }

            targetPort = new PortKey(node.Id, defaultInput.Name, PortDirection.Input);
            return true;
        }

        var output = nodeType.Outputs.FirstOrDefault();
        if (output is null)
        {
            return false;
        }

        targetPort = new PortKey(node.Id, output.Name, PortDirection.Output);
        return true;
    }

    private bool TryFindPortLineGrabTarget(Point pointerScreen, out PortLineGrabTarget target)
    {
        target = default;
        var nearestDistance = double.MaxValue;
        var found = false;

        var connectedInputs = new HashSet<PortKey>();
        var connectedOutputs = new HashSet<PortKey>();
        foreach (var edge in _edgeSnapshot)
        {
            var inputKey = new PortKey(edge.ToNodeId, edge.ToPort, PortDirection.Input);
            var outputKey = new PortKey(edge.FromNodeId, edge.FromPort, PortDirection.Output);
            connectedInputs.Add(inputKey);
            connectedOutputs.Add(outputKey);

            if (!TryResolveConnectedLineAnchors(outputKey, inputKey, out var outputAnchor, out var inputAnchor))
            {
                continue;
            }

            var outputAnchorScreen = WorldToScreen(outputAnchor);
            var inputAnchorScreen = WorldToScreen(inputAnchor);
            if (GraphWireGeometryController.TryResolveLineGrabSegment(outputAnchorScreen, inputAnchorScreen, ConnectedLineGrabLength, out var outputGrabStart, out var outputGrabEnd))
            {
                var outputDistance = GraphWireGeometryController.ComputeDistanceToSegment(pointerScreen, outputGrabStart, outputGrabEnd);
                if (outputDistance <= PortLineGrabDistance && outputDistance < nearestDistance)
                {
                    nearestDistance = outputDistance;
                    target = new PortLineGrabTarget(inputKey, edge);
                    found = true;
                }
            }

            if (GraphWireGeometryController.TryResolveLineGrabSegment(inputAnchorScreen, outputAnchorScreen, ConnectedLineGrabLength, out var inputGrabStart, out var inputGrabEnd))
            {
                var inputDistance = GraphWireGeometryController.ComputeDistanceToSegment(pointerScreen, inputGrabStart, inputGrabEnd);
                if (inputDistance <= PortLineGrabDistance && inputDistance < nearestDistance)
                {
                    nearestDistance = inputDistance;
                    target = new PortLineGrabTarget(outputKey, edge);
                    found = true;
                }
            }
        }

        foreach (var (candidatePort, _) in _portHandles)
        {
            var isConnected = candidatePort.Direction == PortDirection.Output
                ? connectedOutputs.Contains(candidatePort)
                : connectedInputs.Contains(candidatePort);
            if (isConnected)
            {
                continue;
            }

            if (!TryGetPortAnchors(candidatePort, out var baseAnchor, out var tipAnchor))
            {
                continue;
            }

            var baseAnchorScreen = WorldToScreen(baseAnchor);
            var tipAnchorScreen = WorldToScreen(tipAnchor);
            if (!GraphWireGeometryController.TryResolveLineGrabSegment(baseAnchorScreen, tipAnchorScreen, PortLineGrabLength, out var grabStart, out var grabEnd))
            {
                continue;
            }

            var distance = GraphWireGeometryController.ComputeDistanceToSegment(pointerScreen, grabStart, grabEnd);
            if (distance <= PortLineGrabDistance && distance < nearestDistance)
            {
                nearestDistance = distance;
                target = new PortLineGrabTarget(candidatePort, null);
                found = true;
            }
        }

        return found;
    }

    private bool TryResolveConnectedLineAnchors(
        PortKey outputPort,
        PortKey inputPort,
        out Point outputAnchor,
        out Point inputAnchor)
    {
        outputAnchor = default;
        inputAnchor = default;
        if (TryGetNodeCenter(outputPort.NodeId, out var sourceCenter) &&
            TryGetNodeCenter(inputPort.NodeId, out var targetCenter) &&
            TryResolveNodeBorderAnchor(outputPort.NodeId, targetCenter, out var sourceBorderAnchor) &&
            TryResolveNodeBorderAnchor(inputPort.NodeId, sourceCenter, out var targetBorderAnchor))
        {
            outputAnchor = sourceBorderAnchor;
            inputAnchor = targetBorderAnchor;
            return true;
        }

        return TryGetPortAnchor(outputPort, out outputAnchor) &&
               TryGetPortAnchor(inputPort, out inputAnchor);
    }

    private static PortLineGrabTarget ResolveLineGrabTargetWithModifiers(
        PortLineGrabTarget target,
        KeyModifiers modifiers)
    {
        if (target.DetachedEdge is not Edge edge ||
            (modifiers & KeyModifiers.Control) == 0)
        {
            return target;
        }

        var invertedSource = target.SourcePort.Direction == PortDirection.Output
            ? new PortKey(edge.ToNodeId, edge.ToPort, PortDirection.Input)
            : new PortKey(edge.FromNodeId, edge.FromPort, PortDirection.Output);
        return new PortLineGrabTarget(invertedSource, edge);
    }

    private bool TryResolveOutputDetachEdgeForDrag(PortKey outputPort, Point pointerScreen, out Edge detachedEdge)
    {
        detachedEdge = default!;
        if (outputPort.Direction != PortDirection.Output)
        {
            return false;
        }

        var candidates = _edgeSnapshot
            .Where(edge => edge.FromNodeId == outputPort.NodeId &&
                           string.Equals(edge.FromPort, outputPort.PortName, StringComparison.Ordinal))
            .ToArray();
        if (candidates.Length == 0)
        {
            return false;
        }

        if (candidates.Length == 1)
        {
            detachedEdge = candidates[0];
            return true;
        }

        var nearestDistance = double.MaxValue;
        Edge? bestEdge = null;
        foreach (var candidate in candidates)
        {
            var inputPort = new PortKey(candidate.ToNodeId, candidate.ToPort, PortDirection.Input);
            if (!TryResolveConnectedLineAnchors(outputPort, inputPort, out var outputAnchor, out var inputAnchor))
            {
                continue;
            }

            var outputAnchorScreen = WorldToScreen(outputAnchor);
            var inputAnchorScreen = WorldToScreen(inputAnchor);
            var distance = GraphWireGeometryController.ComputeDistanceToSegment(pointerScreen, outputAnchorScreen, inputAnchorScreen);
            if (distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = distance;
            bestEdge = candidate;
        }

        detachedEdge = bestEdge ?? candidates[0];
        return true;
    }

    private static (PortKey FromPort, PortKey ToPort) ResolveConnectionOrder(PortKey first, PortKey second)
    {
        return first.Direction == PortDirection.Output
            ? (first, second)
            : (second, first);
    }

    private void UpdateDraggedNodeWireHover(NodeId nodeId)
    {
        if (TryFindBestEdgeForNodeInsert(nodeId, out var edge))
        {
            _hoverNodeInsertEdge = edge;
            return;
        }

        _hoverNodeInsertEdge = null;
    }

    private bool IsPointerInsideNodeBody(NodeId nodeId, Point pointerScreen)
    {
        if (!TryGetNodeBodyRect(nodeId, paddingWorld: 0, out var nodeRect))
        {
            return false;
        }

        return nodeRect.Contains(ScreenToWorld(pointerScreen));
    }

    private bool TryGetNodeBodyRect(NodeId nodeId, double paddingWorld, out Rect rect)
    {
        rect = default;
        if (!_nodePositions.TryGetValue(nodeId, out var position))
        {
            return false;
        }

        var width = GetCardWidth(nodeId);
        var height = GetCardHeight(nodeId);
        if (width <= 0 || height <= 0)
        {
            return false;
        }

        rect = new Rect(position.X - paddingWorld, position.Y - paddingWorld, width + (paddingWorld * 2), height + (paddingWorld * 2));
        return true;
    }

    private bool TryFindNearestEdgeAtPointer(
        Point pointerScreen,
        double maxDistancePixels,
        out Edge hitEdge,
        out double hitDistancePixels)
    {
        hitEdge = default!;
        hitDistancePixels = double.MaxValue;
        var found = false;
        foreach (var edge in _edgeSnapshot)
        {
            var outputPort = new PortKey(edge.FromNodeId, edge.FromPort, PortDirection.Output);
            var inputPort = new PortKey(edge.ToNodeId, edge.ToPort, PortDirection.Input);
            if (!TryResolveConnectedLineAnchors(outputPort, inputPort, out var outputAnchor, out var inputAnchor))
            {
                continue;
            }

            var outputAnchorScreen = WorldToScreen(outputAnchor);
            var inputAnchorScreen = WorldToScreen(inputAnchor);
            var distance = GraphWireGeometryController.ComputeDistanceToSegment(pointerScreen, outputAnchorScreen, inputAnchorScreen);
            if (distance > maxDistancePixels || distance >= hitDistancePixels)
            {
                continue;
            }

            found = true;
            hitDistancePixels = distance;
            hitEdge = edge;
        }

        return found;
    }

    private bool TryFindBestEdgeForNodeInsert(NodeId nodeId, out Edge hitEdge)
    {
        hitEdge = default!;
        if (!TryGetNodeBodyRect(
                nodeId,
                NodeInsertEdgeBoundsTolerancePixels / Math.Max(0.001, _zoomScale),
                out var nodeBounds))
        {
            return false;
        }

        var nodeCenterWorld = new Point(nodeBounds.X + (nodeBounds.Width / 2), nodeBounds.Y + (nodeBounds.Height / 2));
        var nodeCenterScreen = WorldToScreen(nodeCenterWorld);
        var nearestDistance = double.MaxValue;
        var found = false;

        foreach (var edge in _edgeSnapshot)
        {
            if (edge.FromNodeId == nodeId || edge.ToNodeId == nodeId)
            {
                continue;
            }

            var outputPort = new PortKey(edge.FromNodeId, edge.FromPort, PortDirection.Output);
            var inputPort = new PortKey(edge.ToNodeId, edge.ToPort, PortDirection.Input);
            if (!TryResolveConnectedLineAnchors(outputPort, inputPort, out var outputAnchor, out var inputAnchor) ||
                !GraphWireGeometryController.SegmentIntersectsRect(outputAnchor, inputAnchor, nodeBounds))
            {
                continue;
            }

            var outputAnchorScreen = WorldToScreen(outputAnchor);
            var inputAnchorScreen = WorldToScreen(inputAnchor);
            var distance = GraphWireGeometryController.ComputeDistanceToSegment(nodeCenterScreen, outputAnchorScreen, inputAnchorScreen);
            if (distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = distance;
            hitEdge = edge;
            found = true;
        }

        return found;
    }

    private bool TryInsertDraggedNodeIntoWire(NodeId nodeId, out bool attempted)
    {
        attempted = false;
        if (!_nodeLookup.TryGetValue(nodeId, out var node) ||
            !TryGetPrimaryPorts(node.Type, out var primaryInput, out var primaryOutput))
        {
            return false;
        }

        Edge? candidateEdge = _hoverNodeInsertEdge;
        if (candidateEdge is null ||
            candidateEdge.FromNodeId == nodeId ||
            candidateEdge.ToNodeId == nodeId)
        {
            if (!TryFindBestEdgeForNodeInsert(nodeId, out var fallbackEdge))
            {
                return false;
            }

            candidateEdge = fallbackEdge;
        }

        attempted = true;
        if (TrySpliceNodeIntoEdge(
                nodeId,
                primaryInput,
                primaryOutput,
                candidateEdge,
                $"Inserted '{node.Type}' on wire."))
        {
            return true;
        }

        return false;
    }

    private bool TryInsertElbowNodeAtWire(Point pointerScreen)
    {
        if (!TryFindNearestEdgeAtPointer(pointerScreen, NodeInsertEdgeDistancePixels, out var hitEdge, out _))
        {
            return false;
        }

        if (!TryGetPrimaryPorts(NodeTypes.Elbow, out var inputPort, out var outputPort))
        {
            SetStatus("Elbow node has no primary ports.");
            return false;
        }

        var pointerWorld = ScreenToWorld(pointerScreen);
        var elbowPosition = new Point(
            pointerWorld.X - (ElbowNodeDiameter / 2),
            pointerWorld.Y - (ElbowNodeDiameter / 2));
        if (!TryAddNodeOfTypeAtWorldPosition(
                NodeTypes.Elbow,
                elbowPosition,
                out var elbowNodeId,
                out var addError,
                refreshBindings: false))
        {
            SetStatus($"Elbow insert failed: {addError}");
            return false;
        }

        if (TrySpliceNodeIntoEdge(
                elbowNodeId,
                inputPort,
                outputPort,
                hitEdge,
                "Inserted elbow on wire."))
        {
            return true;
        }

        try
        {
            _editorSession.RemoveNode(elbowNodeId, reconnectPrimaryStream: false);
            RefreshGraphBindings();
        }
        catch
        {
            RefreshGraphBindings();
        }

        return false;
    }

    private bool TrySpliceNodeIntoEdge(
        NodeId nodeId,
        string primaryInputPort,
        string primaryOutputPort,
        Edge edgeToReplace,
        string successStatus)
    {
        var existingPrimaryInput = _edgeSnapshot.FirstOrDefault(edge =>
            edge.ToNodeId == nodeId &&
            string.Equals(edge.ToPort, primaryInputPort, StringComparison.Ordinal));

        var disconnectedPrimaryInput = false;
        var disconnectedEdgeToReplace = false;
        var connectedUpstreamToNode = false;
        var connectedNodeToTarget = false;
        try
        {
            _editorSession.Disconnect(
                edgeToReplace.FromNodeId,
                edgeToReplace.FromPort,
                edgeToReplace.ToNodeId,
                edgeToReplace.ToPort);
            disconnectedEdgeToReplace = true;

            if (existingPrimaryInput is Edge inputEdge && !inputEdge.Equals(edgeToReplace))
            {
                _editorSession.Disconnect(
                    inputEdge.FromNodeId,
                    inputEdge.FromPort,
                    inputEdge.ToNodeId,
                    inputEdge.ToPort);
                disconnectedPrimaryInput = true;
            }

            _editorSession.Connect(
                edgeToReplace.FromNodeId,
                edgeToReplace.FromPort,
                nodeId,
                primaryInputPort);
            connectedUpstreamToNode = true;

            _editorSession.Connect(
                nodeId,
                primaryOutputPort,
                edgeToReplace.ToNodeId,
                edgeToReplace.ToPort);
            connectedNodeToTarget = true;

            RefreshGraphBindings();
            SetStatus(successStatus);
            return true;
        }
        catch (Exception exception)
        {
            try
            {
                if (connectedNodeToTarget)
                {
                    _editorSession.Disconnect(nodeId, primaryOutputPort, edgeToReplace.ToNodeId, edgeToReplace.ToPort);
                }

                if (connectedUpstreamToNode)
                {
                    _editorSession.Disconnect(edgeToReplace.FromNodeId, edgeToReplace.FromPort, nodeId, primaryInputPort);
                }

                if (disconnectedPrimaryInput && existingPrimaryInput is Edge inputEdge)
                {
                    _editorSession.Connect(inputEdge.FromNodeId, inputEdge.FromPort, inputEdge.ToNodeId, inputEdge.ToPort);
                }

                if (disconnectedEdgeToReplace)
                {
                    _editorSession.Connect(
                        edgeToReplace.FromNodeId,
                        edgeToReplace.FromPort,
                        edgeToReplace.ToNodeId,
                        edgeToReplace.ToPort);
                }
            }
            catch
            {
                // Best-effort rollback only.
            }

            RefreshGraphBindings();
            SetStatus($"Wire insert failed: {exception.Message}");
            return false;
        }
    }

    private bool TryGetPrimaryPorts(string nodeType, out string inputPort, out string outputPort)
    {
        inputPort = string.Empty;
        outputPort = string.Empty;

        var nodeDefinition = _editorSession.GetNodeTypeDefinition(nodeType);
        var input = nodeDefinition.Inputs.FirstOrDefault();
        var output = nodeDefinition.Outputs.FirstOrDefault();
        if (input is null || output is null)
        {
            return false;
        }

        inputPort = input.Name;
        outputPort = output.Name;
        return true;
    }

    private bool TryGetPortAnchor(PortKey key, out Point anchor)
    {
        anchor = default;

        if (_portHandles.TryGetValue(key, out var visual))
        {
            if (TryResolveVisualAnchorPoint(visual, visual.TipLocalPoint, out anchor))
            {
                return true;
            }
        }

        if (!_nodeLookup.TryGetValue(key.NodeId, out var node) ||
            !_nodePositions.TryGetValue(key.NodeId, out var nodePosition))
        {
            return false;
        }

        var nodeType = _editorSession.GetNodeTypeDefinition(node.Type);
        if (!GraphPortLayoutController.TryResolveAnchorPlan(
                nodeType,
                key.PortName,
                key.Direction,
                out var anchorPlan))
        {
            return false;
        }

        var cardHeight = GetCardHeight(key.NodeId);
        var cardWidth = GetCardWidth(key.NodeId);
        var edgeAnchor = GraphPortLayoutController.ResolveAnchor(
            nodePosition,
            cardWidth,
            cardHeight,
            anchorPlan,
            PortAnchorEdgePadding);
        anchor = edgeAnchor + GraphWireGeometryController.ResolvePortTipOffset(anchorPlan.Side, key.Direction);
        return true;
    }

    private bool TryGetPortAnchors(PortKey key, out Point baseAnchor, out Point tipAnchor)
    {
        baseAnchor = default;
        tipAnchor = default;

        if (_portHandles.TryGetValue(key, out var visual))
        {
            if (TryResolveVisualAnchorPoint(visual, visual.EdgeLocalPoint, out baseAnchor) &&
                TryResolveVisualAnchorPoint(visual, visual.TipLocalPoint, out tipAnchor))
            {
                return true;
            }
        }

        if (!_nodeLookup.TryGetValue(key.NodeId, out var node) ||
            !_nodePositions.TryGetValue(key.NodeId, out var nodePosition))
        {
            return false;
        }

        var nodeType = _editorSession.GetNodeTypeDefinition(node.Type);
        if (!GraphPortLayoutController.TryResolveAnchorPlan(
                nodeType,
                key.PortName,
                key.Direction,
                out var anchorPlan))
        {
            return false;
        }

        var cardHeight = GetCardHeight(key.NodeId);
        var cardWidth = GetCardWidth(key.NodeId);
        baseAnchor = GraphPortLayoutController.ResolveAnchor(
            nodePosition,
            cardWidth,
            cardHeight,
            anchorPlan,
            PortAnchorEdgePadding);
        tipAnchor = baseAnchor + GraphWireGeometryController.ResolvePortTipOffset(anchorPlan.Side, key.Direction);
        return true;
    }

    private bool TryGetNodeCenter(NodeId nodeId, out Point center)
    {
        center = default;
        if (!_nodePositions.TryGetValue(nodeId, out var nodePosition))
        {
            return false;
        }

        center = GraphPortLayoutController.ResolveNodeCenter(
            nodePosition,
            GetCardWidth(nodeId),
            GetCardHeight(nodeId));
        return true;
    }

    private bool TryResolveNodeBorderAnchor(NodeId nodeId, Point towardPoint, out Point borderAnchor)
    {
        borderAnchor = default;
        if (!TryGetNodeCenter(nodeId, out var nodeCenter))
        {
            return false;
        }

        return GraphPortLayoutController.TryResolveBorderIntersection(
            nodeCenter,
            GetCardWidth(nodeId),
            GetCardHeight(nodeId),
            towardPoint,
            out borderAnchor);
    }

    private bool TryResolveVisualAnchorPoint(PortHandleVisual visual, Point localPoint, out Point anchor)
    {
        var width = visual.HitArea.Bounds.Width;
        var height = visual.HitArea.Bounds.Height;
        if (width <= 0 || height <= 0)
        {
            anchor = default;
            return false;
        }

        var translated = visual.HitArea.TranslatePoint(localPoint, _graphLayer);
        if (translated.HasValue)
        {
            anchor = translated.Value;
            return true;
        }

        anchor = default;
        return false;
    }
}
