using ClientXMLApp.Models;
using System.ComponentModel.DataAnnotations;

namespace ClientXMLApp.Services.DTOs
{
    public class AddressDto
    {
        [Required(ErrorMessage = "Address text is required.")]
        [MinLength(5, ErrorMessage = "Address text must be at least 5 characters long.")]
        public string AddressText { get; set; }

        [Required(ErrorMessage = "Address type is required.")]
        public AddressType Type { get; set; }
    }
}
