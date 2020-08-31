using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LuchIntegrationEF.Objects.Custom.Entities;
using LuchIntegrationEF.SyncService.Contracts.V1;
using LuchIntegrationEF.SyncService.Contracts.V1.Requests.Queries;
using LuchIntegrationEF.SyncService.Helpers;
using LuchIntegrationEF.SyncService.Services.Interfaces;
using LuchIntegrationEF.SyncService.Synchronization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuchIntegrationEF.SyncService.Controllers.V1
{
    [ApiController]
    public class SyncController : Controller
    {
        private readonly DeviceSync _deviceSync;
        private readonly DictionarySync _dictionarySync;
        private readonly CalcChannelSync _calcChannelSync;
        private readonly CalcValueDeviceSync _calcValueDeviceSync;
        private readonly CalibrationSync _calibrationSync;
        private readonly ChannelSync _channelSync;
        private readonly EventSync _eventSync;
        private readonly IndicationSync _indicationSync;
        private readonly InstantValueSync _instantValueSync;
        private readonly PowerProfileSync _powerProfileSync;
        private readonly IConsumptionService _consumptionService;
        private readonly ICommandSyncService _commandSyncService;

        public SyncController(DictionarySync dictionarySync, DeviceSync deviceSync, ChannelSync channelSync,
            IndicationSync indicationSync, EventSync eventSync, InstantValueSync instantValueSync,
            PowerProfileSync powerProfileSync, CalibrationSync calibrationSync, CalcChannelSync calcChannelSync,
            CalcValueDeviceSync calcValueDeviceSync, IConsumptionService consumptionService, ICommandSyncService commandSyncService)
        {
            _dictionarySync = dictionarySync;
            _deviceSync = deviceSync;
            _channelSync = channelSync;
            _indicationSync = indicationSync;
            _eventSync = eventSync;
            _instantValueSync = instantValueSync;
            _powerProfileSync = powerProfileSync;
            _calibrationSync = calibrationSync;
            _calcChannelSync = calcChannelSync;
            _calcValueDeviceSync = calcValueDeviceSync;
            _consumptionService = consumptionService;
            _commandSyncService = commandSyncService;
        }


        /// <summary>
        /// Запускает синхронизацию справочников.
        /// </summary>
        [HttpGet(ApiRoutes.ActionLuch.Dictionary)]
        public async Task DictionariesSync()
        {
            await _dictionarySync.SynchronizeAll(Constants.Luch);
        }

        /// <summary>
        /// Запускает синхронизацию команд.
        /// </summary>
        [HttpGet(ApiRoutes.ActionLuch.Commands)]
        public async Task CommandsSync()
        {
            await _commandSyncService.CommandSync();
        }

        /// <summary>
        /// Запускает синхронизацию устройств
        /// </summary>
        /// <param name="dateRangeQuery">Диапазон дат.</param>
        [HttpPost(ApiRoutes.ActionLuch.All)]
        public async Task SyncDeviceAll([FromBody] DateRangeQuery dateRangeQuery)
        {
            var selectedSystem = Constants.Luch;

            List<Device> devices;
            var pageNumber = 1;

            do
            {
                devices = await _deviceSync.SynchronizeByPage(dateRangeQuery, pageNumber, selectedSystem);

                if (devices.Any())
                {
                    await _eventSync.SynchronizeWithBackendData(devices, _deviceSync.BackendSyncData, selectedSystem);

                    var channels = await _channelSync.SynchronizeWithBackendData(devices, _deviceSync.BackendSyncData);

                    var calibrations = await _calibrationSync.SynchronizeWithBackendData(channels, _deviceSync.BackendSyncData);

                    var calculatedChannels = await _calcChannelSync.SynchronizeWithBackendData(devices, channels, _deviceSync.BackendSyncData);

                    if (channels.Any())
                    {
                        await _indicationSync.SynchronizeWithBackendData(channels, _deviceSync.BackendSyncData);

                        await _powerProfileSync.SynchronizeWithBackendData(channels, _deviceSync.BackendSyncData);

                        await _instantValueSync.SynchronizeWithBackendData(devices, channels, _deviceSync.BackendSyncData);
                    }

                    if (calculatedChannels.Any())
                        await _calcValueDeviceSync.SynchronizeWithBackendData(calibrations, calculatedChannels, _deviceSync.BackendSyncData);
                }

                pageNumber++;
            } while (devices.Any());

            await _commandSyncService.CommandSync();
            await _consumptionService.RefreshAllAsync();
        }

        /// <summary>
        /// Запускает синхронизацию устройств.
        /// </summary>
        /// <param name="startDate">Начальная дата</param>
        /// <param name="finishDate">Конечная дата</param>
        [HttpPost(ApiRoutes.ActionMilur.All)]
        public async Task SyncDeviceAllMilur([FromBody] DateRangeQuery dateRangeQuery)
        {
            var selectedSystem = Constants.Milur;

            List<Device> devices;
            var pageNumber = 1;

            devices = await _deviceSync.SynchronizeByPage(dateRangeQuery, pageNumber, selectedSystem);

            if (devices.Any())
            {
                var channels =
                    await _channelSync.SynchronizeWithBackendData(devices, _deviceSync.BackendSyncData);

                if (channels.Any())
                    await _indicationSync.SynchronizeWithBackendData(channels, _deviceSync.BackendSyncData);
            }

            await _consumptionService.RefreshAllAsync();
        }
    }
}