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
    public class CalcValueDeviceSync
    {
        private readonly DataContext _dataContext;

        public CalcValueDeviceSync(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        /// <summary>
        ///     Синхронизирует каналы, получая на вход данные с бекэнда об устройствах.
        /// </summary>
        /// <param name="initValues">Коллекция инициирующих показаний.</param>
        /// <param name="calculatedChannelsSynced">Коллекция расчетных каналов.</param>
        /// <param name="devicesBackEnd">Json массив устройств с бека.</param>
        /// <returns></returns>
        public async Task<List<Indication>> SynchronizeWithBackendData(List<Calibration> initValues,
            List<Channel> calculatedChannelsSynced, JArray devicesBackEnd)
        {
            var dataToSave = MapData(initValues, calculatedChannelsSynced, devicesBackEnd).Result;
            await SaveData(dataToSave);
            return dataToSave;
        }

        /// <summary>
        ///     Мапим показания.
        /// </summary>
        /// <param name="initValues">Коллекция инициирующих показаний.</param>
        /// <param name="calculatedChannelsSynced">Коллекция расчетных каналов.</param>
        /// <param name="dataBackEnd">Json массив устройств с каналами</param>
        /// <returns></returns>
        public async Task<List<Indication>> MapData(List<Calibration> calibrations,
            List<Channel> calculatedChannelsSynced, JArray dataBackEnd)
        {
            var result = new List<Indication>();
            var dates = new List<DateTimeOffset>();

            // Здесь не маппим профили и мгновенные - их читаем отдельно.
            var channelFilter = new[]
            {
                "impulse_counter_ch1",
                "impulse_counter_ch2",
                "impulse_counter_ch3",
                "impulse_counter_ch4"
            };

            foreach (var deviceBackEnd in dataBackEnd)
            foreach (var channelBackEnd in deviceBackEnd["channels"]
                .Where(x => channelFilter.Contains(x["name"].Value<string>())))
            foreach (var value in channelBackEnd["values"])
                dates.Add(value["timestamp"].Value<DateTimeOffset>());

            dates.Sort();
            var minDate = dates.FirstOrDefault(x => x > new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero)
                                                    && x < new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero));
            var maxDate = dates.LastOrDefault(x => x > new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero)
                                                   && x < new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero));

            var calcValues = await _dataContext.Indications.Where(x =>
                    calculatedChannelsSynced.Select(y => y.Id).Contains(x.Channel.Id) &&
                    x.TimeStamp >= minDate.AddDays(-1) && x.TimeStamp <= maxDate.AddDays(1))
                .Select(x => new
                {
                    ChannelId = x.Channel.Id,
                    x.TimeStamp
                })
                .AsNoTracking()
                .ToListAsync();

            if (dataBackEnd.HasValues)
                foreach (var deviceBackEnd in dataBackEnd)
                foreach (var channelBackEnd in deviceBackEnd["channels"]
                    .Where(x => channelFilter.Contains(x["name"].Value<string>())))
                {
                    var calculatedChannelSynced = calculatedChannelsSynced.FirstOrDefault(x =>
                        x.PhysicalChannel.BackEndId == channelBackEnd["id"].Value<int>());

                    var channelCalcValues = calcValues.Where(x => x.ChannelId == calculatedChannelSynced.Id);

                    if (calculatedChannelSynced != null && channelBackEnd["unit"].Value<string>() == "imp")
                    {
                        var lastCalibration = calibrations.OrderByDescending(x => x.TimeStampEvent)
                            .FirstOrDefault(x => x.Channel.BackEndId == channelBackEnd["id"].Value<int>());

                        if (lastCalibration != null)
                        {
                            var selectedCalcValues = channelBackEnd["values"].Where(x =>
                                !channelCalcValues.Select(y => y.TimeStamp)
                                    .Contains(x["timestamp"].Value<DateTimeOffset>()));

                            foreach (var value in selectedCalcValues)
                                if (lastCalibration.TimeStampEvent <
                                    value["timestamp"].Value<DateTimeOffset>().ToLocalTime())
                                    result.Add(UpdateValues(null, lastCalibration, calculatedChannelSynced, value));
                        }
                    }
                }

            return result;
        }

        /// <summary>
        ///     Обновляет/создает показание.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="initValue"></param>
        /// <param name="channel"></param>
        /// <param name="backendData"></param>
        /// <returns></returns>
        private Indication UpdateValues(Indication value, Calibration initValue, Channel channel, JToken backendData)
        {
            if (value == null)
            {
                value = new Indication
                {
                    Id = new Guid(),
                    isReliable = backendData["isReliable"].Value<bool>(),
                    ChannelId = channel.Id,
                    TimeStamp = backendData["timestamp"].Value<DateTimeOffset>(),
                    Value = initValue.InitialValue +
                            (backendData["value"].Value<decimal>() - initValue.InitialImpulse) *
                            initValue.ImpulseCoefficient
                };
            }
            else
            {
                if (!value.isReliable)
                    if (backendData["isReliable"].Value<bool>())
                    {
                        value.isReliable = backendData["isReliable"].Value<bool>();
                        value.ChannelId = channel.Id;
                        value.TimeStamp = backendData["timestamp"].Value<DateTimeOffset>();
                        value.Value = initValue.InitialValue +
                                      (backendData["value"].Value<decimal>() - initValue.InitialImpulse) *
                                      initValue.ImpulseCoefficient;
                    }
            }

            return value;
        }

        public async Task SaveData<T>(List<T> objects)
        {
            _dataContext.UpdateRange(objects.Cast<object>());
            await _dataContext.SaveChangesAsync();
        }
    }
}