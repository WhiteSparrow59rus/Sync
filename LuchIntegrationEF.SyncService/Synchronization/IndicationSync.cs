using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
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
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace LuchIntegrationEF.SyncService.Synchronization
{
    public class IndicationSync
    {
        private readonly DataContext _dataContext;
        private readonly ClientLuch _clientLuch;
        private readonly ClientMilur _clientMilur;
        
        public IndicationSync(DataContext dataContext, ClientLuch clientLuch, ClientMilur clientMilur)
        {
            _dataContext = dataContext;
            _clientLuch = clientLuch;
            _clientMilur = clientMilur;
        }

        /// <summary>
        /// Синхронизирует каналы, получая на вход данные с бекэнда об устройствах.
        /// </summary>
        /// <param name="channelsSynced"></param>
        /// <param name="devicesBackEnd"></param>
        /// <param name="system"></param>
        public async Task<List<Indication>> SynchronizeWithBackendData(List<Channel> channelsSynced, JArray devicesBackEnd)
        {
            var dataToSave = MapData(channelsSynced, devicesBackEnd).Result;
            await SaveData(dataToSave);
            return dataToSave;
        }

        /// <summary>
        /// Получает показания по каналу с BackEnd`a.
        /// </summary>
        /// <param name="data">Json массив устройств с каналами</param>
        /// <returns></returns>
        public JArray GetBackEndData(JArray data, DateRangeQuery dateRangeQuery, string system)
        {
            switch (system)
            {
                case Constants.Luch:
                {
                    _clientLuch.GetIndicationsByChannels(data, dateRangeQuery);
                        break;
                }
                case Constants.Milur:
                {
                    _clientMilur.GetIndicationsByChannels(data, dateRangeQuery);
                        break;
                }
            }
            
            return data;
        }

        /// <summary>
        /// Мапим показания.
        /// </summary>
        /// <param name="dataFrontEnd">Список каналов</param>
        /// <param name="dataBackEnd">Json массив устройств с каналами</param>
        /// <returns></returns>
        public async Task<List<Indication>> MapData(List<Channel> channelsFrontEnd, JArray dataBackEnd)
        {
            var result = new List<Indication>();

            // Фильтруем только по суточным.
            var channelFilter = new string[]
            {
                "electro_ac_m_lsum_t1",
                "electro_ac_m_lsum_t2",
                "electro_ac_m_lsum_t3",
                "electro_ac_m_lsum_t4",
                "electro_ac_m_lsum_tsum",
                "electro_ac_p_lsum_t1",
                "electro_ac_p_lsum_t2",
                "electro_ac_p_lsum_t3",
                "electro_ac_p_lsum_t4",
                "electro_ac_p_lsum_tsum",
                "electro_re_m_lsum_t1",
                "electro_re_m_lsum_t2",
                "electro_re_m_lsum_t3",
                "electro_re_m_lsum_t4",
                "electro_re_m_lsum_tsum",
                "electro_re_p_lsum_t1",
                "electro_re_p_lsum_t2",
                "electro_re_p_lsum_t3",
                "electro_re_p_lsum_t4",
                "electro_re_p_lsum_tsum",
                "impulse_counter_ch1",
                "impulse_counter_ch2",
                "impulse_counter_ch3",
                "impulse_counter_ch4",
            };

            var filteredChannels = channelsFrontEnd.Where(x => channelFilter.Contains(x.Name));

            var dates = new List<DateTimeOffset>();

            foreach (var deviceBackEnd in dataBackEnd)
            {
                foreach (var channelBackEnd in deviceBackEnd["channels"])
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

                var indications = await _dataContext.Indications.Where(x => filteredChannels.Select(y => y.Id).Contains(x.Channel.Id) && (x.TimeStamp >= minDate.AddDays(-1) && x.TimeStamp <= maxDate.AddDays(1))).Select(x => new
                    {
                        Channel = x.Channel.Id,
                        x.TimeStamp
                    })
                    .AsNoTracking()
                    .ToListAsync();

                if (dataBackEnd.HasValues)
                {
                    foreach (var deviceBackEnd in dataBackEnd)
                    {
                        foreach (var channelBackEnd in deviceBackEnd["channels"])
                        {
                            var channelFrontEnd = channelsFrontEnd.AsParallel().FirstOrDefault(x => x.BackEndId == channelBackEnd["id"].Value<int>());
                            
                            if (channelFilter.Contains(channelBackEnd["name"].Value<string>()) && channelBackEnd["values"].HasValues)
                            {
                                var channelIndications = indications.AsParallel().Where(x => x.Channel == channelFrontEnd.Id).ToList();

                                var selectedIndication = channelBackEnd["values"].AsParallel().Where(x =>
                                    !channelIndications.Select(y => y.TimeStamp)
                                        .Contains(x["timestamp"].Value<DateTimeOffset>())
                                    && x["timestamp"].Value<DateTimeOffset>() >
                                    new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero));
                                result.AddRange(selectedIndication.Select(x => UpdateValues(null, channelFrontEnd, x)));
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Обновляет/создает показание.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="channel"></param>
        /// <param name="backendData"></param>
        /// <returns></returns>
        private Indication UpdateValues(Indication value, Channel channel, JToken backendData)
        {
            if (value == null)
            {
                value = new Indication
                {
                    Id = new Guid(),
                    isReliable = backendData["isReliable"].Value<bool>(),
                    ChannelId = channel.Id,
                    TimeStamp = backendData["timestamp"].Value<DateTimeOffset>(),
                    Value = backendData["value"].Value<decimal>(),
                };
            }
            else
            {
                if (!value.isReliable)
                {
                    if (backendData["isReliable"].Value<bool>())
                    {
                        value.isReliable = backendData["isReliable"].Value<bool>();
                        value.ChannelId = channel.Id;
                        value.TimeStamp = backendData["timestamp"].Value<DateTimeOffset>();
                        value.Value = backendData["value"].Value<decimal>();
                    }
                }
            }

            return value;
        }

        public async Task SaveData<T>(List<T> objects)
        {
            _dataContext.DetachAllEntities();
            _dataContext.UpdateRange(objects.Cast<Object>());
            await _dataContext.SaveChangesAsync();
        }
    }
}
