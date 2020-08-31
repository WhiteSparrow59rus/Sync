using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using LuchIntegrationEF.SyncService.Contracts.V1.Requests.Queries;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LuchIntegrationEF.SyncService.HttpClients
{
    public class ClientLuch
    {
        private readonly IConfiguration _configuration;
        public HttpClient Client { get; set; }

        public ClientLuch(HttpClient client, IConfiguration configuration)
        {
            _configuration = configuration;
            client.BaseAddress = new Uri(_configuration.GetValue<string>("LuchBackend:BackEndApiEndpointLuch"));
            client.DefaultRequestHeaders.Add("Connection", "close");
            Client = client;
        }

        /// <summary>
        /// Десериализует json.
        /// </summary>
        /// <param name="data">Строка Json</param>
        /// <returns></returns>
        private JArray DeserializeJArray(string data)
        {
            var jsonReader = new JsonTextReader(new StringReader(data))
                {DateParseHandling = DateParseHandling.DateTimeOffset};
            return JArray.Load(jsonReader);
        }

        public async Task<JArray> GetDevicesByPage(DateRangeQuery dateRangeQuery, int page)
        {
            JArray result = null;

            var startDateStr = dateRangeQuery.From.ToUniversalTime().ToString("o");
            var requestString =
                $"devices?since={startDateStr}&perPage={_configuration.GetValue<string>("LuchBackend:DevicesPerPage")}&page={page}";

            var response = await Client.GetAsync(requestString);

            if (response.IsSuccessStatusCode) result = DeserializeJArray(await response.Content.ReadAsStringAsync());

            return result;
        }

        public JArray GetChannelsByDevices(JArray data)
        {
            Parallel.ForEach(data, deviceBackEnd =>
            {
                var response = Client.GetAsync($"devices/{deviceBackEnd["id"].Value<int>()}/channels").Result;

                if (response.IsSuccessStatusCode)
                    deviceBackEnd["channels"] = DeserializeJArray(response.Content.ReadAsStringAsync().Result);
            });

            return data;
        }

        public JArray GetIndicationsByChannels(JArray data, DateRangeQuery dateRangeQuery)
        {
            // Приводим дату к UTC.
            var startDateStr = dateRangeQuery.From.ToUniversalTime().ToString("o");
            var finishDateStr = dateRangeQuery.To.ToUniversalTime().ToString("o");

            Parallel.ForEach(data, deviceBackEnd =>
            {
                foreach (var channel in deviceBackEnd["channels"])
                {
                    var deviceId = deviceBackEnd["id"].Value<string>();
                    var channelId = channel["id"].Value<string>();

                    var response = Client
                        .GetAsync(
                            $"devices/{deviceId}/channels/{channelId}/values?from={startDateStr}&to={finishDateStr}&perPage={_configuration.GetValue<int>("LuchBackend:ValuesPerPage")}",
                            default(CancellationToken)).Result;
                    if (response.IsSuccessStatusCode)
                        channel["values"] = DeserializeJArray(response.Content.ReadAsStringAsync().Result);
                }
            });

            return data;
        }

        public JArray GetEventsByDevices(JArray data, DateRangeQuery dateRangeQuery)
        {
            // Приводим дату к UTC.
            var startDateStr = dateRangeQuery.From.ToUniversalTime().ToString("o");
            var finishDateStr = dateRangeQuery.To.ToUniversalTime().ToString("o");

            var resultChannelsBackEnd = new JArray();

            // В цикле устройствам запрашиваем события с Back End`a и добавляем их в устройство.
            Parallel.ForEach(data, deviceBackEnd =>
            {
                var response = Client
                    .GetAsync(
                        $"devices/{deviceBackEnd["id"].Value<int>()}/events?from={startDateStr}&to={finishDateStr}&perPage={_configuration.GetValue<int>("LuchBackend:ValuesPerPage")}",
                        default(CancellationToken)).Result;

                if (response.IsSuccessStatusCode)
                    deviceBackEnd["events"] = DeserializeJArray(response.Content.ReadAsStringAsync().Result);
                else
                    deviceBackEnd["events"] = new JArray();
            });

            return data;
        }

        public JArray GetCalibrationsByDevices(JArray data)
        {
            Parallel.ForEach(data,
                devicesBackEnd =>
                {
                    var response = Client.GetAsync($"devices/{devicesBackEnd["id"].Value<int>()}/readings").Result;

                    if (response.IsSuccessStatusCode)
                        devicesBackEnd["readings"] = DeserializeJArray(response.Content.ReadAsStringAsync().Result);
                });

            return data;
        }

        public JArray GetDictionary()
        {
            JArray result = null;

            var response = Client.GetAsync("dictionaries/all").Result;

            if (response.IsSuccessStatusCode)
                result = JArray.Parse("[" + response.Content.ReadAsStringAsync().Result + "]");

            return result;
        }

        public JObject GetCommandById(string backEndId)
        {
            var response = Client.GetAsync($"commands/{backEndId}").Result;

            if (response.IsSuccessStatusCode)
                return JObject.Parse(response.Content.ReadAsStringAsync().Result);

            return null;
        }
    }
}