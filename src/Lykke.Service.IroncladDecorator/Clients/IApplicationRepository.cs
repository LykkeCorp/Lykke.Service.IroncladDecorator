using System.Threading.Tasks;

namespace Lykke.Service.IroncladDecorator.Clients
{
    public interface IApplicationRepository
    {
        Task<ClientApplication> GetByIdAsync(string id);
    }
}
