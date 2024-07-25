using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Xml.Serialization;

namespace ClientXMLApp.Models
{
    public class Client
    {
        [Key]
        public int ID { get; set; }

        public string Name { get; set; }

        public DateTime BirthDate { get; set; }

        public ICollection<Address> Addresses { get; set; }
    }
}
