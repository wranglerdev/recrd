using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Recrd.Core.Ast;

namespace Recrd.Compilers.Internal;

internal static class KeywordNameBuilder
{
    private static readonly Dictionary<ActionType, string> ActionVerbs = new()
    {
        [ActionType.Click] = "Clicar Em",
        [ActionType.Type] = "Digitar Em",
        [ActionType.Select] = "Selecionar Em",
        [ActionType.Navigate] = "Navegar Para",
        [ActionType.Upload] = "Enviar Arquivo Em",
        [ActionType.DragDrop] = "Arrastar",
    };

    private static readonly Dictionary<AssertionType, string> AssertionVerbs = new()
    {
        [AssertionType.TextEquals] = "Verificar Texto Igual Em",
        [AssertionType.TextContains] = "Verificar Texto Contem Em",
        [AssertionType.Visible] = "Verificar Visivel Em",
        [AssertionType.Enabled] = "Verificar Habilitado Em",
        [AssertionType.UrlMatches] = "Verificar Url",
    };

    internal static string Build(ActionType actionType, string selectorValue)
    {
        var verb = ActionVerbs[actionType];
        return BuildName(verb, selectorValue);
    }

    internal static string BuildAssertion(AssertionType assertionType, string selectorValue)
    {
        var verb = AssertionVerbs[assertionType];
        return BuildName(verb, selectorValue);
    }

    private static string BuildName(string verb, string selectorValue)
    {
        if (string.IsNullOrEmpty(selectorValue))
            return verb;

        var slug = NormalizeSlug(selectorValue);
        if (string.IsNullOrEmpty(slug))
            return verb;

        var full = $"{verb} {slug}";
        return full.Length > 64 ? full[..64] : full;
    }

    private static string NormalizeSlug(string value)
    {
        // Replace separators with space
        var replaced = value.Replace('-', ' ').Replace('_', ' ').Replace('.', ' ');

        // Remove non-alphanumeric/non-space/non-accented characters
        var cleaned = Regex.Replace(replaced, @"[^\w\s]", " ", RegexOptions.None);

        // Collapse multiple spaces
        var collapsed = Regex.Replace(cleaned, @"\s+", " ").Trim();

        if (string.IsNullOrEmpty(collapsed))
            return string.Empty;

        // Title-case each word
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(collapsed.ToLowerInvariant());
    }
}
