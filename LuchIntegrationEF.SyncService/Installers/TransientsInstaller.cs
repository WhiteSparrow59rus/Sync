using LuchIntegrationEF.SyncService.Data;
using LuchIntegrationEF.SyncService.Synchronization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LuchIntegrationEF.SyncService.Installers
{
    public class TransientsInstaller : IInstaller
    {
        public void InstallServices(IServiceCollection services, IConfiguration configuration)
        {
        }
    }
}