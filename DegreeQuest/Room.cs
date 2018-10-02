using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using Microsoft.Xna.Framework;


namespace DegreeQuest
{
    public class Room
    {
        public static int ITEM_MAX = 256;
        public static int num_floor = 10;
        public static int num_wall = 2;
        /* Populate actors by looping over members and getting X/Y values on location in Room */
        public volatile Item[] items;
        public volatile int num_item;
        public volatile Actor[] members;
        public volatile int num;
        public int floor, walls;


        public Room()
        {
            lock (this)
            {
                /** Handlers for given players/monsters should remove objects/edit objects ;; members should really only be read by other threads/functions/methods **/
                members = new Actor[200];
                items = new Item[ITEM_MAX];
                num = num_item = 0;
                floor = new Random().Next(1, num_floor+1);
                walls = new Random().Next(1, num_wall+1);

                Random rand = new Random();
                int enemyCount = rand.Next(4)+rand.Next(4);
                for(int i = 0; i < enemyCount; i++)
                {
                    NPC npc = NPC.Random();
                    
                    npc.Initialize(npc.name, new Vector2(rand.Next(64, 1472), rand.Next(64, 704)));
                    if (npc.TryMove(this, npc.GetPos()))
                    {
                        Add(npc);
                    }
                }
                int itemCount = rand.Next(3)+rand.Next(3);
                for (int i = 0; i < itemCount; i++)
                {
                    Item item = Item.Random();

                    item.Initialize(item.name, new Vector2(rand.Next(64, 1472), rand.Next(64, 704)));
                    Add(item);
                }
                Console.WriteLine("lol:" + walls);
            }
        }

        /// <summary>
        /// Secondary constructor used for duplicating a room
        /// </summary>
        /// <param name="floor">the room num being used</param>
        /// <param name="walls">the walls num being used</param>
        public Room(int floor, int walls)
        {
            members = new Actor[200];
            items = new Item[ITEM_MAX];
            num = num_item = 0;
            this.floor = floor;
            this.walls = walls;
        }
        public void sortMembers()
        {
            lock (this)
            {
                IComparer comp = new ActorComparer();
                Array.Sort(members, 0, num, comp);
            }         
        }

        /// <summary>
        /// creates a shallow copy of the room.
        /// </summary>
        /// <returns>a shallow copy of the room</returns>
        public Room copy()
        {
            lock (this)
            {
                Room room = new Room(floor, walls);
                for (int i = 0; i < num_item; i++)
                {
                    room.Add(items[i]);
                }
                for (int i = 0; i < num; i++)
                {
                    room.Add(members[i]);
                }
                return room;
            }
        }


        public PC[] GetPCs()
        {
            lock (this) {
                int count;
                for (count = 0; count < num; count++)
                {
                    Actor next = members[count];
                    if (next == null || next.GetAType() != AType.PC) { break; }
                }
                PC[] pcs = new PC[count];
                for(int i = 0; i < count; i++)
                {
                    pcs[i] = (PC)members[i];
                }
                return pcs;
            }
        }

        public NPC[] GetNPCs()
        {
            lock (this)
            {
                int count = 0;
                for (int i = 0; i < num; i++)
                {
                    Actor next = members[i];
                    if (next!= null && next.GetAType() == AType.NPC && next.Active) { count++; }
                }
                NPC[] npcs = new NPC[count];
                int n = 0;
                for (int i = 0; n < count;i++ )
                {
                    Actor next = members[i];
                    if (next != null && next.GetAType() == AType.NPC && next.Active) { npcs[n] = (NPC)next; n++; }
                }
                return npcs;
            }
        }

        public Item[] GetItems()
        {
            lock (this)
            {
                Item[] arr = new Item[num_item];
                for (int i = 0; i< num_item; i++)
                {
                    Item next = items[i];
                    arr[i] = next;
                }
                return arr;
            }
        }


        public void Add(Actor a)
        {
            lock (this)
            {
                if (a.GetAType() == AType.Item)
                {
                    if (num_item + 1 < ITEM_MAX)
                    {
                        items[num_item] = (Item)a;
                        num_item++;
                    }
                }
                else
                {
                    if (num + 1 < 200)
                    {
                        members[num] = a;
                        num++;
                        sortMembers();
                    }
                }
            }
        }


        public void Delete(Actor a)
        {
            lock (this)
            {
                if (a.GetAType() == AType.Item) {
                    a.Active = false;
                    Array.Sort(items, 0, this.num_item, new ClientComparator());
                    num_item--;
                }
                else
                {
                    a.Active = false;
                    ClientComparator comp = new ClientComparator();
                    Array.Sort(members, 0, this.num, comp);
                    num--;
                }
            }
        }

        //true if the given actor overlaps with any other actor in the room
        public bool Overlap(Actor a)
        {
            lock (this)
            {
                for(int i = 0; i < num; i++)
                {
                    if (a.Overlap(members[i]))
                    {
                        if (!a.Equals(members[i])) { return true; }
                    }
                }
                return false;
            }
        }


        public Actor NearestPC(Vector2 loc)
        {
            lock (this)
            {
                int i = 1;
                Actor best = members[0];
                while (i < num && members[i].GetAType() == AType.PC)
                {
                    if ((members[i].GetPos() - loc).Length() < (best.GetPos() - loc).Length()) { best = members[i]; }
                    i++;
                }
                return best;
            }
        }
        public Actor Occupying(Vector2 loc)
        {
            lock (this)
            {
                for (int i = 0; i < num; i++)
                {
                    if (members[i].Occupying(loc) && (members[i].GetAType()==AType.PC || members[i].GetAType() == AType.NPC)) { return members[i]; }
                }
                return null;
            }
        }

        public String Pickup(PC p)
        {
            lock (this)
            {
                String ret = null;
                for (int i = 0; i < num_item; i++)
                {
                    if (p.Overlap(items[i]))
                    {
                        if (p.pickup(items[i]))
                        {
                            ret = "Picked up " + items[i].name + ".";
                            Delete(items[i]);
                            return ret;
                        }
                        else
                        {
                            return "Inventory is full.";
                        }
                    }
                }
                return ret;
            }
        }


        public RoomInfo Info()
        {
            lock (this)
            {
                return new RoomInfo(this);
            }
        }

        [Serializable()]
        public class RoomInfo{
            public String[] textures;
            public Vector2[] locs;
            public PC[] pcs;
            public int num_npc;
            public int num_item;
            public int walls, floor;
            
            public RoomInfo(Room r)
            {
                lock (r)
                {
                    pcs = r.GetPCs();
                    NPC[] npcs = r.GetNPCs();
                    Item[] items = r.GetItems();
                    num_npc = npcs.Length;
                    num_item = items.Length;
                    textures = new String[num_npc + num_item];
                    locs = new Vector2[num_npc + num_item];
                    int i;
                    for (i = 0; i < num_npc; i++)
                    {
                        textures[i] = npcs[i].GetTexture();
                        locs[i] = npcs[i].GetPos();
                    }
                    for (i = num_npc; i < num_npc + num_item; i++)
                    {
                        textures[i] = items[i + num_item].GetTexture();
                        locs[i] = items[i + num_item].GetPos();
                    }
                    walls = r.walls;
                    floor = r.floor;
                }
            }
        }
    }

    class ClientComparator : IComparer
    {
        public int Compare(object x, object y)
        {
            if (x == null && y != null)
                return 1;

            if (y == null && x != null)
                return -1;

            if (y == null && x == null)
                return 0;

            if( ((Actor)x).Active && !((Actor)y).Active )
            {
                return -1;
            }

            if (!((Actor)x).Active && ((Actor)y).Active)
            {
                return 1;
            }
            
            return 0;
           
        }
    }
}
