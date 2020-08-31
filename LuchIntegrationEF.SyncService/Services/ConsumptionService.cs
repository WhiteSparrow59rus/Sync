using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using LuchIntegrationEF.Objects.Custom.Entities;
using LuchIntegrationEF.SyncService.Data;
using LuchIntegrationEF.SyncService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LuchIntegrationEF.SyncService.Services
{
    public class ConsumptionService : IConsumptionService
    {
        private readonly DataContext _dataContext;

        public ConsumptionService(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task RefreshAllAsync()
        {
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
                "impulse_counter_ch1_Water"
            };
            var currentTimeStamp = DateTimeOffset.Now;
            var sixMonthsAgoTimeStamp = new DateTimeOffset(currentTimeStamp.Year, currentTimeStamp.Month, 1,0,0,0, TimeSpan.Zero).AddMonths(-6);

            // ToDo: Сделать вычитку не всей БД =)).
            var devices = await _dataContext.Devices
                .Include(x => x.Channels).ThenInclude(x => x.Indications)
                .Include(x => x.Channels).ThenInclude(x => x.Consumptions)
                .Where(x => x.TimeStampCalc < x.TimeStampSync || x.TimeStampCalc == null)
                .ToListAsync();

            foreach (var device in devices)
            {
                var consumptionsList = new List<Consumption>();
                foreach (var channel in device.Channels)
                {
                    var sortedIndications = channel.Indications.OrderByDescending(x => x.TimeStamp).ToList();

                    var valuesGroupByMonth = sortedIndications
                        .GroupBy(x => new { x.TimeStamp.Month, x.TimeStamp.Year })
                        .OrderBy(x => x.Key.Month).ThenBy(x => x.Key.Year).ToList();

                    Indication previousIndication = null;
                    foreach (var itemGroup in valuesGroupByMonth)
                    {
                        var maxDateValues = itemGroup.FirstOrDefault(x => x.TimeStamp == itemGroup.Max(y => y.TimeStamp));
                        var minDateValues = itemGroup.FirstOrDefault(x => x.TimeStamp == itemGroup.Min(y => y.TimeStamp));
                        var consumptionByMonth = channel.Consumptions.FirstOrDefault(x =>
                            x.TimeStamp.Month == itemGroup.Key.Month &&
                            x.TimeStamp.Year == itemGroup.Key.Year);

                        var consumptionTimeStamp = new DateTimeOffset();
                        if (device.System == "Milur")
                        {
                            // Костыль из-за того, что с милура приходят корявые данные.(время на сутки впереди)
                            consumptionTimeStamp = maxDateValues.TimeStamp.Date;
                        }
                        else
                        {
                            consumptionTimeStamp = maxDateValues.TimeStamp;
                        }
                        

                        if (previousIndication == null)
                        {

                        }
                        else
                        {
                            if (maxDateValues != null && maxDateValues.TimeStamp.Month - previousIndication.TimeStamp.Month <= 1)
                            {
                                if (consumptionByMonth != null)
                                {
                                    consumptionByMonth.Value = maxDateValues.Value - previousIndication.Value;
                                    consumptionByMonth.TimeStamp = consumptionTimeStamp;
                                    consumptionByMonth.CalculationTimeStamp = DateTimeOffset.Now;
                                    consumptionByMonth.IndicationFinishId = maxDateValues.Id;
                                    consumptionByMonth.IndicationStartId = previousIndication.Id;
                                }
                                else
                                {
                                    _dataContext.Consumptions.Add(new Consumption
                                    {
                                        Value = maxDateValues.Value - previousIndication.Value,
                                        TimeStamp = consumptionTimeStamp,
                                        CalculationTimeStamp = DateTimeOffset.Now,
                                        IndicationFinishId = maxDateValues.Id,
                                        IndicationStartId = previousIndication.Id,
                                        ChannelId = channel.Id
                                    });
                                }
                            }
                            else
                            {
                                if (consumptionByMonth != null)
                                {
                                    consumptionByMonth.Value = maxDateValues.Value - minDateValues.Value;
                                    consumptionByMonth.TimeStamp = consumptionTimeStamp;
                                    consumptionByMonth.CalculationTimeStamp = DateTimeOffset.Now;
                                    consumptionByMonth.IndicationFinishId = maxDateValues.Id;
                                    consumptionByMonth.IndicationStartId = minDateValues.Id;
                                }
                                else
                                {
                                    _dataContext.Consumptions.Add(new Consumption
                                    {
                                        Value = maxDateValues.Value - minDateValues.Value,
                                        TimeStamp = consumptionTimeStamp,
                                        CalculationTimeStamp = DateTimeOffset.Now,
                                        IndicationFinishId = maxDateValues.Id,
                                        IndicationStartId = minDateValues.Id,
                                        ChannelId = channel.Id
                                    });
                                }
                            }
                        }

                        previousIndication = maxDateValues;
                    }
                }
                device.TimeStampCalc = DateTimeOffset.Now;
            }
            await _dataContext.SaveChangesAsync();
        }
    }
}
