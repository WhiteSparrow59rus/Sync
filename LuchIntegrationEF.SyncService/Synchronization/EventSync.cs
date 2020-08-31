using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
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
    public class EventSync
    {
        private readonly DictionarySync _dictionarySync;
        private readonly DataContext _dataContext;
        private readonly ClientLuch _clientLuch;
        private readonly ClientMilur _clientMilur;

        public EventSync(DataContext dataContext, DictionarySync dictionarySync, ClientLuch clientLuch, ClientMilur clientMilur)
        {
            _dataContext = dataContext;
            _dictionarySync = dictionarySync;
            _clientLuch = clientLuch;
            _clientMilur = clientMilur;
        }

        /// <summary>
        /// Синхронизирует каналы, получая на вход данные с бекэнда.
        /// </summary>
        /// <param name="devicesSynced"></param>
        /// <param name="devicesBackEnd"></param>
        public async Task<List<Event>> SynchronizeWithBackendData(List<Device> devicesSynced, JArray devicesBackEnd, string system)
        {
            var dataToSave = MapData(devicesSynced, devicesBackEnd, system).Result;
            await _dataContext.SaveChangesAsync();
            return dataToSave;
        }

        /// <summary>
        /// Получает события для устройства.
        /// </summary>
        /// <param name="data">Json массив устройств с BackEnd`a</param>
        /// <param name="startDate">Начальная дата</param>
        /// <param name="finishDate">Конечная дата</param>
        /// <returns>Json массив устройств с событиями</returns>
        public JArray GetBackEndData(JArray data, DateRangeQuery dateRangeQuery, string system)
        {
            switch (system)
            {
                case Constants.Luch:
                {
                    _clientLuch.GetEventsByDevices(data, dateRangeQuery);
                        break;
                }
                case Constants.Milur:
                {
                    _clientMilur.GetEventsByDevices(data, dateRangeQuery);
                        break;
                }
            }
            
            return data;
        }

        /// <summary>
        /// Мапим события.
        /// </summary>
        /// <param name="dataFrontEnd">Список устройств</param>
        /// <param name="dataBackEnd">Json массив устройств с BackEnd`a</param>
        /// <returns></returns>
        public async Task<List<Event>> MapData(List<Device> devicesFrontEnd, JArray devicesBackEnd, string system)
        {
            // Подготовка ограничений вычитки.
            var dates = new List<DateTimeOffset>();
            var codes = new List<int>();
            var protocols = new List<string>();

            foreach (var deviceBackEnd in devicesBackEnd)
            {
                protocols.Add(deviceBackEnd["protocol"].Value<string>());

                foreach (var eventBackEnd in deviceBackEnd["events"])
                {
                    dates.Add(eventBackEnd["timestamp"].Value<DateTimeOffset>());
                    codes.Add(eventBackEnd["code"].Value<int>());
                }
            }

            dates.Sort();
            var minDate = dates.FirstOrDefault();
            var maxDate = dates.LastOrDefault();

            var selectedCodes = codes.Distinct();
            var selectedProtocols = protocols.Distinct();

            var result = new List<Event>();

            // Вычитка событий.
            var events = await _dataContext.Events.Where(x => devicesFrontEnd.Contains(x.Device) 
                                                            && (x.TimeStamp >= minDate && x.TimeStamp <= maxDate)
                                                            && selectedProtocols.Contains(x.EventType.Protocol.Name)
                                                            && selectedCodes.Contains(x.EventType.Code))
                                                            .Select(x => new
                                                            {
                                                                x.Device,
                                                                x.TimeStamp,
                                                                x.EventType.Code
                                                            })
                                                            .AsNoTracking()
                                                            .ToListAsync();

            // Вычитка типов событий.
            var typeEvents = await _dataContext.EventTypes.Where(x =>
                    devicesFrontEnd.Select(d => d.Protocol).Contains(x.Protocol) && x.System == system)
                .Select(x => new EventType
                {
                    Id = x.Id,
                    Code = x.Code,
                    Protocol = new Protocol
                    {
                        Name = x.Protocol.Name
                    }
                })
                .AsNoTracking()
                .ToListAsync();

            // Проверка, есть ли все типы событий.
            var checkEventType = true;
            foreach (var deviceBackEnd in devicesBackEnd)
            {
                foreach (var eventsBackEnd in deviceBackEnd["events"])
                {
                    if (typeEvents.FirstOrDefault(x => x.Code == eventsBackEnd["code"].Value<int>() && x.Protocol.Name == deviceBackEnd["protocol"].Value<string>()) == null)
                    {
                        checkEventType = false;
                    }
                }
            }

            if (!checkEventType)
            {
                var dateRange = new DateRangeQuery
                {
                    From = DateTime.Now,
                    To = DateTime.Now
                };
                await _dictionarySync.SynchronizeAll(system);
                typeEvents = await _dataContext.EventTypes.Where(x =>
                        devicesFrontEnd.Select(d => d.Protocol).Contains(x.Protocol) && x.System == system)
                    .Select(x => new EventType
                    {
                        Id = x.Id,
                        Code = x.Code,
                        Protocol = new Protocol
                        {
                            Name = x.Protocol.Name
                        }
                    })
                    .ToListAsync();
            }

            foreach (var deviceBackEnd in devicesBackEnd)
            {
                var device = devicesFrontEnd.FirstOrDefault(x => x.BackEndId == deviceBackEnd["id"].Value<int>());

                if (deviceBackEnd["events"].HasValues)
                {
                    var deviceEvents = events.Where(x => x.Device.Id == device.Id).ToList();

                    var selectedEvents = deviceBackEnd["events"].Where(x => 
                        !deviceEvents.Any(z => 
                            z.TimeStamp == x["timestamp"].Value<DateTimeOffset>() && 
                            z.Code == x["code"].Value<int>())).ToList();

                    foreach (var eventDevice in selectedEvents)
                    {
                        var selectedEventType = typeEvents.FirstOrDefault(x => x.Code == eventDevice["code"].Value<int>() && x.Protocol.Name == deviceBackEnd["protocol"].Value<string>());
                        
                        result.Add(await UpdateEvent(null, device, selectedEventType, eventDevice));
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Создает событие.
        /// </summary>
        /// <param name="eventDevice">Канал</param>
        /// <param name="device">Устройство</param>
        /// <param name="typeEvent">Тип события</param>
        /// <param name="backendData">Json объект канала с BackEnd`a</param>
        /// <returns></returns>
        private async Task<Event> UpdateEvent(Event eventDevice, Device device, EventType typeEvent, JToken backendData)
        {
            if (eventDevice == null)
            {
                eventDevice = new Event
                {
                    TimeStampReceived = DateTimeOffset.Now,
                    DeviceId = device.Id,
                    TimeStamp = backendData["timestamp"].Value<DateTimeOffset>(),
                    Info = backendData["metadata"].Value<string>(),
                    EventTypeId = typeEvent.Id
                };

                await _dataContext.Events.AddAsync(eventDevice);
            }

            return eventDevice;
        }
    }
}
