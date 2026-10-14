using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace NavalSault.Utils;

// The curve a shell flies along between two points: a parabola that peaks higher for longer shots.
public static class ShotArc
{
    // Peak height as a share of the shot's length.
    private const float PeakPerLength = 0.25f;

    // t runs from 0 at the muzzle to 1 at impact.
    public static Vector3 PointAt(Vector3 from, Vector3 to, float t)
    {
        float peak = Vector3.Distance(from, to) * PeakPerLength;
        var point = Vector3.Lerp(from, to, t);
        point.Y += 4f * peak * t * (1f - t);
        return point;
    }

    // Points along the part of the arc between tStart and tEnd.
    public static List<Vector3> Points(Vector3 from, Vector3 to, float tStart, float tEnd, int segments)
    {
        var points = new List<Vector3>(segments + 1);
        for (int i = 0; i <= segments; i++)
            points.Add(PointAt(from, to, MathHelper.Lerp(tStart, tEnd, (float)i / segments)));
        return points;
    }
}
