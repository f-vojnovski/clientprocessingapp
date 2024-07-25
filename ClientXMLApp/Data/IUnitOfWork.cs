using ClientXMLApp.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

public interface IUnitOfWork
{
    IClientRepository Clients { get; }
    IAddressRepository Addresses { get; }
    Task<int> CompleteAsync();
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}