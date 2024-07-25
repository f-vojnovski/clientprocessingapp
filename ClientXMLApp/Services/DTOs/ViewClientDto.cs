namespace ClientXMLApp.Services.DTOs
{
    public class ViewClientDto
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public DateTime BirthDate { get; set; }
        public ICollection<AddressDto> Addresses { get; set; }
    }
}
