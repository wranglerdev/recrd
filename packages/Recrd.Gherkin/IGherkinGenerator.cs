using Recrd.Core.Ast;
using Recrd.Core.Interfaces;

namespace Recrd.Gherkin;

public interface IGherkinGenerator
{
    Task GenerateAsync(
        Session session,
        IDataProvider? dataProvider,
        TextWriter output,
        GherkinGeneratorOptions? options = null,
        CancellationToken ct = default);
}
