using Microsoft.Xna.Framework;
using Riptide;

namespace nix_fps_server
{
    public class Player
    {
        public uint id;
        public string name;
        public ushort netId;

        public Vector3 position = Vector3.Zero;
        public Vector3 frontDirection = Vector3.Zero;
        public float yaw;
        public bool connected;
        public bool connectedMessageSent;
        public bool disconnectedMessageSent;

        public Player(uint id)
        {
            this.id = id;
            name = "noname";
            connectedMessageSent = false;
            disconnectedMessageSent = false;

        }
    }
}
