﻿using System;
using System.Threading;
using ColossalFramework.Plugins;
using Lidgren.Network;

namespace Tango.Networking
{
    public class Server : IDisposable
    {
        #region Setup
        private static Server _serverInstance;
        public static Server Instance => _serverInstance ?? (_serverInstance = new Server());
        #endregion

        private NetServer _netServer;
        private NetPeerConfiguration _natPeerConfiguration;
        private ParameterizedThreadStart _pts;
        private Thread _messageProcessingThread;
        private bool _isDisposed;

        /// <summary>
        /// Is the server currently running
        /// </summary>
        public bool IsServerStarted { get; private set; }

        // ReSharper disable once MemberCanBePrivate.Global
        public Server()
        {
            _pts = ProcessMessage;
            _messageProcessingThread = new Thread(_pts);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="password"></param>
        public void StartServer(int port = 4230, string password = "")
        {
            // Server already started
            if (IsServerStarted)
                return;

            TangoMod.Log(PluginManager.MessageType.Message, $"Starting server on port {port}...");

            _natPeerConfiguration = new NetPeerConfiguration("Tango")
            {
                Port = port,
                AutoFlushSendQueue = false
            };

            _natPeerConfiguration.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

            _netServer = new NetServer(_natPeerConfiguration);
            _netServer.Start();

            if (_netServer.Status == NetPeerStatus.Running)
            {
                IsServerStarted = true;

                _messageProcessingThread = new Thread(_pts);
                _messageProcessingThread.Start(_netServer);

                TangoMod.Log(PluginManager.MessageType.Message, "Server started.");
            }
            else
            {
                TangoMod.Log(PluginManager.MessageType.Message, "Server not started...");
            }
        }

        /// <summary>
        /// Stop the server
        /// </summary>
        public void StopServer()
        {
            // Only shutdown server if it is
            // all ready running.
            if (!IsServerStarted)
                return;

            TangoMod.Log(PluginManager.MessageType.Message, "Stopping server...");

            try
            {
                _netServer.Shutdown("Server Shutting Down...");
            }
            finally
            {
                IsServerStarted = false;
                _serverInstance = null;
            }
        }

        private void ProcessMessage(object obj)
        {
            try
            {
                _netServer = (NetServer)obj;

                TangoMod.Log(PluginManager.MessageType.Message, "Started server processing thread.");

                while (IsServerStarted)
                {
                    NetIncomingMessage msg;
                    while ((msg = _netServer.ReadMessage()) != null)
                    {
                        switch (msg.MessageType)
                        {
                            case NetIncomingMessageType.ConnectionApproval:
                                msg.SenderConnection.Approve();
                                break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                TangoMod.Log(PluginManager.MessageType.Error, "Server thread crashes: " + e.Message);
            }
            finally
            {
                TangoMod.Log(PluginManager.MessageType.Message, "Server thread stopped.");
            }
        }

        public void Dispose()
        {
            StopServer();

            if (!_isDisposed)
            {
                GC.SuppressFinalize(this);
                _isDisposed = true;
            }
        }

        ~Server()
        {
            Dispose();
        }
    }
}
