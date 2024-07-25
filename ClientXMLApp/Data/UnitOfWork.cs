using ClientXMLApp.Repositories;

namespace ClientXMLApp.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        public IClientRepository Clients { get; private set; }
        public IAddressRepository Addresses { get; private set; }

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Clients = new ClientRepository(_context);
            Addresses = new AddressRepository(_context);
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}