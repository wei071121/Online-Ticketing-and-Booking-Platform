using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace QigloRestaurant.Web.Validation;

[AttributeUsage(AttributeTargets.Property)]
public sealed class AllowedFileExtensionsAttribute : ValidationAttribute
{
    private readonly HashSet<string> _extensions;

    public AllowedFileExtensionsAttribute(params string[] extensions)
    {
        _extensions = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IEnumerable<IFormFile> files)
            return ValidationResult.Success;

        foreach (var file in files.Where(x => x.Length > 0))
        {
            if (!_extensions.Contains(Path.GetExtension(file.FileName)))
                return new ValidationResult($"Only {string.Join(", ", _extensions)} files are allowed.");
        }

        return ValidationResult.Success;
    }
}
