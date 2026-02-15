using Avalonia.Controls;
using Dock.Model.Avalonia;
using Dock.Model.Avalonia.Controls;
using Dock.Model.Core;

namespace App.Workspace;

internal sealed class WorkspaceLayoutManager
{
    private readonly Dictionary<WorkspacePanelId, Document> _documents;

    public WorkspaceLayoutManager(Control graphPanel, Control viewerPanel, Control propertiesPanel)
    {
        Factory = new Factory();

        var graphDocument = CreatePanelDocument(WorkspacePanelId.Graph, graphPanel);
        var viewerDocument = CreatePanelDocument(WorkspacePanelId.Viewer, viewerPanel);
        var propertiesDocument = CreatePanelDocument(WorkspacePanelId.Properties, propertiesPanel);

        _documents = new Dictionary<WorkspacePanelId, Document>
        {
            [WorkspacePanelId.Graph] = graphDocument,
            [WorkspacePanelId.Viewer] = viewerDocument,
            [WorkspacePanelId.Properties] = propertiesDocument
        };

        var leftDock = CreateDocumentDock("workspace-graph-dock", graphDocument, 0.33);
        var centerDock = CreateDocumentDock("workspace-viewer-dock", viewerDocument, 0.34);
        var rightDock = CreateDocumentDock("workspace-properties-dock", propertiesDocument, 0.33);

        var mainDock = new ProportionalDock
        {
            Id = "workspace-main-dock",
            Orientation = Orientation.Horizontal,
            VisibleDockables = Factory.CreateList<IDockable>(
                leftDock,
                new ProportionalDockSplitter(),
                centerDock,
                new ProportionalDockSplitter(),
                rightDock),
            ActiveDockable = centerDock,
            DefaultDockable = centerDock
        };

        Layout = new RootDock
        {
            Id = "workspace-root",
            VisibleDockables = Factory.CreateList<IDockable>(mainDock),
            ActiveDockable = mainDock,
            DefaultDockable = mainDock
        };

        Factory.InitLayout(Layout);
    }

    public Factory Factory { get; }

    public RootDock Layout { get; }

    public bool TryDockAsTab(WorkspacePanelId sourceId, WorkspacePanelId targetId)
    {
        if (!TryGetDocuments(sourceId, targetId, out var source, out var target))
        {
            return false;
        }

        if (!MoveDockableToTargetDock(source, target))
        {
            return false;
        }

        Factory.SetActiveDockable(source);
        return true;
    }

    public bool TrySplit(WorkspacePanelId sourceId, WorkspacePanelId targetId, DockOperation operation)
    {
        if (operation is not DockOperation.Left and not DockOperation.Right and not DockOperation.Top and not DockOperation.Bottom)
        {
            return false;
        }

        if (!TryGetDocuments(sourceId, targetId, out var source, out var target))
        {
            return false;
        }

        if (!MoveDockableToTargetDock(source, target))
        {
            return false;
        }

        switch (operation)
        {
            case DockOperation.Left:
                Factory.NewHorizontalDocumentDock(target);
                break;
            case DockOperation.Right:
                Factory.NewHorizontalDocumentDock(source);
                break;
            case DockOperation.Top:
                Factory.NewVerticalDocumentDock(target);
                break;
            case DockOperation.Bottom:
                Factory.NewVerticalDocumentDock(source);
                break;
        }

        Factory.SetActiveDockable(source);
        return true;
    }

    private bool TryGetDocuments(
        WorkspacePanelId sourceId,
        WorkspacePanelId targetId,
        out Document source,
        out Document target)
    {
        source = null!;
        target = null!;

        if (sourceId == targetId)
        {
            return false;
        }

        if (!_documents.TryGetValue(sourceId, out var sourceDocument) ||
            sourceDocument is null ||
            !_documents.TryGetValue(targetId, out var targetDocument) ||
            targetDocument is null)
        {
            return false;
        }

        source = sourceDocument;
        target = targetDocument;
        return true;
    }

    private bool MoveDockableToTargetDock(Document source, Document target)
    {
        if (target.Owner is not IDock targetDock)
        {
            return false;
        }

        if (ReferenceEquals(source.Owner, targetDock))
        {
            Factory.SetActiveDockable(source);
            return true;
        }

        if (source.Owner is IDock sourceDock)
        {
            Factory.RemoveDockable(source, collapse: false);
            if (IsDockEmpty(sourceDock))
            {
                Factory.CollapseDock(sourceDock);
            }
        }

        Factory.AddDockable(targetDock, source);
        Factory.SetActiveDockable(source);
        return true;
    }

    private static bool IsDockEmpty(IDock dock)
    {
        return dock.VisibleDockables is null || dock.VisibleDockables.All(item => item is ProportionalDockSplitter);
    }

    private Document CreatePanelDocument(WorkspacePanelId id, Control panelContent)
    {
        return new Document
        {
            Id = ToDocumentId(id),
            Title = GetPanelTitle(id),
            Content = panelContent,
            CanFloat = false
        };
    }

    private DocumentDock CreateDocumentDock(string id, Document document, double proportion)
    {
        return new DocumentDock
        {
            Id = id,
            Proportion = proportion,
            VisibleDockables = Factory.CreateList<IDockable>(document),
            ActiveDockable = document,
            DefaultDockable = document
        };
    }

    private static string ToDocumentId(WorkspacePanelId id)
    {
        return id switch
        {
            WorkspacePanelId.Graph => "panel-graph",
            WorkspacePanelId.Viewer => "panel-viewer",
            WorkspacePanelId.Properties => "panel-properties",
            _ => "panel-unknown"
        };
    }

    private static string GetPanelTitle(WorkspacePanelId id)
    {
        return id switch
        {
            WorkspacePanelId.Graph => "Node Graph",
            WorkspacePanelId.Viewer => "Viewer",
            WorkspacePanelId.Properties => "Properties",
            _ => "Panel"
        };
    }
}
