using IPA;
using IPALogger = IPA.Logging.Logger;

using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using TMPro;
using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using System.Net;
using System.Net.Sockets;
using System.Text;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace SquatCounter
{
    public class Config
    {
        public static Config Instance { get; set; }
        public virtual long Timestamp { get; set; } = 0;
        public virtual float StandingThreshold { get; set; } = 0.2f;
        public virtual float SquatThreshold { get; set; } = 0.0f;
    }

    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }

        float wallHeight = 1.1f;
        int squatCount = 0;
        WebSocketServer wsserv;

        [Init]
        public Plugin(IPALogger logger, IPA.Config.Config config)
        {
            Instance = this;
            Log = logger;
            Config.Instance = config.Generated<Config>();
            wsserv = new WebSocketServer();
        }

        [OnStart]
        public void OnApplicationStart()
        {
            Plugin.Log.Info("meow");
            new Thread(new ThreadStart(SquatCounterThread)).Start();
            Harmony harmony = new Harmony("Catse.BeatSaber.SquatCounter");
            harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
            if(DateTimeOffset.Now.ToUnixTimeSeconds() > Config.Instance.Timestamp + 3600)
                File.WriteAllText("UserData\\SquatCount.txt", "0");
            squatCount = Convert.ToInt32(File.ReadAllText("UserData\\SquatCount.txt"));
        }

        [OnExit]
        public void OnApplicationQuit()
        {

        }

        [HarmonyPatch(typeof(PlayerHeightSettingsController), nameof(PlayerHeightSettingsController.RefreshUI))]
        class Patch1
        {
            static void Postfix(ref TextMeshProUGUI ____text, ref float ____value)
            {
                Plugin.Instance.wallHeight = Mathf.Max(1.1f, 1.1f + ((____value - 1.4f) * 0.5f));
            }
        }

        [HarmonyPatch(typeof(LevelStatsView), "ShowStats")]
        class Patch2
        {
            static void Postfix()
            {
                Config.Instance.Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            }
        }

        void SquatCounterThread()
        {
            while (true)
            {
                var inputDevices = new List<UnityEngine.XR.InputDevice>();
                UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.Head, inputDevices);
                if (inputDevices.Count == 1)
                {
                    bool squatting = true;
                    while (inputDevices[0].isValid)
                    {
                        inputDevices[0].TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 pos);
                        inputDevices[0].TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion ang);
                        float headY = pos.y + (ang * Vector3.forward * -0.1f).y;
                        if (headY > (wallHeight+Config.Instance.StandingThreshold))
                            squatting = false;
                        if (headY < (wallHeight+Config.Instance.SquatThreshold) && !squatting)
                        {
                            squatting = true;
                            squatCount++;
                            wsserv.BroadcastMessage("" + squatCount);
                            File.WriteAllText("UserData\\SquatCount.txt", "" + squatCount);
                        }
                        Thread.Sleep(11);
                    }
                }
                Thread.Sleep(1000);
            }
        }


    }

    public class WebSocketServer
    {
        TcpClient[] clients = new TcpClient[8];
        int clientCount = 0;
        public WebSocketServer()
        {
            new Thread(new ThreadStart(AcceptClientsThread)).Start();
        }
        private void AcceptClientsThread()
        {
            TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 2350);
            server.Start();
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                new Thread(() => ClientThread(client)).Start();
            }
        }
        public void BroadcastMessage(string payload)
        {
            Byte[] msg = new byte[2 + payload.Length];
            msg[0] = 0x81;
            msg[1] = (byte)payload.Length;
            Encoding.UTF8.GetBytes(payload).CopyTo(msg, 2);
            int i = 0;
            while (i < clientCount)
            {
                try
                {
                    clients[i].GetStream().Write(msg, 0, msg.Length);
                    i++;
                }
                catch (Exception e)
                {
                    clientCount--;
                    clients[i] = clients[clientCount];
                }
            }
        }
        private void ClientThread(TcpClient client)
        {
            if (clientCount == clients.Length)
                return;
            clients[clientCount] = client;
            clientCount++;
            try
            {
                NetworkStream stream = client.GetStream();
                while (true)
                {
                    while (!stream.DataAvailable && client.Connected) ;
                    Byte[] bytes = new Byte[client.Available];
                    stream.Read(bytes, 0, bytes.Length);
                    String data = Encoding.UTF8.GetString(bytes);
                    if (data.StartsWith("GET /squatcount"))
                    {
                        int i1 = data.ToLower().IndexOf("sec-websocket-key") + 19;
                        int i2 = data.IndexOf("\r", i1);
                        string key = data.Substring(i1, i2 - i1);
                        Byte[] response = Encoding.UTF8.GetBytes(
                            "HTTP/1.1 101 Switching Protocols\r\n" +
                            "Connection: Upgrade\r\n" +
                            "Upgrade: websocket\r\n" +
                            "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                                 System.Security.Cryptography.SHA1.Create().ComputeHash(
                                    Encoding.UTF8.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"))) + "\r\n" +
                            "\r\n"
                        );
                        stream.Write(response, 0, response.Length);
                    }
                    else
                    {
                        if (bytes[0] == 0x89)
                        {
                            bytes[0] = 0x8A;
                            stream.Write(bytes, 0, bytes.Length);
                        }
                    }
                }
            }
            catch (Exception e) { }
        }
    }
}
