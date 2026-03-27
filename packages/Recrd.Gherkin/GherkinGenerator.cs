using Recrd.Core.Ast;
using Recrd.Core.Interfaces;

namespace Recrd.Gherkin;

public sealed class GherkinGenerator : IGherkinGenerator
{
    public Task GenerateAsync(
        Session session,
        IDataProvider? dataProvider,
        TextWriter output,
        GherkinGeneratorOptions? options = null,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
