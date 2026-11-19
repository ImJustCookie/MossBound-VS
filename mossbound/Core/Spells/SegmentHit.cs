using System;

namespace Mossbound.Core;

// Where a straight step first enters a box, as a fraction of the step (0 at its start, 1 at its end), or null when it
// misses. The slab method: the step is inside the box where it is inside all three axis ranges at once.
public static class SegmentHit
{
    public static double? Enters((double X, double Y, double Z) from, (double X, double Y, double Z) to,
        (double X, double Y, double Z) min, (double X, double Y, double Z) max)
    {
        double enter = 0, exit = 1;
        double[] start = { from.X, from.Y, from.Z }, step = { to.X - from.X, to.Y - from.Y, to.Z - from.Z };
        double[] low = { min.X, min.Y, min.Z }, high = { max.X, max.Y, max.Z };
        for (int axis = 0; axis < 3; axis++)
        {
            if (Math.Abs(step[axis]) < 1e-12)
            {
                if (start[axis] < low[axis] || start[axis] > high[axis]) return null;
                continue;
            }
            double a = (low[axis] - start[axis]) / step[axis], b = (high[axis] - start[axis]) / step[axis];
            enter = Math.Max(enter, Math.Min(a, b));
            exit = Math.Min(exit, Math.Max(a, b));
            if (enter > exit) return null;
        }
        return enter;
    }
}
