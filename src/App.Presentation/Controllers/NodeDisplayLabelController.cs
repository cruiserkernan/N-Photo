namespace App.Presentation.Controllers;

public static class NodeDisplayLabelController
{
    public static string GetNodeToolbarLabel(string nodeType)
    {
        if (string.IsNullOrWhiteSpace(nodeType))
        {
            return "?";
        }

        var trimmed = nodeType.Trim();
        var acronym = new List<char>(2);
        var firstIndex = -1;
        for (var index = 0; index < trimmed.Length; index++)
        {
            var current = trimmed[index];
            if (!char.IsLetterOrDigit(current))
            {
                continue;
            }

            if (acronym.Count == 0)
            {
                acronym.Add(char.ToUpperInvariant(current));
                firstIndex = index;
                continue;
            }

            var previous = trimmed[index - 1];
            var isWordBoundary =
                (char.IsUpper(current) && char.IsLower(previous)) ||
                (char.IsDigit(current) && !char.IsDigit(previous));

            if (!isWordBoundary)
            {
                continue;
            }

            acronym.Add(char.ToUpperInvariant(current));
            if (acronym.Count == 2)
            {
                break;
            }
        }

        if (acronym.Count == 2)
        {
            return new string(acronym.ToArray());
        }

        for (var index = firstIndex + 1; index < trimmed.Length; index++)
        {
            var current = trimmed[index];
            if (!char.IsLetterOrDigit(current))
            {
                continue;
            }

            if (acronym.Count == 1)
            {
                acronym.Add(char.ToUpperInvariant(current));
                break;
            }
        }

        return new string(acronym.ToArray());
    }
}
