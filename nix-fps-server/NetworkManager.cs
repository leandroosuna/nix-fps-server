﻿using Microsoft.Xna.Framework;
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
            CheckRTT();
            BroadcastPlayerData();
        }
        
        void CheckRTT()
        {
            foreach(var c in Program.Server.Clients)
            {
                GetPlayerFromNetId(c.Id).RTT = c.RTT;
            }
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

        bool packetloss = false;
        public void BroadcastPlayerData()
        {
            //if (players.Count == 0)
            //    return;
            Message message = Message.Create(MessageSendMode.Unreliable, ServerToClient.AllPlayerData);
            
            //filter out players that didnt send a new update (caused by framerate lower than server TPS)
            //var playersToAdd = players.FindAll(p => p.lastMessageAck != p.lastProcessedMesage);


            //no update from any player this tick
            //if (playersToAdd.Count == 0)
            //    return;

            message.AddInt(players.Count);
            foreach (Player p in players)
            {
                message.AddUInt(p.id);
                message.AddBool(p.connected);
                if (p.connected)
                { 
                    message.AddUInt(p.lastProcessedMesage);
                    message.AddBool(p.lastMovementValid);
                    message.AddVector3(p.position);
                    message.AddFloat(p.yaw);
                    message.AddFloat(p.pitch);
                    message.AddByte(p.clipId);
                }
                p.outboundPackets++;
            }
            Program.Server.SendToAll(message);
        }

        
        [MessageHandler((ushort)ClientToServer.PlayerData)]
        private static void HandlePlayerData(ushort fromClientId, Message message)
        {
            var id = message.GetUInt();
            Player p = GetPlayerFromId(id);

            var clientState= new ClientInputState();
            clientState.messageId = message.GetUInt();
            
            clientState.position = message.GetVector3();
            clientState.positionDelta = message.GetVector3();
            clientState.yaw = message.GetFloat();
            clientState.pitch = message.GetFloat();
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
            //clientState.accDeltaTime = message.GetFloat();
            clientState = ValidateInput(clientState);
            
            p.Apply(clientState);
            //clientState.ApplyInputTo(p);

        }
        public static ClientInputState ValidateInput(ClientInputState state)
        {
            //float validDelta = 0.005f; //expected delta time
            //validDelta *= (state.Sprint ? 18f : 9.5f); //speed modifier
            //validDelta += 0.015f; //error margin

            //state.valid = true;
            //var len = state.positionDelta.Length();
            //if (len < validDelta)
            //    return state;

            //var diff = len - validDelta;
            //state.valid = false;

            //state.positionDelta.Normalize();
            //state.positionDelta *= diff;

            //state.position -= state.positionDelta;
            //return state;


            state.valid = true;
            return state;
        }
        public void ShowStatus()
        {
            var str = "Server status";
            if (Program.Server.IsRunning)
            {
                str += " ONLINE    TPS "+Program.ServerTPS + "    players online "+Program.Server.ClientCount + "/"+players.Count+" max "+Program.Server.MaxClientCount;
            }
            else
                str += " OFFLINE";
            
            Console.SetCursorPosition(0, 0);
            Console.Write(str);

            
            str = "Mode -    Map -    Time -    State - ";

            Console.SetCursorPosition(0, 1);
            Console.Write(str);
        }
        public void ShowPacketCount()
        {
            var line = 2;
            //Console.SetCursorPosition(0, 2);

            int countIn = 0;
            int countOut = 0;
            foreach(var c in Program.Server.Clients)
            {
                countIn += c.Metrics.UnreliableIn;
                countOut += c.Metrics.UnreliableOut;
            }
            //Console.Write("total IO " + countIn + " - " + countOut);
            line++;
            foreach(var c in Program.Server.Clients)
            {
                var player = GetPlayerFromNetId(c.Id);
                //Console.SetCursorPosition(0, line);
                //Console.WriteLine(player.name + " IO " + c.Metrics.UnreliableIn + " - " + player.outboundPackets + " - " + c.Metrics.UnreliableOut + " RTT " + c.RTT + " ms   lv " + player.lastMovementValid);
                Console.WriteLine(player.name + " IO " + c.Metrics.UnreliableIn + " - " + 
                    player.outboundPackets + " - " + c.Metrics.UnreliableOut + " RTT " + c.RTT + " ms " + (int)player.position.X + " " + (int)player.position.Y + " " + (int)player.position.Z);

                line++;
            }


             
        }
        public void ClearPacketCount()
        {
            foreach (var c in Program.Server.Clients)
            {
                c.Metrics.Reset();
            }
            foreach(var p in players)
            {
                p.outboundPackets = 0;
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