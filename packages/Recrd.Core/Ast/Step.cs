namespace Recrd.Core.Ast;

public abstract record Step(Guid Id);

public sealed record ActionStep(
    Guid Id,
    string ActionType,
    Selector[] Selectors,
    string? Value,
    string? DataVariable
) : Step(Id);

public sealed record AssertionStep(
    Guid Id,
    string AssertionType,
    Selector[] Selectors,
    string? ExpectedValue
) : Step(Id);

public enum GroupType { Given, When, Then }

public sealed record GroupStep(
    Guid Id,
    GroupType GroupType,
    IReadOnlyList<Step> Steps
) : Step(Id);
