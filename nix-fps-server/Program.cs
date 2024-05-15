using Microsoft.Xna.Framework;
using Riptide;
using Riptide.Utils;
using System.Diagnostics;

namespace nix_fps_server
{
    class Program
    {
        public static Server Server;
        static NetworkManager networkManager;

        static int ServerFREQ = 200;
        static void Main(string[] args)
        {

            RiptideLogger.Initialize(Console.WriteLine, false);
            networkManager = new NetworkManager();
            Server = new Server();
            Server.Start(7777, 8);
            Server.ClientConnected += (s, e) => NetworkManager.HandleConnect(e.Client.Id);
            Server.ClientDisconnected += (s, e) => NetworkManager.HandleDisconnect(e.Client.Id);


            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (true) {
                if(stopwatch.ElapsedMilliseconds >= 5) //200tps
                //if (stopwatch.ElapsedMilliseconds >= 1) //1000tps
                {
                    ServerUpdate();
                    stopwatch.Restart();
                }
            };
        }

        static int t = 0;
        private static void ServerUpdate()
        {
            Server.Update();

            networkManager.Update();
            t++;

            //if (t == 1000) //1 sec at 1000 tps
            if (t == 200) //1 sec at 200 tps
            {
                t = 0;
                networkManager.ShowPacketCount();
                networkManager.ClearPacketCount();
                //Console.WriteLine("outbound packets " + networkManager.outboundPackets);
                networkManager.outboundPackets = 0;
                foreach (var c in Server.Clients)
                {

                    Console.WriteLine(NetworkManager.GetPlayerFromNetId(c.Id).name + " RTT " + c.RTT);
                }
            }
        }
    }
}