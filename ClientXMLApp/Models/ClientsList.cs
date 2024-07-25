using System.Xml.Serialization;

namespace ClientXMLApp.Models
{
    [XmlRoot("Clients")]
    public class ClientList
    {
        [XmlElement("Client")]
        public List<Client> Clients { get; set; }
    }
}
