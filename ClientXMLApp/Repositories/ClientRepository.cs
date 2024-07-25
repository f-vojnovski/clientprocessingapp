﻿using Microsoft.EntityFrameworkCore;
using ClientXMLApp.Data;
using ClientXMLApp.Models;

namespace ClientXMLApp.Repositories
{
    public class ClientRepository : IClientRepository
    {
        private readonly AppDbContext _context;

        public ClientRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Client>> GetAllClientsAsync()
        {
            return await _context.Clients.Include(c => c.Addresses).ToListAsync();
        }

        public async Task<Client> GetClientByIdAsync(int id)
        {
            return await _context.Clients.Include(c => c.Addresses).FirstOrDefaultAsync(c => c.ID == id);
        }

        public async Task AddClientAsync(Client client)
        {
            await _context.Clients.AddAsync(client);
        }

        public async Task UpdateClientAsync(Client client)
        {
            _context.Clients.Update(client);
        }

        public async Task DeleteClientAsync(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client != null)
            {
                _context.Clients.Remove(client);
            }
        }
    }
}