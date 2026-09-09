using System.ComponentModel.DataAnnotations;

namespace ClientXMLApp.Models
{
    public class Client
    {
        [Key]
        public int ID { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime BirthDate { get; set; }

        public ICollection<Address> Addresses { get; set; } = new List<Address>();
    }
}
