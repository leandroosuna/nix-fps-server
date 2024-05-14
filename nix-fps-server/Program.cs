using Microsoft.Xna.Framework;
using Riptide;
using Riptide.Utils;
using System.Timers;
namespace nix_fps_server
{
    class Program
    {
        public static Server Server;
        static NetworkManager networkManager;
        static void Main(string[] args)
        {

            RiptideLogger.Initialize(Console.WriteLine, false);
            networkManager = new NetworkManager();
            Server = new Server();
            Server.Start(7777, 8);
            Server.ClientConnected += (s, e) => NetworkManager.HandleConnect(e.Client.Id);
            Server.ClientDisconnected += (s, e) => NetworkManager.HandleDisconnect(e.Client.Id);
            System.Timers.Timer timer = new System.Timers.Timer(10);
            timer.Elapsed += ServerUpdateTimer;
            timer.AutoReset = true;
            timer.Start();

            while (true) { };
        }
        static int t = 0;
        private static void ServerUpdateTimer(object? sender, ElapsedEventArgs e)
        {
            Server.Update();
            
            networkManager.Update();

            t++;
            if(t == 100) //1 sec
            {
                t = 0;
                networkManager.ShowPacketCount();
                networkManager.ClearPacketCount();
                foreach (var c in Server.Clients)
                {
                    
                    Console.WriteLine(NetworkManager.GetPlayerFromNetId(c.Id).name + " RTT " + c.RTT);
                }
            }
        }
    }
}