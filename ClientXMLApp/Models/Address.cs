using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClientXMLApp.Models
{
    public class Address
    {
        [Key]
        public int ID { get; set; }

        public AddressType Type { get; set; }
        public int ClientID { get; set; }

        [ForeignKey("ClientID")]
        public Client? Client { get; set; }
        public string AddressText { get; set; } = string.Empty;
    }

    public enum AddressType
    {
        Unknown = 0,
        Home = 1,
        Public = 2
    }
}
