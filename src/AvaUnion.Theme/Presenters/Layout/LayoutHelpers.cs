namespace AvaUnion.Presenters.Layout;

/// <summary>Helper functions for the positioner layout.</summary>
public static class LayoutHelpers
{
    /// <summary>Gets the sum of the sizes, plus one spacing between each pair of sizes that are not zero.</summary>
    public static double SpacedSize(double spacing, params double[] sizes)
    {
        var result = 0.0;
        foreach (var s in sizes)
        {
            result += s;
        }

        if (sizes.Length > 1)
        {
            var totalSpacing = 0.0;
            foreach (var s in sizes)
            {
                totalSpacing += s > 0 ? spacing : 0.0;
            }

            result += totalSpacing > 0 ? totalSpacing - spacing : 0.0;
        }

        return result;
    }
}
