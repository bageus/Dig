using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

namespace Dig.Domain.Content
{

public sealed class ContentValidationIssue
{
    public ContentValidationIssue(string code, string path, string message)
    {
        if (string.IsNullOrWhiteSpace(code)
            || string.IsNullOrWhiteSpace(path)
            || string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Validation issue fields are required.");
        }

        Code = code.Trim();
        Path = path.Trim();
        Message = message.Trim();
    }

    public string Code { get; }

    public string Path { get; }

    public string Message { get; }

    public override string ToString()
    {
        return $"{Code} at {Path}: {Message}";
    }
}

public sealed class ContentValidationResult
{
    public ContentValidationResult(
        ProductionContentCatalog? catalog,
        IReadOnlyCollection<ContentValidationIssue> issues)
    {
        if (issues is null)
        {
            throw new ArgumentNullException(nameof(issues));
        }

        Catalog = catalog;
        Issues = new ReadOnlyCollection<ContentValidationIssue>(
            issues.OrderBy(issue => issue.Path, StringComparer.Ordinal)
                .ThenBy(issue => issue.Code, StringComparer.Ordinal)
                .ToArray());
    }

    public ProductionContentCatalog? Catalog { get; }

    public IReadOnlyList<ContentValidationIssue> Issues { get; }

    public bool Succeeded => Catalog is not null && Issues.Count == 0;
}
}
