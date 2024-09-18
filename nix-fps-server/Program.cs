using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using Riptide;
using Riptide.Utils;
using System.Diagnostics;

namespace nix_fps_server
{
    class Program
    {
        public static Server Server;
        static NetworkManager networkManager;

        public static uint ServerTPS;
        public static JObject CFG;
        public static long syncErrorMs = 0;
        static void Main(string[] args)
        {
            CFG = JObject.Parse(File.ReadAllText("app-settings.json"));

            RiptideLogger.Initialize(Console.WriteLine, false);
            networkManager = new NetworkManager();
            Server = new Server();
            Server.Start(7777, 8);
            Server.ClientConnected += (s, e) => NetworkManager.HandleConnect(e.Client.Id);
            Server.ClientDisconnected += (s, e) => NetworkManager.HandleDisconnect(e.Client.Id);

            if (!CFG.ContainsKey("ServerTPS"))
            {
                CFG["ServerTPS"] = (uint)200;

                File.WriteAllText("app-settings.json", CFG.ToString());
            }
            ServerTPS = CFG["ServerTPS"].Value<uint>();
            var targetms = (1000 / ServerTPS);
            
            
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (true) {
                var ms = stopwatch.ElapsedMilliseconds;
                var currentTargetMs = targetms + syncErrorMs;
                if (ms >= currentTargetMs) 
                {
                    syncErrorMs = (ms - currentTargetMs);
                    ServerUpdate();
                    stopwatch.Restart();
                }
                Thread.Sleep(1);
            };
        }

        static int t = 0;
        private static void ServerUpdate()
        {
            Server.Update();

            networkManager.Update();
            t++;

            if (t == ServerTPS) //1 sec 
            {
                t = 0;
                Console.Clear();
                networkManager.ShowStatus();
                networkManager.ShowPacketCount();
                networkManager.ClearPacketCount();
                
            }
        }
    }
}