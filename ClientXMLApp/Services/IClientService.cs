﻿using ClientXMLApp.Services.DTOs;

namespace ClientXMLApp.Services
{
    public interface IClientService
    {
        Task<IEnumerable<ViewClientDto>> GetAllClientsAsync();
        Task<ViewClientDto> GetClientByIdAsync(int id);
        Task AddClientAsync(AddClientDto clientDto);
        Task UpdateClientAsync(UpdateClientDto clientDto);
        Task DeleteClientAsync(int id);
    }
}