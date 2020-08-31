using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LuchIntegrationEF.Objects.Custom.Entities;
using LuchIntegrationEF.Objects.Custom.Enums;
using LuchIntegrationEF.SyncService.Contracts.V1.Requests.Queries;
using LuchIntegrationEF.SyncService.Data;
using LuchIntegrationEF.SyncService.Helpers;
using LuchIntegrationEF.SyncService.HttpClients;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LuchIntegrationEF.SyncService.Synchronization
{
    public class DeviceSync
    { 
        private readonly DataContext _dataContext;
        private readonly DictionarySync _dictionarySync;
        private readonly ChannelSync _channelSync;
        private readonly EventSync _eventSync;
        private readonly CalibrationSync _calibrationSync;
        private readonly ClientLuch _clientLuch;
        private readonly ClientMilur _clientMilur;

        public DeviceSync(DataContext dataContext, DictionarySync dictionarySync, ChannelSync channelSync, EventSync eventSync, CalibrationSync calibrationSync, ClientLuch clientLuch, ClientMilur clientMilur)
        {
            _dataContext = dataContext;
            _dictionarySync = dictionarySync;
            _channelSync = channelSync;
            _eventSync = eventSync;
            _calibrationSync = calibrationSync;
            _clientLuch = clientLuch;
            _clientMilur = clientMilur;
        }

        /// <summary>
        /// Данные об устройствах, пришедшие с бекэнда.
        /// </summary>
        public JArray BackendSyncData { get; set; }

        /// <summary>
        /// Синхронизирует порционно данные с бекэндом.
        /// </summary>
        public async Task<List<Device>> SynchronizeByPage(DateRangeQuery dateRangeQuery, int page, string system)
        {
            //ChangeApiEndPoint(system);
            var backEndData = GetBackEndData(dateRangeQuery, page, system).Result;
            var dataToSave = MapData(backEndData, system).Result;
            await SaveData(dataToSave);
            return dataToSave;
        }

        /// <summary>
        /// Получает все устройства, изменения в которых произошли после указанной начальной даты.
        /// </summary>
        /// <param name="page">Страница</param>
        /// <returns>Json список устройств с BackEnd`a</returns>
        public async Task<JArray> GetBackEndData(DateRangeQuery dateRangeQuery, int page, string system)
        {
            JArray result = null;

            switch (system)
            {
                case Constants.Luch:
                {
                    result = await _clientLuch.GetDevicesByPage(dateRangeQuery, page);
                        break;
                }
                case Constants.Milur:
                {
                    result = await _clientMilur.GetDevicesByPage(dateRangeQuery, page); ;
                    break;
                }
            }

            if (result != null && result.HasValues)
            {
                var deviceChannel = _channelSync.GetBackEndData(result, dateRangeQuery, system);
                var deviceChannelAndEvents = _eventSync.GetBackEndData(deviceChannel, dateRangeQuery, system);
                var deviceChannelEventAndInitValues = _calibrationSync.GetBackEndData(deviceChannelAndEvents, system);

                BackendSyncData = deviceChannelEventAndInitValues;
            }

            return result;
        }
        
        /// <summary>
        /// Мапим показания из Json в модели БД.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="system"></param>
        /// <returns></returns>
        protected async Task<List<Device>> MapData(JArray data, string system)
        {
            List<Device> devicesToUpdate = new List<Device>();
            if (data.HasValues)
            {
                // Вычитка видов устройств.
                var deviceTypes = await _dataContext.DeviceTypes.ToListAsync();

                var arrayDevice = data.Select(x => x["id"].Value<int?>()).ToList();
                // Вычитка устройств.
                var devices = await _dataContext.Devices
                    .Where(x => x.System == system && arrayDevice.Contains(x.BackEndId)).ToListAsync();

                // Вычитка из кэша моделей устройств.
                var deviceModels = await _dataContext.DeviceModels.ToListAsync();

                // Вычитка протоколов.
                var protocols = await _dataContext.Protocols.ToListAsync();
                
                var checkModels = true;
                foreach (var deviceBackEnd in data)
                {
                    if (deviceModels.FirstOrDefault(x => x.BackEndId == deviceBackEnd["modelId"].Value<int>()) == null)
                    {
                        checkModels = false;
                    }
                }
                if (!checkModels)
                {
                    await _dictionarySync.SynchronizeAll(system);
                    deviceModels = _dataContext.DeviceModels.ToList();
                }
                foreach (var deviceBackEnd in data)
                {
                    var model = deviceModels.FirstOrDefault(x =>
                        x.BackEndId == deviceBackEnd["modelId"].Value<int>());

                    if (model != null)
                    {
                        var deviceType = deviceTypes.FirstOrDefault(x => x.MeasuredResource == model.MeasuredResource) ??
                                         deviceTypes.FirstOrDefault(x => x.MeasuredResource == MeasuredResource.Unknown);

                        var device = devices.FirstOrDefault(x => x.BackEndId == deviceBackEnd["id"].Value<int>()) ??
                            new Device();

                        var protocol =
                            protocols.FirstOrDefault(x => x.Name == deviceBackEnd["protocol"].Value<string>());
                        if (protocol == null)
                        {
                            protocol = new Protocol
                            {
                                Name = deviceBackEnd["protocol"].Value<string>()
                            };

                            await _dataContext.Protocols.AddAsync(protocol);
                            await _dataContext.SaveChangesAsync();

                            if (!protocols.Contains(protocol))
                            {
                                protocols.Add(protocol);
                            }
                        }

                        devicesToUpdate.Add(UpdateDevice(device, model, deviceType, protocol, deviceBackEnd, system));
                    }
                }                
            }

            return devicesToUpdate;
        }

        /// <summary>
        /// Обновляет/создает устройство.
        /// </summary>
        /// <param name="device">Устройство</param>
        /// <param name="deviceModel">Модель устройства</param>
        /// <param name="deviceView">Вид устройства</param>
        /// <param name="protocol"></param>
        /// <param name="backendDevice">Данные с Back End`a</param>
        /// <param name="system"></param>
        /// <returns>Устройство</returns>
        private Device UpdateDevice(Device device, DeviceModel deviceModel, DeviceType deviceView, Protocol protocol, JToken backendDevice, string system)
        {
            device.ProtocolId = protocol.Id;
            device.SerialNumber = backendDevice["modemId"].Value<string>();
            device.Manufacturer = backendDevice["vendor"].Value<string>();
            device.DeviceModelId = deviceModel.Id;
            device.BackEndId = backendDevice["id"].Value<int>();
            device.DeviceTypeId = deviceView.Id;
            device.TimeStampSync = DateTimeOffset.Now;
            device.System = system;

            return device;
        }

        public async Task SaveData<T>(List<T> objects)
        {
            _dataContext.UpdateRange(objects.Cast<Object>());
            await _dataContext.SaveChangesAsync();
        }
    }
}
