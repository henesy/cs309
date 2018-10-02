using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace DegreeQuest
{
    class DQClient
    {
        TcpClient c = new TcpClient();
        DegreeQuest dq;
        public volatile Boolean _halt = false;
        int comSize;
        int spectatorPort;

        public DQClient(DegreeQuest mainDQ, Config conf)
        {
            dq = mainDQ;
            comSize = conf.getComSize();
            spectatorPort = Convert.ToInt32(conf.get("spectatorPort"));
        }

        public void ThreadRun()
        {

            try
            {
                c.Connect("127.0.0.1", spectatorPort);
            } catch(SocketException e)
            {
                Console.WriteLine("> Cannot connect to server on port :13337...ending Client...");
                _halt = true;
                return;
            }

            Console.WriteLine(">>> Client Connected!");
            NetworkStream serverStream = c.GetStream();

            if (c == null)
            {
                Console.WriteLine("CLIENT IS NULL!");
            }

            while (!_halt)
            {
                Byte[] byt2 = new Byte[comSize];

                try
                {
                    serverStream.Read(byt2, 0, comSize);
                } catch(System.IO.IOException e)
                {
                    Console.WriteLine(">>> Client failed to write to the server on port :13337...Client ending...");
                    _halt = true;
                    break;
                }

                
                string str = Util.bts(byt2);
                string[] locations = str.Split('@');

                //this is bad and unsafe and can cause crashes
                lock (dq.dungeon.currentRoom) {

                    string[] sub = locations[0].Split('#');

                    //Console.WriteLine("name: " + sub[2] + "," + sub[3]);
                    if (Convert.ToInt32(sub[2]) != dq.dungeon.index_x || Convert.ToInt32(sub[3]) != dq.dungeon.index_y)
                    {
                        dq.dungeon.switchRooms(Convert.ToInt32(sub[2]), Convert.ToInt32(sub[3]));
                        string[] pos = locations[1].Split('#');
                        dq.pc.Position = new Location(pos[0]);
                        //Console.WriteLine("ConvName: " + dq.dungeon.index_x + "," + dq.dungeon.index_y);
                        //Console.WriteLine("PC: " + dq.pc.Position.ToString());
                    }
                    

                    dq.dungeon.currentRoom.num = int.Parse(sub[0]);
                    dq.dungeon.currentRoom.num_item = int.Parse(sub[1]);

                    //Console.WriteLine("item_count: " + dq.rooms["secondary"].num_item);
                    //need to expand
                    int j;
                    for(j = 0; j < dq.dungeon.currentRoom.num && j < dq.dungeon.currentRoom.members.Length; j++)
                    {
                        PC tc = new PC();
                        dq.LoadPC(tc, tc.GetTexture());
                        dq.dungeon.currentRoom.members[j] = tc;
                    }

                    int i;
                    for (i = 0; i < dq.dungeon.currentRoom.num && i < dq.dungeon.currentRoom.members.Length; i++)
                    {
                        sub = locations[i+1].Split('#');
                        //Console.WriteLine(">>>SUB STRING: " + sub[0] + " then " + sub[1]);

                        dq.dungeon.currentRoom.members[i].Position = new Location(sub[0]);
                        dq.dungeon.currentRoom.members[i].SetTexture(sub[1]);
                    }
                    for (i = 0; i< dq.dungeon.currentRoom.num_item && i < dq.dungeon.currentRoom.items.Length; i++) //issue is that the second rooms items gets the values of the first room
                    {
                        //2#2#2@pos#tex@pos2#tex2@ipos#itex@ipos2#itex2@
                        dq.dungeon.currentRoom.items[i] = new Item();
                        sub = locations[i+dq.dungeon.currentRoom.num+1].Split('#');
                        //Console.WriteLine(">>>SUB STRING: " + sub[0] + " then " + sub[1]);

                        dq.dungeon.currentRoom.members[i].SetTexture(sub[1]);
                        dq.dungeon.currentRoom.items[i].Position = new Location(sub[0]);
                    
                    }
                }

                Thread.Sleep(5);
            }
            _halt = true;
        }

        public void Halt()
        {
            _halt = true;
        }
    }


    class DQPostClient
    {
        TcpClient c = new TcpClient();
        Vector2 pos;
        PC pc;
        DegreeQuest dq;
        public volatile Boolean _halt = false;
        int postPort;

        public DQPostClient(PC mainPC, DegreeQuest mainDQ, Config conf)
        {
            pc = mainPC;
            dq = mainDQ;
            postPort = Convert.ToInt32(conf.get("postPort"));
        }

        public void ThreadRun()
        {
            try
            {
                c.Connect(dq.conf.get("srvAddr"), postPort);
            } catch(SocketException e)
            {
                Console.WriteLine("> Cannot connect to server on port :13337...ending POST Client...");
                _halt = true;
                return;
            }
            
            Console.WriteLine(">>> POST Client Connected!");
            NetworkStream srvStream = c.GetStream();

            if (c == null)
            {
                Console.WriteLine("POST CLIENT IS NULL!");
            }

            //var js = new JavaScriptSerializer();
            BinaryFormatter bin = new BinaryFormatter();

            while (!_halt)
            {
                try
                {
                    bin.Serialize(srvStream, pc);
                } catch(Exception e)
                {
                    Console.WriteLine(">>> Failed to serialize to server...ending POST Client...");
                    return;
                }

                srvStream.Flush();

                /* submit to server and read back ;; update relevant fields */

                //disabled due to performance issues
                //PC tc = (PC)bin.Deserialize(srvStream);
                //pc.Position = tc.Position;
                //pc.Texture = tc.Texture;

                //wrap up

                Thread.Sleep(5);
            }
            _halt = true;
        }

        public void Halt()
        {
            _halt = true;
        }
    }
}
