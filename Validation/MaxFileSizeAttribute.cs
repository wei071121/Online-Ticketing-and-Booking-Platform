using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace QigloRestaurant.Web.Validation;

[AttributeUsage(AttributeTargets.Property)]
public sealed class MaxFileSizeAttribute : ValidationAttribute
{
    private readonly long _maxBytes;

    public MaxFileSizeAttribute(long maxBytes)
    {
        _maxBytes = maxBytes;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IEnumerable<IFormFile> files)
            return ValidationResult.Success;

        var tooLarge = files.FirstOrDefault(file => file.Length > _maxBytes);
        return tooLarge is null
            ? ValidationResult.Success
            : new ValidationResult($"{tooLarge.FileName} is larger than {_maxBytes / 1024 / 1024} MB.");
    }
}
