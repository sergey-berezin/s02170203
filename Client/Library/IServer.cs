
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Library
{
    public interface IServer
    {
        public event Action Connected;
        public event Action Disconnected;
        public event Action<Prediction> Result;

        public ServerConnectionState State { get; }

        public Task StartAsync(System.Net.Http.StringContent data);
        public Task StopAsync();
        public Task ClearAsync();
        public Task<List<Contracts.Recognition>> LoadAsync();
        public Task SaveAsync();
    }
}
