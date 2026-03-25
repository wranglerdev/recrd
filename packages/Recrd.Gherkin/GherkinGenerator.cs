using Recrd.Core.Ast;

namespace Recrd.Gherkin;

public sealed class GherkinGenerator
{
    public Task<string> GenerateAsync(Session session, IReadOnlyList<IReadOnlyDictionary<string, string>>? data = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
