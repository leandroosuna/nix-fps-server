using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using Riptide;
using Riptide.Utils;
using System.Collections.Generic;

namespace nix_fps_server
{
    public class NetworkManager
    {
        static List<Player> players = new List<Player>();
        static List<Gun> guns = new List<Gun>();
        static Queue<(uint p1, uint p2, byte gun)> killFeed = new Queue<(uint p1, uint p2, byte gun)>();

        static List<Player> playersJustJoined = new List<Player>();
        public void Init()
        {
            guns.Add(new Gun("rifle", 150, 40, 25));
            guns.Add(new Gun("pistol", 100, 20, 10));

        }
        public void Update()
        {
            CheckRTT();
            CheckPlayersJustJoined();
            BroadcastPlayerData();
            BroadcastKillFeed();
        }
        
        void CheckRTT()
        {
            foreach(var c in Program.Server.Clients)
            {
                GetPlayerFromNetId(c.Id).RTT = c.RTT;
            }
        }
        
        void CheckPlayersJustJoined()
        {
            var othersCount = (uint)players.Count - 1;

            foreach(var p in playersJustJoined)
            {
                Message newName = Message.Create(MessageSendMode.Reliable, ServerToClient.PlayerName);
                newName.AddUInt(1);
                newName.AddUInt(p.id);
                newName.AddString(p.name);

                Program.Server.SendToAll(newName, p.netId);

                if(othersCount > 0)
                {
                    Message names = Message.Create(MessageSendMode.Reliable, ServerToClient.PlayerName);
                    names.AddUInt(othersCount);
                
                    foreach(var op in players)
                    {                    
                        if (op.id != p.id)
                        {
                            names.AddUInt(op.id);
                            names.AddString(op.name);
                        }
                    }
                    Program.Server.Send(names, p.netId);
                }
            }

            playersJustJoined.Clear();
        }
        void BroadcastKillFeed()
        {
            if(killFeed.Count > 0)
            {
                var kf = killFeed.Dequeue();
                Message m = Message.Create(MessageSendMode.Reliable, ServerToClient.KillFeed);
                m.AddUInt(kf.p1);
                m.AddUInt(kf.p2);
                m.AddByte(kf.gun);

                Program.Server.SendToAll(m);
            }
        }
        [MessageHandler((ushort)ClientToServer.PlayerIdentity)]
        static void HandlePlayerIdentity(ushort fromClientId, Message message)
        {
            //Console.WriteLine("identity received");
            var playerId = message.GetUInt();
            var playerName = message.GetString();
            var version = message.GetInt();

            var current = Program.CFG["Version"].Value<int>();
            if (version != current)
            {
                Program.Server.DisconnectClient(fromClientId);
                //Console.WriteLine($"id {playerId} wrong version: {version} -> (current {current})");
                return;
            }

            Player p = GetPlayerFromId(playerId, true);
            p.name = playerName;
            p.netId = fromClientId;
            p.connected = true;

            playersJustJoined.Add(p);
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
                    message.AddVector3(p.color);
                    message.AddVector3(p.position);
                    message.AddFloat(p.yaw);
                    message.AddFloat(p.pitch);
                    message.AddByte(p.clipId);
                    message.AddByte(p.health);
                    message.AddByte(p.hitLocation);
                    message.AddUInt(p.damagerId);
                    message.AddByte(p.fired?p.gunId:(byte)0);
                    message.AddUInt(p.kills);
                    message.AddUInt(p.deaths);

                }
                p.outboundPackets++;

                if(p.health == 0)
                {
                    //just killed, restore hp after sending this last message
                    p.health = 150;
                }
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
            
            var color = message.GetVector3();
            clientState.position = message.GetVector3();
            clientState.positionDelta = message.GetVector3();
            clientState.yaw = message.GetFloat();
            clientState.pitch = message.GetFloat();
            var hitLocation = message.GetByte();
            var gunId = message.GetByte();
            var enemyId = message.GetUInt();
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
            p.gunId = gunId;
            p.fired = clientState.Fire;
            ApplyHit(p.id, enemyId, gunId, hitLocation);

            p.color = color;
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
        

        public static void ApplyHit(uint damager, uint player, byte gunId, byte hitLocation)
        {
            Player p = GetPlayerFromId(player);

            if (hitLocation <= 0) {
                
                p.hitLocation = hitLocation;
                return;
            };


            if (gunId <= 0 || gunId > guns.Count) return;

            var gun = guns[gunId - 1];

            bool killed = false;
            switch(hitLocation)
            {
                case 1:
                    killed = Damage(p, gun.GetHeadDamage()); break;
                case 2:
                    killed = Damage(p, gun.GetBodyDamage()); break;
                case 3:
                    killed = Damage(p, gun.GetLegDamage()); break;

            }
            p.hitLocation = hitLocation;
            p.damagerId = damager;
            if(killed)
            {
                Player ep = GetPlayerFromId(damager);
                ep.kills++;
                p.deaths++;

                killFeed.Enqueue((damager, player, gunId));
            }

        }

        public static bool Damage(Player p, byte damage)
        {
            if (p.health - damage <= 0)
            {
                p.health = 0;
                return true;

            }
            else
                p.health -= damage;

            return false;
            
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
            foreach (var c in Program.Server.Clients)
            {
                var player = GetPlayerFromNetId(c.Id);
                //Console.SetCursorPosition(0, line);
                //Console.WriteLine(player.name + " IO " + c.Metrics.UnreliableIn + " - " + player.outboundPackets + " - " + c.Metrics.UnreliableOut + " RTT " + c.RTT + " ms   lv " + player.lastMovementValid);
                Console.WriteLine($"{player.name} {player.id} IO {c.Metrics.UnreliableIn}-{c.Metrics.UnreliableOut}  RTT " +
                    $" {c.RTT} ms KD {player.kills}/{player.deaths}"
                    //+ (int)player.position.X + " " + (int)player.position.Y + " " + (int)player.position.Z);
                    );
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
            Message m = Message.Create(MessageSendMode.Reliable, ServerToClient.Version);
            var v = Program.CFG["Version"].Value<int>();
            m.AddInt(v);
            Program.Server.Send(m, id);
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