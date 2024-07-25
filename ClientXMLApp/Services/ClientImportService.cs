using AutoMapper;
using ClientXMLApp.Models;
using ClientXMLApp.Services.DTOs;
using System.Xml.Serialization;

namespace ClientXMLApp.Services
{
    public class ClientImportService : IClientImportService
    {
        private readonly IClientService _clientService;

        public ClientImportService(IClientService clientService, IMapper mapper)
        {
            _clientService = clientService;
        }

        public async Task ImportClientsAsync(string xmlFilePath)
        {
            var xmlSerializer = new XmlSerializer(typeof(XmlClientList));

            using (var reader = new StreamReader(xmlFilePath))
            {
                var xmlClientList = (XmlClientList)xmlSerializer.Deserialize(reader);

                var clientDtos = xmlClientList.Clients.Select(xmlClient => new AddClientDto
                {
                    Name = xmlClient.Name,
                    BirthDate = xmlClient.BirthDate,
                    Addresses = xmlClient.Addresses.Select(xmlAddress => new AddressDto
                    {
                        AddressText = xmlAddress.AddressText,
                        Type = (AddressType)xmlAddress.Type
                    }).ToList()
                }).ToList();

                await _clientService.AddClientsAsync(clientDtos);
            }
        }
    }
}
