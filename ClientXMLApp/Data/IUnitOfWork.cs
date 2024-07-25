using ClientXMLApp.Repositories;

public interface IUnitOfWork
{
    IClientRepository Clients { get; }
    IAddressRepository Addresses { get; }
    Task<int> CompleteAsync();
    void Dispose();
}