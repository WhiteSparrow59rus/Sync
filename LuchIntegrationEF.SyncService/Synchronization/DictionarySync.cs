using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;
using LuchIntegrationEF.Objects.Custom.Entities;
using LuchIntegrationEF.SyncService.Contracts.V1.Requests.Queries;
using LuchIntegrationEF.SyncService.Data;
using LuchIntegrationEF.SyncService.HttpClients;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using TimeZone = LuchIntegrationEF.Objects.Custom.Entities.TimeZone;

namespace LuchIntegrationEF.SyncService.Synchronization
{
    public class DictionarySync
    {
        private readonly DataContext _dataContext;
        private readonly ClientLuch _clientLuch;

        public DictionarySync(DataContext dataContext, ClientLuch clientLuch)
        {
            _dataContext = dataContext;
            _clientLuch = clientLuch;
        }
        
        /// <summary>
        /// Синхронизирует все данные с бекэндом.
        /// </summary>
        public async Task<List<Object>> SynchronizeAll(string system)
        {
            var backEndData = GetBackEndData();
            var dataToSave = MapData(backEndData, system).Result;
            await SaveData(dataToSave);
            return dataToSave;
        }

        /// <summary>
        /// Получает справочник моделей устройств
        /// </summary>
        /// <param name="system"></param>
        /// <returns>Json массив справочник моделей устройств</returns>
        protected JArray GetBackEndData()
        {
            return _clientLuch.GetDictionary();
        }
        
        /// <inheritdoc />
        protected async Task<List<Object>> MapData(JArray data, string system)
        {
            // Вычитка моделей устройств.
            var deviceModels = await _dataContext.DeviceModels.ToListAsync();

            // Вычитка временных зон.
            var timeZones = await _dataContext.TimeZones.ToListAsync();

            // Вычитка типов событий.
            var eventTypes = await _dataContext.EventTypes.ToListAsync();

            //Вычитка протоколов устройств.
            var protocols = await _dataContext.Protocols.ToListAsync();

            List<Object> toUpdate = new List<Object>();
            foreach (var objectsBackEnd in data.First)
            {
                JProperty parentProp = (JProperty)objectsBackEnd;
                switch (parentProp.Name)
                {
                    case "deviceModels":
                    {
                        foreach (var objectBackEnd in objectsBackEnd.First)
                        {
                            var model = deviceModels.FirstOrDefault(x => x.BackEndId == objectBackEnd["id"].Value<int>());
                            if (model == null)
                            {
                                model = new DeviceModel
                                {
                                    SKU = objectBackEnd["SKU"].Value<string>(),
                                    BackEndId = objectBackEnd["id"].Value<int>(),
                                    Name = objectBackEnd["fullName"].Value<string>(),
                                    ShortName = objectBackEnd["name"].Value<string>(),
                                    System = system
                                };

                                toUpdate.Add(model);
                            }
                        }
                        break;
                    }
                    case "timezones":
                    {
                        foreach (var objectBackEnd in objectsBackEnd.First)
                        {
                            var timezone =
                                timeZones.FirstOrDefault(x => x.BackEndId == objectBackEnd["id"].Value<int>());
                            if (timezone == null)
                            {
                                timezone = new TimeZone
                                {
                                    Name = objectBackEnd["name"].Value<string>(),
                                    BackEndId = objectBackEnd["id"].Value<int>(),
                                    OffsetSeconds = objectBackEnd["offsetSeconds"].Value<int>(),
                                    OffsetUTC = objectBackEnd["offsetUTC"].Value<string>(),
                                    System = system
                                };

                                toUpdate.Add(timezone);
                            }
                        }
                        break;
                    }
                    case "eventTypes":
                    {
                        foreach (var objectBackEnd in objectsBackEnd.First)
                        {
                            var eventType = eventTypes.FirstOrDefault(x =>
                                x.Code == objectBackEnd["code"].Value<int>() &&
                                x.Protocol.Name == objectBackEnd["protocol"].Value<string>());
                            if (eventType == null)
                            {
                                var protocol = protocols.FirstOrDefault(x =>
                                    x.Name == objectBackEnd["protocol"].Value<string>());
                                if (protocol == null)
                                {
                                    protocol = new Protocol 
                                    {
                                        Name = objectBackEnd["protocol"].Value<string>()
                                    };

                                    await _dataContext.Protocols.AddAsync(protocol);
                                    await _dataContext.SaveChangesAsync();

                                    if (!protocols.Contains(protocol))
                                    {
                                        protocols.Add(protocol);
                                    }
                                }

                                eventType = new EventType
                                {
                                    Code = objectBackEnd["code"].Value<int>(),
                                    Description = objectBackEnd["description"].Value<string>(),
                                    ProtocolId = protocol.Id,
                                    System = system
                                };

                                toUpdate.Add(eventType);
                            }
                        }
                        break;
                    }
                    default:
                    {
                        break;
                    }
                }
                
            }
            
            return toUpdate;
        }

        public async Task SaveData<T>(List<T> objects)
        {
            _dataContext.UpdateRange(objects.Cast<Object>());
            await _dataContext.SaveChangesAsync();
        }
    }
}
