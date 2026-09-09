using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClientXMLApp.Models
{
    public class Address
    {
        public const int AddressTextMaxLength = 400;

        [Key]
        public int ID { get; set; }

        public AddressType Type { get; set; }
        public int ClientID { get; set; }

        [ForeignKey("ClientID")]
        public Client? Client { get; set; }

        [MaxLength(AddressTextMaxLength)]
        public string AddressText { get; set; } = string.Empty;
    }

    public enum AddressType
    {
        Unknown = 0,
        Home = 1,
        Public = 2
    }
}
