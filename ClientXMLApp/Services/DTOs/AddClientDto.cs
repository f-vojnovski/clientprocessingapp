﻿using System.ComponentModel.DataAnnotations;

namespace ClientXMLApp.Services.DTOs
{
    public class AddClientDto
    {
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Birthdate is required.")]
        [DataType(DataType.Date)]
        public DateTime BirthDate { get; set; }

        [Required(ErrorMessage = "At least one address is required.")]
        public List<AddressDto> Addresses { get; set; } = new List<AddressDto>();
    }
}
