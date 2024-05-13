using Microsoft.Xna.Framework;
using Riptide;
using Riptide.Utils;

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

            while (true)
            {
                Server.Update();

                networkManager.Update();

                Thread.Sleep(10);
            }

        }
    
    
    }
}