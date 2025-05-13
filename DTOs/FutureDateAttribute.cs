using System.ComponentModel.DataAnnotations;

namespace geotagger_backend.DTOs
{
    public class FutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            return value is DateTime dt && dt > DateTime.UtcNow;
        }
    }
}
