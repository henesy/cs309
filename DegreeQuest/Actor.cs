using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DegreeQuest
{
    /* For reflection of what's filling Actor, Item/Object/Static are placeholders for non-combatant elements of the world */
    [DataContract]
    public enum AType {Item, NPC, PC, Terrain, Static, Projectile}; //In Dispaly order bottom to top
    public enum PType {Arrow, Beam, Dot};
    public enum Bear {N, S, E, W };

    /*
    *   Exists to provide a placeholder within which a player or a Monster can be placed without mutual exclusion 
    *   to implement you must use "MyClass : Actor" this is equivalent to the Java "MyClass implements Actor"
    */
    [Serializable()]
    public abstract class Actor
    {
        public bool Active;

        //float Position { get; set; }

        protected string Texture { get; set; }

        //public Texture2D Texture { get; set; }

        public Location Position;

        public float MoveSpeed;

        /* Provides "type" of object filling the interface */
        public abstract AType GetAType();

        public abstract  Vector2 GetPos();

        public abstract int GetHeight();
        public abstract int GetWidth();
        public  string GetTexture() { return Texture; }

        public void SetTexture(String text) { Texture = text; }

        //public abstract void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch);

        public void Initialize(string texture, Vector2 position)
        {
            Texture = texture;

            //Set the starting position of the player around the middle of the screen and to the back
            Position = new Location(position);

            // Set the player to be active
            Active = true;

            // Stats would be set here...

        }

        /* returns true if we will collide with the given actor, called by canMove */
        public bool Collides(Actor a, Config conf)
        {
            var w = conf.iget("spriteLen")+this.MoveSpeed;
            
            // we can move through ourselves ;; need to fix npc test spawning to not be on top of us or it locks our movement
            //if (a.Position.X == this.Position.X && a.Position.Y == this.Position.Y)
             //   return false;

            // we can move through projectiles
            var xd = Math.Abs(a.Position.X - this.Position.X);
            var yd = Math.Abs(a.Position.Y - this.Position.Y);
            if (((xd <= w && yd <= w) && (xd > 0 && yd > 0)) && (a.GetAType() != AType.Projectile || a.Texture == "dot") && a.Active)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool CanMove(Bear b, Room r, Config conf)
        {
            Actor us = this;
            Location orig = new Location(us.Position.X, us.Position.Y);
            switch (b)
            {
                case Bear.N:
                    us.Position.Y -= us.MoveSpeed;
                    break;
                case Bear.S:
                    us.Position.Y += us.MoveSpeed;
                    break;
                case Bear.E:
                    us.Position.X += us.MoveSpeed;
                    break;
                case Bear.W:
                    us.Position.X -= us.MoveSpeed;
                    break;
            }

            //Console.WriteLine("Members in room to be scanned: " + r.num);
            int i;
            for (i = 0; i < r.num; i++)
            {
                if(r.members[i].Overlap(us))
                {
                    
                    //Console.WriteLine("Cancelled movement with: " + r.members[i].GetHashCode() + " " + i + " " + this.GetHashCode());
                    this.Position = orig;
                    us.Position = orig;
                    return false;
                }
            }

            this.Position = orig;
            us.Position = orig;
            return true;
        }

        //true if the actor's texture occupies the given loc
        public bool Occupying(Vector2 loc)
        {
            Vector2 pos = GetPos();
            float farX = pos.X + GetWidth();
            float farY = pos.Y + GetHeight();
            return Active && (loc.X >= pos.X && loc.X <= farX && loc.Y >= pos.Y && loc.Y <= farY);

        }

        //true if the actors currently overlap, false otherwise
        public bool Overlap(Actor a)
        {
            if(!(a.Active && Active)) { return false; }
            if (a.Equals(this)) { return false; }
            if(a.GetAType() == AType.Projectile || GetAType() == AType.Projectile) { return false; }
            if(a.Position.X == Position.X && a.Position.Y == Position.Y) { return false; }
            Vector2 diff = a.GetPos() - GetPos();
            if(Math.Abs(diff.X) < 0 && Math.Abs(diff.Y) < 0) { return false; }
            int x = GetWidth();
            int y = GetHeight();
            if (diff.X < 0) { x = a.GetWidth(); diff.X =diff.X * -1; }
            if (diff.Y < 0) { y = a.GetHeight(); diff.Y = diff.Y * -1; }

            return (diff.X < x && diff.Y < y);
        }

    }

    public class ActorComparer : IComparer
    {
        int IComparer.Compare(object x, object y)
        {
            if (x == null && y != null)
                return 1;

            if (y == null && x != null)
                return -1;

            if (y == null && x == null)
                return 0;

            AType xType = ((Actor)x).GetAType();
            AType yType = ((Actor)y).GetAType();

            if (xType == yType)
                return 0;

            if (xType == AType.PC) 
                return -1;
            if (yType == AType.PC)
                return 1;

            return 0;

        }
    }
}
