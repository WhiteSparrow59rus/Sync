using LuchIntegrationEF.SyncService.Data;
using LuchIntegrationEF.SyncService.Services;
using LuchIntegrationEF.SyncService.Services.Interfaces;
using LuchIntegrationEF.SyncService.Synchronization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LuchIntegrationEF.SyncService.Installers
{
    public class ScopedInstaller : IInstaller
    {
        public void InstallServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<DictionarySync>();
            services.AddScoped<DeviceSync>();
            services.AddScoped<ChannelSync>();
            services.AddScoped<IndicationSync>();
            services.AddScoped<EventSync>();
            services.AddScoped<InstantValueSync>();
            services.AddScoped<PowerProfileSync>();
            services.AddScoped<CalibrationSync>();
            services.AddScoped<CalcChannelSync>();
            services.AddScoped<CalcValueDeviceSync>();
            services.AddScoped<IConsumptionService, ConsumptionService>();
            services.AddScoped<ICommandSyncService, CommandSyncService>();
        }
    }
}