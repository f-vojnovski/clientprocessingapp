using System.ComponentModel.DataAnnotations;

namespace ClientXMLApp.Models
{
    public class Client
    {
        public const int NameMaxLength = 200;

        [Key]
        public int ID { get; set; }

        [MaxLength(NameMaxLength)]
        public string Name { get; set; } = string.Empty;

        public DateTime BirthDate { get; set; }

        public ICollection<Address> Addresses { get; set; } = new List<Address>();
    }
}
