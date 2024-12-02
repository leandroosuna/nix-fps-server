using Microsoft.Xna.Framework;
using Riptide;

namespace nix_fps_server
{
    public class Player
    {
        public uint id;
        public string name;
        public ushort netId;
        public short RTT;

        public Vector3 lastPosition = Vector3.Zero;
        public Vector3 position = Vector3.Zero;
        public float yaw;
        public float pitch;
        public byte clipId;

        public byte health = 150;
        public byte hitLocation;
        public uint damagerId;

        public bool connected;
        public bool connectedMessageSent;
        public bool disconnectedMessageSent;
        
        public uint lastProcessedMesage;

        public uint outboundPackets = 0;
        public bool lastMovementValid = false;

        public byte gunId;
        public bool fired;
        public uint kills = 0;
        public uint deaths = 0;

        public Vector3 color;
        public Player(uint id)
        {
            this.id = id;
            name = "noname";
            connectedMessageSent = false;
            disconnectedMessageSent = false;

        }

        public void Apply(ClientInputState state)
        {
            position = state.position;

            pitch = state.pitch;
            yaw = state.yaw;

            var dz = (state.Forward? 1 : 0) - (state.Backward? 1: 0);
            var dx = (state.Right? 1 : 0) - (state.Left? 1 : 0);

            if (dz > 0 && dx == 0)
            {
                if (state.Sprint)
                    clipId = (byte)PlayerAnimation.sprintForward;
                else
                    clipId = (byte)PlayerAnimation.runForward;
            }
            else if (dz > 0 && dx > 0)
            {
                if (state.Sprint)
                    clipId = (byte)PlayerAnimation.sprintForwardRight;
                else
                    clipId = (byte)PlayerAnimation.runForwardRight;
            }
            else if (dz > 0 && dx < 0)
            {
                if (state.Sprint)
                    clipId = (byte)PlayerAnimation.sprintForwardLeft;
                else
                    clipId = (byte)PlayerAnimation.runForwardLeft;
            }
            else if (dz < 0 && dx == 0)
            {
                clipId = (byte)PlayerAnimation.runBackward;
            }
            else if (dz < 0 && dx > 0)
            {
                clipId = (byte)PlayerAnimation.runBackwardRight;
            }
            else if (dz < 0 && dx < 0)
            {
                clipId = (byte)PlayerAnimation.runBackwardLeft;
            }
            else if (dz == 0 && dx > 0)
            {
                clipId = (byte)PlayerAnimation.runRight;
            }
            else if (dz == 0 && dx < 0)
            {
                clipId = (byte)PlayerAnimation.runLeft;
            }
            else
                clipId = (byte)PlayerAnimation.idle;
            //clipId = //calc from input;
            lastMovementValid = state.valid;

        }
    }
}
