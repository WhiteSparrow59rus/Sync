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
    public class CalibrationSync
    {
        private readonly DataContext _dataContext;
        private readonly ClientLuch _clientLuch;
        private readonly ClientMilur _clientMilur;

        public CalibrationSync(DataContext dataContext, ClientLuch clientLuch, ClientMilur clientMilur)
        {
            _dataContext = dataContext;
            _clientLuch = clientLuch;
            _clientMilur = clientMilur;
        }

        /// <summary>
        /// Синхронизирует инициирющие показания, получая на вход данные с бекэнда об устройствах.
        /// </summary>
        /// <param name="channelsSynced">Коллекция каналов.</param>
        /// <param name="devicesBackEnd">Json массив устройств с бека.</param>
        /// <returns></returns>
        public async Task<List<Calibration>> SynchronizeWithBackendData(List<Channel> channelsSynced, JArray devicesBackEnd)
        {
            var dataToSave = MapData(channelsSynced, devicesBackEnd).Result;
            await SaveData(dataToSave);
            return dataToSave;
        }

        /// <summary>
        /// Получает инициирующие показания по устройству с BackEnd`a.
        /// </summary>
        /// <param name="data">Json массив устройств с каналами</param>
        /// <param name="startDate">Начальная дата</param>
        /// <param name="finishDate">Конечная дата</param>
        /// <returns></returns>
        public JArray GetBackEndData(JArray data, string system)
        {
            switch (system)
            {
                case Constants.Luch:
                {
                    _clientLuch.GetCalibrationsByDevices(data);
                        break;
                }
                case Constants.Milur:
                {
                    _clientMilur.GetCalibrationsByDevices(data);
                        break;
                }
            }
            
            return data;
        }

        /// <summary>
        /// Мапим инициирующие показания.
        /// </summary>
        /// <param name="channelsFrontEnd">Список каналов</param>
        /// <param name="dataBackEnd">Json массив устройств с каналами</param>
        /// <returns></returns>
        public async Task<List<Calibration>> MapData(List<Channel> channelsFrontEnd, JArray dataBackEnd)
        {
            var result = new List<Calibration>();

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
                .Select(x => new
                {
                    Channel = x
                })
                .ToListAsync();

                if (channels != null)
                {
                    var units = await _dataContext.Units.ToListAsync();

                    var calibrations = await _dataContext.Calibrations.Where(x => channels.Select(y => y.Channel.Id).Contains(x.Channel.Id)).ToListAsync();

                    if (dataBackEnd.HasValues)
                    {
                        foreach (var deviceBackEnd in dataBackEnd)
                        {
                            foreach (var channelsBackEnd in deviceBackEnd["channels"].Where(x => filteredChannels.Select(y => y.Name).Contains(x["name"].Value<string>())))
                            {
                                var channelFrontEnd = channels.FirstOrDefault(x => x.Channel.BackEndId == channelsBackEnd["id"].Value<int>())?.Channel;

                                var channelCalibrations = calibrations.Where(x => x.Channel.Id == channelFrontEnd.Id);

                                var selectedReadings = deviceBackEnd["readings"].Where(x => channelFrontEnd != null && x["channel"].Value<int>() == channelNamePropMapping[channelFrontEnd.Name]).ToList();
                                if (selectedReadings.Any())
                                {
                                    var lastReadingBackEnd = selectedReadings.OrderByDescending(x => x["initTimestamp"].Value<DateTimeOffset>()).First();
                                    var selectedCalibration = channelCalibrations.FirstOrDefault(x =>
                                        x.TimeStampInput == lastReadingBackEnd["initTimestamp"].Value<DateTimeOffset>());

                                    var selectedToUpdate = selectedReadings.FirstOrDefault(x => !channelCalibrations
                                        .Select(y => y.TimeStampEvent).Contains(x["eventTimestamp"].Value<DateTimeOffset>()));

                                    var unit = units.FirstOrDefault(x => x.ShortNameBackEnd == lastReadingBackEnd["unit"].Value<string>());
                                    
                                    result.Add(UpdateValues(selectedCalibration, channelFrontEnd, unit, selectedToUpdate));
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Обновляет/создает инициирующее показание.
        /// </summary>
        /// <param name="initValue"></param>
        /// <param name="channel"></param>
        /// <param name="unit"></param>
        /// <param name="backendData"></param>
        /// <returns></returns>
        private Calibration UpdateValues(Calibration calibration, Channel channel, Unit unit, JToken backendData)
        {
            if (calibration == null)
            {
                calibration = new Calibration()
                {
                    Id = new Guid(),
                    TimeStampInput = backendData["initTimestamp"].Value<DateTimeOffset>(),
                    UnitId = unit.Id,
                    ChannelId = channel.Id,
                    TimeStampEvent = backendData["eventTimestamp"].Value<DateTimeOffset>(),
                    ImpulseCoefficient = backendData["impulseValue"].Value<decimal>(),
                    // Пропало поле.
                    InitialImpulse = 0,
                    InitialValue = backendData["readingValue"].Value<decimal>()
                };

                _dataContext.Calibrations.AddAsync(calibration);
            }

            return calibration;
        }

        public async Task SaveData<T>(List<T> objects)
        {
            
            await _dataContext.SaveChangesAsync();
        }
    }
}
