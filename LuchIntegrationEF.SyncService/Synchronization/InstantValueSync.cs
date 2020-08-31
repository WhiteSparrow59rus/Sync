using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LuchIntegrationEF.Objects.Custom.Entities;
using LuchIntegrationEF.SyncService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace LuchIntegrationEF.SyncService.Synchronization
{
    public class InstantValueSync
    {
        private readonly DataContext _dataContext;

        public InstantValueSync(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        /// <summary>
        /// Синхронизирует мгновенные показания устройства, получая на вход данные с бекэнда об устройствах.
        /// </summary>
        /// <param name="channelsSynced">Коллекция каналов.</param>
        /// <param name="devicesBackEnd">Данные об устройствах с бекэнда.</param>
        /// <param name="system"></param>
        /// <returns></returns>
        public async Task<List<InstantValue>> SynchronizeWithBackendData(List<Device> deviceFrontEnd, List<Channel> channelsSynced, JArray devicesBackEnd)
        {
            var dataToSave = MapData(deviceFrontEnd, channelsSynced, devicesBackEnd).Result;
            await SaveData(dataToSave);
            return dataToSave;
        }

        /// <summary>
        /// Мапим показания.
        /// </summary>
        /// <param name="channelsFrontEnd">Список каналов</param>
        /// <param name="dataBackEnd">Json массив устройств с каналами</param>
        /// <returns></returns>
        public async Task<List<InstantValue>> MapData(List<Device> deviceFrontEnd, List<Channel> channelsFrontEnd, JArray dataBackEnd)
        {
            var result = new List<InstantValue>();

            // Наименования каналов с мгновенными показаниями.
            var channelNames = new string[]
            {
                "electro_voltage_lsum",
                "electro_current_lsum",
                "electro_frequency",
                "electro_neutral_current"
            };

            var dates = new List<DateTimeOffset>();

            foreach (var deviceBackEnd in dataBackEnd)
            {
                foreach (var channelBackEnd in deviceBackEnd["channels"].Where(x => channelNames.Contains(x["name"].Value<string>())))
                {
                    foreach (var value in channelBackEnd["values"])
                    {
                        dates.Add(value["timestamp"].Value<DateTimeOffset>());
                    }
                }
            }

            if (dates.Any())
            {
                dates.Sort();
                var minDate = dates.FirstOrDefault(x => x > new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero)
                                                        && x < new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero));
                var maxDate = dates.LastOrDefault(x => x > new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero)
                                                       && x < new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero));


                var channelsFrontEndWithUnit = await _dataContext.Channels.Where(x => channelsFrontEnd.Select(y => y.Id).Contains(x.Id))
                    .Select(x => new Channel
                    {
                        Id = x.Id,
                        BackEndId = x.BackEndId,
                        Name = x.Name,
                        Unit = x.Unit
                    })
                    .ToListAsync();

                var instantValues = await _dataContext.InstantValues.Where(x =>
                        deviceFrontEnd.Select(y => y.Id).Contains(x.Device.Id) &&
                    (x.TimeStamp >= minDate.AddDays(-1) && x.TimeStamp <= maxDate.AddDays(1)))
                    .Select(x => new
                    {
                        x.TimeStamp,
                        UnitId = x.Unit.Id,
                        DeviceBackEndId = x.Device.BackEndId,
                        x.Name
                    })
                    .AsNoTracking()
                    .ToListAsync();

                if (dataBackEnd.HasValues)
                {
                    foreach (var deviceBackEnd in dataBackEnd)
                    {
                        var deviceInstantValue =
                            instantValues.Where(x => x.DeviceBackEndId == deviceBackEnd["id"].Value<int>());

                        var selectedDevice =
                            deviceFrontEnd.FirstOrDefault(x => x.BackEndId == deviceBackEnd["id"].Value<int>());

                        foreach (var channelBackEnd in deviceBackEnd["channels"].Where(x => channelNames.Contains(x["name"].Value<string>())))
                        {
                            var channelFrontEnd = channelsFrontEndWithUnit.FirstOrDefault(x => x.BackEndId == channelBackEnd["id"].Value<int>());

                            var unit = channelFrontEnd.Unit;

                            if (channelFrontEnd != null)
                            {
                                //var selectedInstantValues = channelBackEnd["values"].Where(x => !deviceInstantValue.Select(y => y.TimeStamp)
                                //                                                          .Contains(x["timestamp"].Value<DateTimeOffset>())
                                //                                                      && !deviceInstantValue.Select(y => y.DeviceBackEndId).Contains(deviceBackEnd["id"].Value<int>())
                                //                                                      && !deviceInstantValue.Select(y => y.UnitId).Contains(channelFrontEnd.Unit.Id)
                                //                                                      && !deviceInstantValue.Select(y => y.Name).Contains(channelFrontEnd.Name));


                                var selectedInstantValues = channelBackEnd["values"].Where(x =>
                                    !deviceInstantValue.Any(y =>
                                        y.TimeStamp == x["timestamp"].Value<DateTimeOffset>() &&
                                        y.DeviceBackEndId == deviceBackEnd["id"].Value<int>() &&
                                        y.UnitId == channelFrontEnd.Unit.Id &&
                                        y.Name == channelFrontEnd.Name)).ToList();

                                result.AddRange(UpdateDeviceInstantValue
                                (
                                    selectedDevice,
                                    unit,
                                    channelFrontEnd.Name,
                                    selectedInstantValues
                                ));
                            }

                        }
                    }
                }
            }
            
            return result;
        }

        private List<InstantValue> UpdateDeviceInstantValue(Device device, Unit unit, string Name, IEnumerable<JToken> values)
        {
            var result = new List<InstantValue>();

            foreach (var value in values)
            {
                result.Add(new InstantValue
                {
                    Value = value["value"].Value<decimal>(),
                    TimeStamp = value["timestamp"].Value<DateTimeOffset>(),
                    DeviceId = device.Id,
                    Name = Name,
                    UnitId = unit.Id
                });
            }

            return result;
        }

        public async Task SaveData<T>(List<T> objects)
        {
            _dataContext.UpdateRange(objects.Cast<Object>());
            await _dataContext.SaveChangesAsync();
        }
    }
}
