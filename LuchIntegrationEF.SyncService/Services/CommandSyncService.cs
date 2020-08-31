using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LuchIntegrationEF.Objects.Custom.Enums;
using LuchIntegrationEF.SyncService.Data;
using LuchIntegrationEF.SyncService.HttpClients;
using LuchIntegrationEF.SyncService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace LuchIntegrationEF.SyncService.Services
{
    public class CommandSyncService : ICommandSyncService
    {
        private readonly DataContext _dataContext;
        private readonly ClientLuch _clientLuch;

        public CommandSyncService(DataContext dataContext, ClientLuch clientLuch)
        {
            _dataContext = dataContext;
            _clientLuch = clientLuch;
        }

        public async Task CommandSync()
        {
            var commands = await _dataContext.Commands.Where(x => x.Status == CommandStatus.Accepted).ToListAsync();

            foreach (var command in commands)
            {
                var commandResponse = JObject.Parse(command.Response);
                var commandNewResponse = _clientLuch.GetCommandById(commandResponse["id"].ToString());

                if (commandNewResponse != null && commandNewResponse["status"].Value<string>() == "sent")
                {
                    command.Status = CommandStatus.Completed;
                }
            }

            await _dataContext.SaveChangesAsync();
        }
    }
}
