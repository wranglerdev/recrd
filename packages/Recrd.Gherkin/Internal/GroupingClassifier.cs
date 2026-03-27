using Recrd.Core.Ast;

namespace Recrd.Gherkin.Internal;

internal static class GroupingClassifier
{
    internal static IReadOnlyList<(GroupType Group, IStep Step)> Classify(IReadOnlyList<IStep> steps)
    {
        // Find the index of the first Navigate ActionStep
        var firstNavIdx = -1;
        for (var i = 0; i < steps.Count; i++)
        {
            if (steps[i] is ActionStep { ActionType: ActionType.Navigate })
            {
                firstNavIdx = i;
                break;
            }
        }

        var result = new List<(GroupType, IStep)>(steps.Count);
        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            GroupType group;

            if (step is AssertionStep)
            {
                group = GroupType.Then;
            }
            else if (firstNavIdx == -1)
            {
                // No navigate found — default all actions to When
                group = GroupType.When;
            }
            else if (i <= firstNavIdx)
            {
                // Up to and including first navigate => Given
                group = GroupType.Given;
            }
            else
            {
                // After first navigate => When
                group = GroupType.When;
            }

            result.Add((group, step));
        }

        return result.AsReadOnly();
    }
}
