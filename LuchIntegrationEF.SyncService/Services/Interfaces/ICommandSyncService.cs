using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LuchIntegrationEF.SyncService.Services.Interfaces
{
    public interface ICommandSyncService
    {
        Task CommandSync();
    }
}
