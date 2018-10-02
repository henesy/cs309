using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/* below are added for MonoGame purposes */
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.Serialization;

namespace DegreeQuest
{
    /* Player class to manage to user */
    [Serializable()]

    /* Player class to manage to user */
    public class PC : Actor
    {
        //action for server side usage
        public Microsoft.Xna.Framework.Input.Keys[] kbState;
        public Microsoft.Xna.Framework.Input.MouseState mouseState;
        public Location mLoc;

        public static readonly int TEXTURE_WIDTH = 64;
        public static readonly int TEXTURE_HEIGHT = 64;

        //Base values and constants related to PCs
        public static readonly int PC_BASE_HP= 100;
        public static readonly int PC_BASE_EP = 100;
        public static readonly int PC_BASE_DEBT = 10000;
        public static readonly int[] PC_BASE_STATS = { 0, 0, 0, 0, 0 };
        public static readonly int BASE_BAG_SIZE = 50;
        public static readonly int PC_BASE_DAMAGE = 5;
        public static readonly int PC_MELEE_RANGE = 96;
        // Animation representing the player
        //[NonSerialized] public Texture2D PlayerTexture;

        // Position of the Player relative to the upper left side of the screen
        // in Actor

        // State of the player
        //public bool Active;

        // Current and maximum amount of hit points that player has
        public int HP, HPMax;

        // Current and maximum amount of energy (mana) the player has
        public int EP, EPMax;

        // Total and currently available amount of debt for player
        public int Debt, DebtTotal;

        // Title/Name
        public string Name;

        //Players current stat levels
        public int[] Stats;

        public Item[] equipment;
        public Item[] bag;

        public int atkmod, defmod, spdmod;
        public long id;
        public int number;

        //Default Constructor
        public PC()
        {
            Position = new Location(new Vector2(-1, -1));
            number = (new Random().Next(1, 4) + 1);
            Active = false;
            HP = HPMax = PC_BASE_HP;
            EP = EPMax = PC_BASE_EP;
            Stats = PC_BASE_STATS;
            Debt = DebtTotal = PC_BASE_DEBT;
            Name = "Paul Chaser";
            equipment = new Item[Item.equip_slots];
            bag = new Item[BASE_BAG_SIZE];
            //changeme
            Texture = "Player"+number.ToString();
            atkmod = defmod = spdmod = 0;
            //for server-side use from a client
            id = System.DateTime.Now.Ticks;
        }



        public void Update()
        {

        }

        /* As per Actor */
        public override AType GetAType()
        { return AType.PC; }

        public override Vector2 GetPos()
        { return Position.toVector2(); }


        public int Swing()
        {
            Item weapon = equipment[(int)Item.IType.TwoHand];
            if (weapon == null) { weapon = equipment[(int)Item.IType.OneHand]; }
            if(weapon == null) { return PC_BASE_DAMAGE; }
            else { return weapon.atk + PC_BASE_DAMAGE; }
        }

        //If room in bag, adds and returns true. else returns false
        public bool pickup(Item p)
        {
            for(int i=0; i < bag.Length; i++)
            {
                if(bag[i] == null)
                {
                    bag[i] = p;
                    return true;
                }
            }
            return false;
        }

        //equips the given item e. Returns the item that was in that slot, or e if it cannot be equipped.
        public Item equip(Item e)
        {
            if (e.isEquip())
            {
                Item temp = equipment[(int)e.type];
                equipment[(int)e.type] = e;
                return temp;
            }
            else
            {
                return e;
            }
        }

        //If room in inventory, unequips the item in the given slot and places it into inventory. else nothing.
        public Boolean unequip(int slot)
        {
            if (this.pickup(equipment[slot]))
            {
                equipment[slot] = null;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override int GetWidth()
        { return TEXTURE_WIDTH; }

        public override int GetHeight()
        { return TEXTURE_HEIGHT; }

        public bool Equals(PC p)
        {
            return p.Name.Equals(Name);
        }

        public bool TakeHit(int damage)
        {
            HP = Math.Max(0, HP - damage);
            return true;
        }

        public String Attack (Actor a)
        {

            if (a == null || a.GetAType() != AType.NPC) { return "Nothing to attack!"; }
            int damage = Swing();
            bool kill = !((NPC)a).TakeHit(damage);
            if (kill)
            {
                return "Killed enemy " + ((NPC)a).name + "!";
            }
            else
            {
                return "Hit enemy " + ((NPC)a).name + " for "+damage+".";
            }
        }
    }
}
