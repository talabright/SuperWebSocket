﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using SuperSocket.Common;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using SuperSocket.SocketEngine;
using SuperSocket.SocketEngine.Configuration;

namespace SuperWebSocketWorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private IBootstrap m_Bootstrap;

        public override void Run()
        {
            // This is a sample worker implementation. Replace with your logic.
            Trace.WriteLine("SuperWebSocketWorkerRole entry point called", "Information");

            while (true)
            {
                Thread.Sleep(10000);
                Trace.WriteLine("Working", "Information");
            }
        }

        public override bool OnStart()
        {
            m_Bootstrap = BootstrapFactory.CreateBootstrap();

            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            var serverConfig = ConfigurationManager.GetSection("socketServer") as SocketServiceConfig;

            if (!m_Bootstrap.Initialize(ResolveServerConfig))
            {
                Trace.WriteLine("Failed to initialize SuperWebSocket!", "Error");
                return false;
            }

            var result = m_Bootstrap.Start();

            switch (result)
            {
                case (StartResult.None):
                    Trace.WriteLine("No server is configured, please check you configuration!");
                    return false;

                case (StartResult.Success):
                    Trace.WriteLine("The server has been started!");
                    break;

                case (StartResult.Failed):
                    Trace.WriteLine("Failed to start SuperWebSocket server! Please check error log for more information!");
                    return false;

                case (StartResult.PartialSuccess):
                    Trace.WriteLine("Some server instances were started successfully, but the others failed to start! Please check error log for more information!");
                    break;
            }

            return base.OnStart();
        }

        private IServerConfig ResolveServerConfig(IServerConfig serverConfig)
        {
            var config = new ServerConfig();
            serverConfig.CopyPropertiesTo(config);

            if (serverConfig.Port > 0)
            {
                var endPointKey = serverConfig.Name + "_" + serverConfig.Port;
                var instanceEndpoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints[endPointKey];
                if (instanceEndpoint == null)
                {
                    Trace.WriteLine(string.Format("Failed to find Input Endpoint configuration {0}!", endPointKey), "Error");
                    return serverConfig;
                }

                var ipEndpoint = instanceEndpoint.IPEndpoint;
                config.Ip = ipEndpoint.Address.ToString();
                config.Port = ipEndpoint.Port;
            }

            if (config.Listeners != null && config.Listeners.Any())
            {
                var listeners = config.Listeners.ToArray();

                var newListeners = new List<ListenerConfig>(listeners.Length);

                for (var i = 0; i < listeners.Length; i++)
                {
                    var listener = listeners[i];

                    var endPointKey = serverConfig.Name + "_" + listener.Port;
                    var instanceEndpoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints[endPointKey];
                    if (instanceEndpoint == null)
                    {
                        Trace.WriteLine(string.Format("Failed to find Input Endpoint configuration {0}!", endPointKey), "Error");
                        return serverConfig;
                    }

                    var newListener = new ListenerConfig();
                    newListener.Ip = instanceEndpoint.IPEndpoint.Address.ToString();
                    newListener.Port = instanceEndpoint.IPEndpoint.Port;
                    newListener.Backlog = listener.Backlog;

                    newListeners.Add(newListener);
                }

                config.Listeners = newListeners;
            }

            return config;
        }

        public override void OnStop()
        {
            if (m_Bootstrap != null)
                m_Bootstrap.Stop();

            base.OnStop();
        }
    }
}