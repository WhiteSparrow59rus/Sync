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
    public class PowerProfileSync
    {
        private readonly DataContext _dataContext;

        public PowerProfileSync(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        /// <summary>
        /// Синхронизирует профиль устройства, получая на вход данные с бекэнда об устройствах.
        /// </summary>
        /// <param name="channelsSynced">Коллекция каналов.</param>
        /// <param name="devicesBackEnd">Данные об устройствах с бекэнда.</param>
        /// <param name="system"></param>
        /// <returns></returns>
        public async Task<List<PowerProfile>> SynchronizeWithBackendData(List<Channel> channelsSynced, JArray devicesBackEnd)
        {
            var dataToSave = MapData(channelsSynced, devicesBackEnd).Result;
            await SaveData(dataToSave);
            return dataToSave;
        }

        /// <summary>
        /// Мапим показания.
        /// </summary>
        /// <param name="channelsFrontEnd">Список каналов</param>
        /// <param name="dataBackEnd">Json массив устройств с каналами</param>
        /// <returns></returns>
        public async Task<List<PowerProfile>> MapData(List<Channel> channelsFrontEnd, JArray dataBackEnd)
        {
            var result = new List<PowerProfile>();

            // Наименования каналов с профилями.
            var channelNames = new string[]
            {
                "electro_ac_m_profile60m_lsum_tsum",
                "electro_ac_p_profile60m_lsum_tsum",
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

                var filteredChannels = channelsFrontEnd.Where(x => channelNames.Contains(x.Name));

                var powerProfiles = await _dataContext.PowerProfiles.Where(x =>
                        filteredChannels.Select(y => y.Id).Contains(x.Channel.Id) &&
                        (x.TimeStamp >= minDate.AddDays(-1) && x.TimeStamp <= maxDate.AddDays(1)))
                    .Select(x => new 
                    {
                        ChannelId = x.Channel.Id,
                        TimeStamp = x.TimeStamp,
                        ChannelBackEndId = x.Channel.BackEndId
                    })
                    .AsNoTracking()
                    .ToListAsync();

                if (dataBackEnd.HasValues)
                {
                    foreach (var deviceBackEnd in dataBackEnd)
                    {
                        foreach (var channelBackEnd in deviceBackEnd["channels"]
                            .Where(x => channelNames.Contains(x["name"].Value<string>())))
                        {
                            var channelFrontEnd = channelsFrontEnd
                                .FirstOrDefault(x => x.BackEndId == channelBackEnd["id"].Value<int>());

                            var channelPowerProfiles = powerProfiles.Where(x => x.ChannelId == channelFrontEnd.Id);

                            var selectedInstantValues = channelBackEnd["values"].AsParallel()
                                .Where(x => !channelPowerProfiles
                                    .Any(y => y.TimeStamp == x["timestamp"].Value<DateTimeOffset>() && 
                                              y.ChannelBackEndId == channelBackEnd["id"].Value<int>()));

                            result.AddRange(UpdateDeviceProfile
                                (
                                    channelFrontEnd,
                                    selectedInstantValues
                                )
                            );
                        }
                    }
                }
            }

            return result;
        }

        private List<PowerProfile> UpdateDeviceProfile(Channel channel, IEnumerable<JToken> values)
        {
            var result = new List<PowerProfile>();

            foreach (var value in values)
            {
                result.Add(new PowerProfile
                {
                    Id = new Guid(),
                    ChannelId = channel.Id,
                    Value = value["value"].Value<decimal>(),
                    TimeStamp = value["timestamp"].Value<DateTimeOffset>()
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
