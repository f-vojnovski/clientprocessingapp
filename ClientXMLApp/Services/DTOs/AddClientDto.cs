using ClientXMLApp.Models;
using System.ComponentModel.DataAnnotations;

namespace ClientXMLApp.Services.DTOs
{
    public class AddClientDto
    {
        [Required(ErrorMessage = "Name is required.")]
        [MinLength(3, ErrorMessage = "Name must be at least 3 characters long.")]
        [MaxLength(Client.NameMaxLength, ErrorMessage = "Name must be at most 200 characters long.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Birthdate is required.")]
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [Required(ErrorMessage = "At least one address is required.")]
        [MinLength(1, ErrorMessage = "At least one address is required.")]
        public List<AddressDto> Addresses { get; set; } = new List<AddressDto>();
    }
}
