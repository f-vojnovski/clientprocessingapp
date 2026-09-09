using System.Xml;
using System.Xml.Serialization;

namespace ClientXMLApp.Services.DTOs
{
    [XmlRoot("Clients")]
    public class XmlClientList
    {
        [XmlElement("Client")]
        public List<XmlClient> Clients { get; set; } = new List<XmlClient>();
    }

    public class XmlClient
    {
        [XmlAttribute("ID")]
        public int ID { get; set; }

        public string Name { get; set; } = string.Empty;

        [XmlArray("Addresses")]
        [XmlArrayItem("Address")]
        public List<XmlAddress> Addresses { get; set; } = new List<XmlAddress>();

        [XmlElement("BirthDate")]
        public string? BirthDate { get; set; }
    }

    public class XmlAddress
    {
        [XmlAttribute("Type")]
        public string? Type { get; set; }

        [XmlText]
        public string AddressText { get; set; } = string.Empty;

        // [XmlText] keeps only the last text node, so markup in the body would be dropped
        // silently. Capturing it here makes the record refusable instead.
        [XmlAnyElement]
        public XmlElement[]? UnexpectedContent { get; set; }
    }
}
