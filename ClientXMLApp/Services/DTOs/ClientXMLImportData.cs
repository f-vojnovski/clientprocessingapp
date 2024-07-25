using System.Xml.Serialization;

namespace ClientXMLApp.Services.DTOs
{
    [XmlRoot("Clients")]
    public class XmlClientList
    {
        [XmlElement("Client")]
        public List<XmlClient> Clients { get; set; }
    }

    public class XmlClient
    {
        [XmlAttribute("ID")]
        public int ID { get; set; }

        public string Name { get; set; }

        [XmlArray("Addresses")]
        [XmlArrayItem("Address")]
        public List<XmlAddress> Addresses { get; set; }

        public DateTime BirthDate { get; set; }
    }

    public class XmlAddress
    {
        [XmlAttribute("Type")]
        public int Type { get; set; }

        [XmlText]
        public string AddressText { get; set; }
    }
}
