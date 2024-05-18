using Microsoft.Xna.Framework;
using Riptide;
using Riptide.Utils;
using System.Collections.Generic;

namespace nix_fps_server
{
    public class NetworkManager
    {
        static List<Player> players = new List<Player>();
        static List<Player> playersMissingEnemies = new List<Player>();
        

        public void Update()
        {
            CheckPlayerMissingEnemies();
            CheckPlayerConnectionState();
            CheckRTT();
            BroadcastPlayerData();
        }
        public void CheckPlayerConnectionState()
        {
            foreach(Player p in players)
            {
                if(p.connected)
                {
                    if (!p.connectedMessageSent)
                    { 
                        Console.WriteLine(p.name +"("+p.id+") connected");

                        SendConnectedMessage(p);
                        p.connectedMessageSent = true;
                        p.disconnectedMessageSent = false;
                    }
                }
                else
                {
                    if(!p.disconnectedMessageSent)
                    {
                        Console.WriteLine(p.name + "(" + p.id + ") disconnected");

                        Message message = Message.Create(MessageSendMode.Reliable, ServerToClient.PlayerDisconnected);
                        message.AddUInt(p.id);
                        message.AddString(p.name);
                        
                        Program.Server.SendToAll(message);
                        p.disconnectedMessageSent = true;
                        p.connectedMessageSent = false;
                    }
                }
            }
        }
        void CheckRTT()
        {
            foreach(var c in Program.Server.Clients)
            {
                GetPlayerFromNetId(c.Id).RTT = c.RTT;
            }
        }
        public void SendConnectedMessage(Player p, ushort toId = ushort.MaxValue)
        {
            Message message = Message.Create(MessageSendMode.Reliable, ServerToClient.PlayerConnected);
            message.AddUInt(p.id);
            message.AddString(p.name);
            if (toId != ushort.MaxValue)
                Program.Server.Send(message, toId);
            else
                Program.Server.SendToAll(message);
        }
        
        public void CheckPlayerMissingEnemies()
        {
            foreach(Player pme in playersMissingEnemies) {  
                foreach(Player p in players)
                {
                    if (p.id != pme.id)
                    {
                        Console.WriteLine(pme.name + " was missing " + p.name);
                        SendConnectedMessage(p, pme.netId);
                    }
                }
            }
            playersMissingEnemies.Clear();
        }
        [MessageHandler((ushort)ClientToServer.PlayerIdentity)]
        static void HandlePlayerIdentity(ushort fromClientId, Message message)
        {
            Console.WriteLine("identity received");
            var playerId = message.GetUInt();
            var playerName = message.GetString();
            Player p = GetPlayerFromId(playerId, true);
            p.name = playerName;
            p.netId = fromClientId;
            p.connected = true;
            playersMissingEnemies.Add(p);
        }
        public int outboundPackets = 0;
        public void BroadcastPlayerData()
        {
            //if (players.Count == 0)
            //    return;
            Message message = Message.Create(MessageSendMode.Unreliable, ServerToClient.AllPlayerData);
            message.AddInt(players.Count);
            foreach (Player player in players)
            {
                message.AddUInt(player.id);
                message.AddUInt(player.lastProcessedMesage);
                message.AddVector3(player.position);
                message.AddFloat(player.yaw);
                message.AddFloat(player.pitch);
                message.AddByte(player.clipId);

                //Console.WriteLine(players.Count+" id " + player.id + " pos " + player.position + " fd " + player.frontDirection + " y " + player.yaw);
            }
            Program.Server.SendToAll(message);
            outboundPackets++;
        }

        
        [MessageHandler((ushort)ClientToServer.PlayerData)]
        private static void HandlePlayerData(ushort fromClientId, Message message)
        {
            var id = message.GetUInt();
            Player p = GetPlayerFromId(id);

            var clientState= new ClientInputState();
            clientState.position = message.GetVector3();

            clientState.messageId = message.GetUInt();
            clientState.Forward = message.GetBool();
            clientState.Backward = message.GetBool();
            clientState.Left = message.GetBool();
            clientState.Right = message.GetBool();
            clientState.Sprint = message.GetBool();
            clientState.Jump = message.GetBool();
            clientState.Crouch = message.GetBool();
            clientState.Fire = message.GetBool();
            clientState.ADS = message.GetBool();
            clientState.Ability1 = message.GetBool();
            clientState.Ability2 = message.GetBool();
            clientState.Ability3 = message.GetBool();
            clientState.Ability4 = message.GetBool();
            clientState.yaw = message.GetFloat();
            clientState.pitch = message.GetFloat();

            clientState.accDeltaTime = message.GetFloat();
            clientState.ApplyInputTo(p);
        }
        public void ShowPacketCount()
        {
            int count = 0;
            foreach(var c in Program.Server.Clients)
            {
                count += c.Metrics.UnreliableIn;
                var player = GetPlayerFromNetId(c.Id);
                Console.WriteLine(player.name + " " + c.Metrics.UnreliableIn + " pps in");
            }
            Console.WriteLine("total pps in" + count); 
        }
        public void ClearPacketCount()
        {
            foreach (var c in Program.Server.Clients)
            {
                c.Metrics.Reset();
            }
        }
        public static void HandleConnect(ushort id)
        {
            //var player = GetPlayerFromNetId(id);
            //player.connected = true;
            //Console.WriteLine("handle connect");
        }
        public static void HandleDisconnect(ushort id)
        {
            var player = GetPlayerFromNetId(id);
            player.connected = false;
            //Console.WriteLine("handle disconnect");
        }

        public static Player GetPlayerFromNetId(ushort id)
        {
            foreach (var player in players)
            {
                if (player.netId == id)
                {
                    return player;
                }
            }
            return new Player(uint.MaxValue);
        }

        public static Player GetPlayerFromId(uint id, bool createIfNull = false)
        {
            foreach (var player in players)
            {
                if (player.id == id)
                {
                    return player;
                }
            }
            if (createIfNull)
            {
                Player p = new Player(id);
                players.Add(p);
                return p;
            }

            return new Player(uint.MaxValue);
        }

    }
}