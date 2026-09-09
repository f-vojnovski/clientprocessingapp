namespace ClientXMLApp.Services.DTOs
{
    public class UpdateClientDto
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public ICollection<AddressDto> Addresses { get; set; } = new List<AddressDto>();
    }
}
