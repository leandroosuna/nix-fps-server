using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nix_fps_server
{
    public class Gun
    {
        string name;
        byte damageHead;
        byte damageBody;
        byte damageLeg;

        public Gun(string name, byte head, byte body, byte leg) 
        {
            this.name = name;
            damageHead = head;
            damageBody = body;
            damageLeg = leg;
        }
        public byte GetHeadDamage() { return damageHead; }
        public byte GetBodyDamage() { return damageBody; }
        public byte GetLegDamage() { return damageLeg; }

    }
}
