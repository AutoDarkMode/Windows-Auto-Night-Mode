namespace AdaptiveBrightness.Core;

/// <summary>Pure coordinate and ordering rules shared by the draggable curve editor and its checks.</summary>
public static class CurveEditorMath
{
    public const double MaximumLux = 10000;
    public static double AxisMaximumLux(IReadOnlyCollection<CurvePoint> points) => MaximumLux;

    public static IReadOnlyList<double> AxisTicks { get; } = BuildAxisTicks();

    private static double[] BuildAxisTicks()
    {
        var ticks = new List<double> { 0 };
        for (var decade = 1d; decade < MaximumLux; decade *= 10)
            for (var multiple = 1; multiple <= 9; multiple++) ticks.Add(decade * multiple);
        ticks.Add(MaximumLux);
        return ticks.ToArray();
    }

    public static double LuxToNormalizedX(double lux, double axisMaximumLux)
    {
        ValidateAxisMaximum(axisMaximumLux);
        if (!double.IsFinite(lux) || lux < 0) throw new ArgumentOutOfRangeException(nameof(lux));
        return Math.Clamp(Math.Log10(1 + lux) / Math.Log10(1 + axisMaximumLux), 0, 1);
    }

    public static double NormalizedXToLux(double normalizedX, double axisMaximumLux)
    {
        ValidateAxisMaximum(axisMaximumLux);
        if (!double.IsFinite(normalizedX)) throw new ArgumentOutOfRangeException(nameof(normalizedX));
        var clamped = Math.Clamp(normalizedX, 0, 1);
        return Math.Pow(10, clamped * Math.Log10(1 + axisMaximumLux)) - 1;
    }

    /// <summary>Clamp a dragged x coordinate so neighboring handles keep a visible gap.</summary>
    public static double ConstrainNormalizedX(
        IReadOnlyList<CurvePoint> orderedPoints,
        int pointIndex,
        double proposedX,
        double axisMaximumLux,
        double minimumGapX)
    {
        ValidateAxisMaximum(axisMaximumLux);
        if (pointIndex < 0 || pointIndex >= orderedPoints.Count) throw new ArgumentOutOfRangeException(nameof(pointIndex));
        if (!double.IsFinite(proposedX) || !double.IsFinite(minimumGapX) || minimumGapX < 0)
            throw new ArgumentOutOfRangeException(nameof(proposedX));

        var lower = pointIndex == 0 ? 0 : LuxToNormalizedX(orderedPoints[pointIndex - 1].Lux, axisMaximumLux) + minimumGapX;
        var upper = pointIndex == orderedPoints.Count - 1 ? 1 : LuxToNormalizedX(orderedPoints[pointIndex + 1].Lux, axisMaximumLux) - minimumGapX;
        if (lower > upper)
        {
            // Existing points may have been entered close together. Keep their relative order,
            // while still returning a finite coordinate between their neighbors.
            var neighborLower = pointIndex == 0 ? 0 : LuxToNormalizedX(orderedPoints[pointIndex - 1].Lux, axisMaximumLux);
            var neighborUpper = pointIndex == orderedPoints.Count - 1 ? 1 : LuxToNormalizedX(orderedPoints[pointIndex + 1].Lux, axisMaximumLux);
            return (neighborLower + neighborUpper) / 2;
        }

        return Math.Clamp(proposedX, lower, upper);
    }

    public static bool HasVisibleGap(
        IReadOnlyList<CurvePoint> orderedPoints,
        int pointIndex,
        double lux,
        double axisMaximumLux,
        double minimumGapX)
    {
        if (!double.IsFinite(lux) || lux < 0 || pointIndex < 0 || pointIndex >= orderedPoints.Count) return false;
        var candidateX = LuxToNormalizedX(lux, axisMaximumLux);
        return (pointIndex == 0 || candidateX - LuxToNormalizedX(orderedPoints[pointIndex - 1].Lux, axisMaximumLux) >= minimumGapX) &&
               (pointIndex == orderedPoints.Count - 1 || LuxToNormalizedX(orderedPoints[pointIndex + 1].Lux, axisMaximumLux) - candidateX >= minimumGapX);
    }

    public static bool HasStrictLuxOrder(IReadOnlyList<CurvePoint> orderedPoints, int pointIndex, double lux)
    {
        if (!double.IsFinite(lux) || lux < 0 || pointIndex < 0 || pointIndex >= orderedPoints.Count) return false;
        return (pointIndex == 0 || orderedPoints[pointIndex - 1].Lux < lux) &&
               (pointIndex == orderedPoints.Count - 1 || lux < orderedPoints[pointIndex + 1].Lux);
    }

    private static void ValidateAxisMaximum(double axisMaximumLux)
    {
        if (!double.IsFinite(axisMaximumLux) || axisMaximumLux <= 0)
            throw new ArgumentOutOfRangeException(nameof(axisMaximumLux));
    }
}
