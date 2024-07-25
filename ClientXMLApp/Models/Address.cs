using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace ClientXMLApp.Models
{
    public class Address
    {
        [Key]
        public int ID { get; set; }

        public AddressType Type { get; set; }
        public int ClientID { get; set; }

        [ForeignKey("ClientID")]
        public Client Client { get; set; }
        public string AddressText { get; set; }
    }

    public enum AddressType
    {
        Unknown = 0,
        Home = 1,
        Public = 2
    }
}
