using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LuchIntegrationEF.Objects.Custom.Entities;
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
    public class ChannelSync
    {
        private readonly DataContext _dataContext;
        private readonly IndicationSync _indicationSync;
        private readonly ClientLuch _clientLuch;
        private readonly ClientMilur _clientMilur;

        public ChannelSync(DataContext dataContext, IndicationSync indicationSync, ClientLuch clientLuch, ClientMilur clientMilur)
        {
            _dataContext = dataContext;
            _indicationSync = indicationSync;
            _clientLuch = clientLuch;
            _clientMilur = clientMilur;
        }
        
        /// <summary>
        /// Синхронизирует каналы, получая на вход данные с бекэнда.
        /// </summary>
        /// <param name="devicesSynced"></param>
        /// <param name="devicesBackEnd"></param>
        public async Task<List<Channel>> SynchronizeWithBackendData(List<Device> devicesSynced, JArray devicesBackEnd)
        {
            var dataToSave = MapData(devicesSynced, devicesBackEnd).Result;
            await SaveData(dataToSave);
            return dataToSave;
        }

        /// <summary>
        /// Получает каналы для устройства.
        /// </summary>
        /// <param name="data">Json массив устройств с BackEnd`a</param>
        /// <returns>Json массив устройств с каналами</returns>
        public JArray GetBackEndData(JArray data, DateRangeQuery dateRangeQuery, string system)
        {
            switch (system)
            {
                case Constants.Luch:
                {
                    _clientLuch.GetChannelsByDevices(data);
                        break;
                }
                case Constants.Milur:
                {
                    _clientMilur.GetChannelsByDevices(data);
                        break;
                }
            }

            _indicationSync.GetBackEndData(data, dateRangeQuery, system);

            return data;
        }

        /// <summary>
        /// Получает каналы устройств.
        /// </summary>
        /// <param name="devices">Коллекция устройств.</param>
        /// <param name="startDate">Начальная дата.</param>
        /// <param name="finishDate">Конечная дата.</param>
        /// <returns></returns>
        //public async Task<JArray> GetBackEndData(List<Device> devices, DateRangeQuery dateRangeQuery)
        //{
        //    var result = new JArray();
        //    Parallel.ForEach(devices,
        //        async device =>
        //        {
        //            var responseChannelsBackEnd = await Client.GetAsync($"devices/{ device.BackEndId}/channels");
        //            if (responseChannelsBackEnd.IsSuccessStatusCode)
        //            {
        //                result.Merge(JArray.Parse(await responseChannelsBackEnd.Content.ReadAsStringAsync()));
        //                var jsonReader = new JsonTextReader(new StringReader(responseChannelsBackEnd.Content.ReadAsStringAsync().Result)) { DateParseHandling = DateParseHandling.DateTimeOffset };
        //                result.Merge(JArray.Load(jsonReader));
        //            }
        //        });

        //    return result;
        //}

        /// <summary>
        /// Маппит каналы устройств с бекэнда на прикладные.
        /// </summary>
        /// <param name="devicesFrontEnd">Устройства с фронтенда.</param>
        /// <param name="devicesBackEnd">Устройства с бекэнда.</param>
        /// <returns></returns>
        protected async Task<List<Channel>> MapData(List<Device> devicesFrontEnd, JArray devicesBackEnd)
        {
            var result = new List<Channel>();

            // Вычитывает единицы измерения.
            var units = await _dataContext.Units.ToListAsync();

            var channelsFrontEnd = await _dataContext.Channels.Where(x => x.Logical == false && devicesFrontEnd.Contains(x.Device))
                .Select(x => new Channel
                {
                    Id = x.Id,
                    BackEndId = x.BackEndId,
                    Name = x.Name
                })
                .ToListAsync();

            foreach (var deviceBackEnd in devicesBackEnd)
            {
                var device = devicesFrontEnd.FirstOrDefault(x => deviceBackEnd["id"].Value<int>() == x.BackEndId);

                if (device != null)
                {
                    foreach (var channelBackEnd in deviceBackEnd["channels"])
                    {
                        var channel = channelsFrontEnd.FirstOrDefault(x => x.BackEndId == channelBackEnd["id"].Value<int>());

                        var unit = units.FirstOrDefault(x =>
                            x.ShortNameBackEnd == channelBackEnd["unit"].Value<string>());
                        if (unit == null)
                        {
                            unit = new Unit
                            {
                                ShortNameBackEnd = channelBackEnd["unit"].Value<string>(),
                                ShortName = channelBackEnd["unit"].Value<string>()
                            };

                            await _dataContext.Units.AddAsync(unit);
                            await _dataContext.SaveChangesAsync();

                            if (!units.Contains(unit))
                            {
                                units.Add(unit);
                            }
                        }

                        result.Add(UpdateChannel(
                            channel,
                            device,
                            unit,
                            channelBackEnd));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Обновляет/создает канал.
        /// </summary>
        /// <param name="channel">Канал</param>
        /// <param name="device">Устройство</param>
        /// <param name="unit">Единица измерения</param>
        /// <param name="backendData">Json объект канала с BackEnd`a</param>
        /// <returns></returns>
        private Channel UpdateChannel(Channel channel, Device device, Unit unit, JToken backendData)
        {
            if (channel == null)
            {
                channel = new Channel
                {
                    DeviceId = device.Id,
                    Name = backendData["name"].Value<string>(),
                    Number = backendData["number"].Value<int>(),
                    UnitId = unit.Id,
                    BackEndId = backendData["id"].Value<int>(),
                    Logical = false,
                };
            }
            else
            {
                channel.DeviceId = device.Id;
                channel.UnitId = unit.Id;
                channel.Name = backendData["name"].Value<string>();
                channel.Number = backendData["number"].Value<int>();
                channel.BackEndId = backendData["id"].Value<int>();
            }

            return channel;
        }

        public async Task SaveData<T>(List<T> objects)
        {
            _dataContext.UpdateRange(objects.Cast<Object>());
            await _dataContext.SaveChangesAsync();
        }
    }
}
