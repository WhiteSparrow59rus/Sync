using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LuchIntegrationEF.SyncService.Contracts.V1;
using LuchIntegrationEF.SyncService.Contracts.V1.Requests.Queries;
using LuchIntegrationEF.SyncService.Helpers;
using LuchIntegrationEF.SyncService.Services;
using LuchIntegrationEF.SyncService.Services.Interfaces;
using LuchIntegrationEF.SyncService.Synchronization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuchIntegrationEF.SyncService.Controllers.V1
{
    [ApiController]
    [Authorize]
    public class ConsumptionController : Controller
    {
        private readonly IConsumptionService _consumptionService;

        public ConsumptionController(IConsumptionService consumptionService)
        {
            _consumptionService = consumptionService;
        }

        /// <summary>
        /// Запускает пересчет расхода.
        /// </summary>
        [HttpGet(ApiRoutes.Consumption.Refresh)]
        public async Task Refresh()
        {
            await _consumptionService.RefreshAllAsync();
        }
    }
}