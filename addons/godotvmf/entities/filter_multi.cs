using Godot;

namespace GodotVMF;

[Tool]
public partial class filter_multi : filter_entity
{
    private static readonly string[] FilterFields =
    {
        "Filter01", "Filter02", "Filter03", "Filter04", "Filter05",
    };

    public override bool IsPassed(Node3D node)
    {
        bool isOr = Entity.GetInt("filtertype") == 1;
        int filtersCount = 0;
        int passedCount = 0;

        foreach (var field in FilterFields)
        {
            string targetName = Entity.GetString(field);
            var filter = GetTarget(targetName) as filter_entity;
            if (filter == null) continue;

            filtersCount++;
            if (filter.IsPassed(node) == isOr)
                passedCount++;
        }

        return isOr ? passedCount > 0 : passedCount == filtersCount;
    }
}
