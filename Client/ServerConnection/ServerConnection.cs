
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Contracts;
using Library;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;

namespace ServerConnection
{
    public class Server : IServer
    {
        public event Action Connected;
        public event Action Disconnected;
        public event Action<Prediction> Result;

        public ServerConnectionState State { get; private set; } = ServerConnectionState.Disconnected;

//===========================================================================================//

        private HttpClient client = null;
        private HubConnection connection = null;

//===========================================================================================//
        public Server()
        {
            client = new HttpClient();
            connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/recognitionhub")
                .Build();
            connection.On<string, string>("RealTimeAdd", (title, path) =>
            {
                Result(new Prediction
                {
                    Path = path,
                    Title = title,
                    Probability = 1
                });
            });
            connection.On("ServerIsReady", () => ConnectToserver());
            Task.Run(() => ConnectToserver());
            connection.Closed += ReconnectToserver;
        }

//===========================================================================================//      
        public async Task StartAsync(StringContent data)
        {           
            var url = "http://localhost:5000/recognition/start/";
            await client.PostAsync(url, data);
        }
        public async Task StopAsync()
        {
            var url = "http://localhost:5000/recognition/stop/";
            await client.PostAsync(url, null);
        }
        public async Task ClearAsync()
        {
            var url = "http://localhost:5000/recognition/clear/";
            await client.PutAsync(url, null);
        }
        public async Task<List<Recognition>> LoadAsync()
        {
            using var ans = await client.GetAsync("http://localhost:5000/recognition/load");
            return JsonConvert.DeserializeObject<List<Recognition>>(await ans.Content.ReadAsStringAsync());
        }
        public async Task SaveAsync()
        {
            var url = "http://localhost:5000/recognition/save/";
            await client.PutAsync(url, null);
        }

//===========================================================================================//
        private async Task ConnectToserver(Exception ex = null)
        {
            await Task.Run(async () =>
            {
                while (connection.State != HubConnectionState.Connected)
                {
                    await Task.Delay(2000);
                    try
                    {
                        await connection.StartAsync();
                    }
                    catch { }
                }
                State = ServerConnectionState.Connected;
                Connected();
            });
        }
        private async Task ReconnectToserver(Exception ex = null)
        {           
            await Task.Run(async () =>
            {
                State = ServerConnectionState.Disconnected;
                Disconnected();
                await ConnectToserver();
            });
        }

    }
}
