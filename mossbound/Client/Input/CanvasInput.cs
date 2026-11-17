using System;
using Mossbound.Core;

namespace Mossbound.Client;

[Flags]
enum Redraw
{
    None = 0,
    Canvas = 1,
    Panel = 2,
    Both = Canvas | Panel,
}

// What the renderer needs to show the editing in progress. Coordinates are local to the pane.
readonly record struct CanvasView(SlotKey? Selected, SlotKey? LinkingFrom, bool Removing, (double x, double y) Cursor);
readonly record struct PanelView(PatternStroke? Stroke, bool Removing, (double x, double y) Cursor);

// Turns mouse actions on the circle (canvas) and the glyph panel into edits of the draft.
//   Canvas: left click selects a slot; left drag links two glyphs, right drag unlinks them; the wheel
//   over a glyph changes its size; a click on the border cycles the source.
//   Panel: sweeping across points draws lines (right button erases); a click cycles a point's mark
//   (right click clears it).
// Every method returns what must be redrawn; Redraw.None means the action was not for us.
sealed class CanvasInput
{
    readonly CircleDraft draft;
    readonly CanvasLayout layout;

    SlotKey? linkingFrom;
    PatternStroke? stroke;
    int? pressedPoint;
    bool removing;
    (double x, double y) cursor;

    public CanvasInput(CircleDraft draft, CanvasLayout layout)
    {
        this.draft = draft;
        this.layout = layout;
    }

    public SlotKey Selected { get; private set; } = new(CircleRing.Heart, 0);
    public bool IsDrawingOnPanel => stroke != null;
    public bool IsLinkingOnCanvas => linkingFrom != null;
    public CanvasView CanvasView => new(Selected, linkingFrom, removing, cursor);
    public PanelView PanelView => new(stroke, removing, cursor);

    // ---------- canvas ----------

    public Redraw PressCanvas(double x, double y, double size, bool right)
    {
        cursor = (x, y);
        removing = right;
        linkingFrom = layout.SlotAt(x, y, size);
        if (linkingFrom == null && CirclePainter.IsOnBorder(x, y, size)) draft.CycleSource(layout.Tier, backwards: right);
        return Redraw.Canvas;
    }

    public Redraw MoveOnCanvas(double x, double y)
    {
        cursor = (x, y);
        return Redraw.Canvas;
    }

    public Redraw ReleaseCanvas(double x, double y, double size)
    {
        if (linkingFrom is not SlotKey start) return Redraw.None;
        linkingFrom = null;
        if (layout.SlotAt(x, y, size) is not SlotKey end) return Redraw.Canvas;

        if (end == start && !removing) Selected = start;
        else if (removing) draft.Unlink(start, end);
        else draft.Link(start, end, layout.Tier);
        return Redraw.Both;
    }

    public Redraw Scroll(double x, double y, double size, int delta)
    {
        if (layout.SlotAt(x, y, size) is not SlotKey slot || !draft.IsWritten(slot)) return Redraw.None;
        draft.ChangeSize(slot, Math.Sign(delta), layout.Tier);
        return Redraw.Canvas;
    }

    // ---------- panel ----------

    public Redraw PressPanel(double x, double y, double size, bool right)
    {
        cursor = (x, y);
        removing = right;
        stroke = new PatternStroke();
        pressedPoint = CanvasLayout.PointAt(x, y, size);
        if (pressedPoint is int p) stroke.Hover(p);
        return Redraw.Panel;
    }

    public Redraw MoveOnPanel(double x, double y, double size)
    {
        cursor = (x, y);
        if (CanvasLayout.PointAt(x, y, size) is int p) stroke?.Hover(p);
        return Redraw.Panel;
    }

    public Redraw ReleasePanel()
    {
        if (stroke == null) return Redraw.None;
        if (stroke.Points.Count >= 2) ApplyLines(stroke);
        else if (pressedPoint is int p && stroke.Points.Count == 1) ApplyMark(p);
        stroke = null;
        pressedPoint = null;
        return Redraw.Both;
    }

    void ApplyLines(PatternStroke drawn)
    {
        if (removing) draft.EraseLines(Selected, drawn.Segments());
        else draft.DrawLines(Selected, drawn.Segments());
    }

    void ApplyMark(int point)
    {
        if (removing) draft.ClearMark(Selected, point);
        else draft.CycleMark(Selected, point);
    }
}
