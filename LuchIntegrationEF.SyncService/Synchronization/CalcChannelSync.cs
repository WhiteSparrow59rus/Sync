using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LuchIntegrationEF.Objects.Custom.Entities;
using LuchIntegrationEF.Objects.Custom.Enums;
using LuchIntegrationEF.SyncService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace LuchIntegrationEF.SyncService.Synchronization
{
    public class CalcChannelSync
    {
        private readonly DataContext _dataContext;

        public CalcChannelSync(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        /// <summary>
        /// Синхронизирует расчетные каналы, получая на вход данные с бекэнда.
        /// </summary>
        /// <param name="devicesFrontEnd">Коллекция устройств фронта.</param>
        /// <param name="channelsSynced">Коллекция каналов фронта.</param>
        /// <param name="devicesBackEnd">Коллекция устройств бека.</param>
        /// <returns></returns>
        public async Task<List<Channel>> SynchronizeWithBackendData(List<Device> devicesFrontEnd, List<Channel> channelsSynced, JArray devicesBackEnd)
        {
            var dataToSave = MapData(devicesFrontEnd, channelsSynced, devicesBackEnd).Result;
            await SaveData(dataToSave);
            return dataToSave;
        }

        /// <summary>
        /// Маппит каналы устройств с бекэнда на прикладные.
        /// </summary>
        /// <param name="devices">Устройства с фронтенда.</param>
        /// <param name="channelsFrontEnd">Каналы с фронтенда.</param>
        /// <param name="devicesBackEnd">Устройства с бекэнда.</param>
        /// <returns></returns>
        protected async Task<List<Channel>> MapData(List<Device> devices, List<Channel> channelsFrontEnd, JArray devicesBackEnd)
        {
            var result = new List<Channel>();

            var channelNamePropMapping = new Dictionary<string, int>
            {
                { "impulse_counter_ch1" , 1 },
                { "impulse_counter_ch2" , 2 },
                { "impulse_counter_ch3" , 3 },
                { "impulse_counter_ch4" , 4 }
            };

            var filteredChannels =
                channelsFrontEnd.Where(x => channelNamePropMapping.Select(y => y.Key).Contains(x.Name)).ToList();

            if (filteredChannels.Any())
            {
                var channels = await _dataContext.Channels.Where(x => filteredChannels.Select(y => y.Id).Contains(x.Id))
                    .ToListAsync();

                if (channels.Any())
                {
                    var units = await _dataContext.Units.ToListAsync();

                    var calibrations = await _dataContext.Calibrations
                        .Where(x => channels.Select(y => y.Id).Contains(x.Channel.Id)).ToListAsync();

                    var logicalChannelsFrontEnd = await _dataContext.Channels.Where(x =>
                        channels.Select(y => y.Id).Contains(x.PhysicalChannel.Id) && x.Logical).ToListAsync();

                    foreach (var deviceBackEnd in devicesBackEnd)
                    {
                        var device = devices.FirstOrDefault(x => deviceBackEnd["id"].Value<int>() == x.BackEndId);

                        if (device != null)
                        {
                            foreach (var channelBackEnd in deviceBackEnd["channels"].Where(x => filteredChannels.Select(y => y.Name).Contains(x["name"].Value<string>())))
                            {
                                if (channelBackEnd["unit"].Value<string>() == "imp")
                                {
                                    // Находим физический канал, измеряющий в импульсах.
                                    var physicalСhannel = channels.FirstOrDefault(x => channelBackEnd["id"].Value<int>() == x.BackEndId);

                                    // Находим все инициирующие показания физического канала.
                                    var selectedCalibrations = calibrations.Where(x => x.Channel.Id == physicalСhannel?.Id).ToList();

                                    if (selectedCalibrations.Any())
                                    {
                                        // Находим последнее инициирующее показание по времени ввода показаний.
                                        // Возможно необходимо смотреть на время события, не обсуждалось.
                                        var lastCalibration = selectedCalibrations.OrderByDescending(x => x.TimeStampInput).First();

                                        var unit = units.FirstOrDefault(x => x.Id == lastCalibration.Unit.Id);

                                        // Находим логический ( расчетный ) канал текущиего физического канала.
                                        var calculatedChannel = logicalChannelsFrontEnd.FirstOrDefault(x => x.PhysicalChannel.Id == physicalСhannel?.Id);

                                        if (lastCalibration != null && device.DeviceType.MeasuredResource != MeasuredResource.Unknown)
                                        {
                                            result.Add(UpdateChannel(
                                                physicalСhannel,
                                                calculatedChannel,
                                                device,
                                                unit,
                                                channelBackEnd));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Обновляет/создает канал.
        /// </summary>
        /// <param name="physicalСhannel">Канал</param>
        /// <param name="calculatedChannel"></param>
        /// <param name="device">Устройство</param>
        /// <param name="unit">Единица измерения</param>
        /// <param name="backendData">Json объект канала с BackEnd`a</param>
        /// <returns></returns>
        private Channel UpdateChannel(Channel physicalСhannel, Channel calculatedChannel, Device device, Unit unit, JToken backendData)
        {
            if (calculatedChannel == null)
            {
                calculatedChannel = new Channel
                {
                    DeviceId = device.Id,
                    Name = $"{backendData["name"].Value<string>()}_{device.DeviceType.NameEng}",
                    Number = backendData["number"].Value<int>(),
                    UnitId = unit.Id,
                    Logical = true,
                    PhysicalChannelId = physicalСhannel.Id
                };
            }

            return calculatedChannel;
        }

        public async Task SaveData<T>(List<T> objects)
        {
            _dataContext.UpdateRange(objects.Cast<Object>());
            await _dataContext.SaveChangesAsync();
        }
    }
}
