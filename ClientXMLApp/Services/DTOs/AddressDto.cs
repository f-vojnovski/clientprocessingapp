using ClientXMLApp.Models;
using System.ComponentModel.DataAnnotations;

namespace ClientXMLApp.Services.DTOs
{
    public class AddressDto
    {
        [Required(ErrorMessage = "Address text is required.")]
        [MinLength(5, ErrorMessage = "Address text must be at least 5 characters long.")]
        [MaxLength(Address.AddressTextMaxLength, ErrorMessage = "Address text must be at most 400 characters long.")]
        public string AddressText { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address type is required.")]
        [EnumDataType(typeof(AddressType), ErrorMessage = "Address type is not a known value.")]
        public AddressType? Type { get; set; }
    }
}
