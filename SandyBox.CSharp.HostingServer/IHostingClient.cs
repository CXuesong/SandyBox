﻿using System.Threading.Tasks;
using JsonRpc.Standard.Contracts;

namespace SandyBox.CSharp.HostingServer
{
    public interface IHostingClient
    {

        [JsonRpcMethod(IsNotification = true)]
        void NotifyStarted();

        [JsonRpcMethod]
        Task<string> GetModuleContent(string moduleName);

    }
}
