using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Collections;
using System.Threading;
using Microsoft.Xna.Framework;
using System.Web.Script.Serialization;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.Serialization.Formatters.Binary;

namespace DegreeQuest
{
    class DQServer
    {
        ClientList clients;
        DegreeQuest dq;
        public volatile Boolean _halt = false;
        TcpListener srv;
        int spectatorPort;

        public DQServer(DegreeQuest mainDQ, Config conf)
        {
            dq = mainDQ;
            spectatorPort = Convert.ToInt32(conf.get("spectatorPort"));
        }

        public void DQSInit()
        {
            srv = new TcpListener(spectatorPort);
            clients = new ClientList();

            srv.Start();
            Console.WriteLine(">>> Server started");

            while (!_halt)
            {
                TcpClient client;
                try
                {
                    client = srv.AcceptTcpClient();
                } catch(SocketException e)
                {
                    Console.WriteLine(">>> Server got Socket Exception on Accept...probably Halt message...Server ending...");
                    _halt = true;
                    return;
                }


                clients.Add(client);
                Handler h = new Handler(client, dq, _halt);

                //handle concurrently 
                Thread handler = new Thread(new ThreadStart(h.ThreadRun));

                try
                {
                    handler.Start();
                    //handler.Join();
                }
                catch (ThreadStateException e)
                {
                    Console.WriteLine(e);
                }
                catch (ThreadInterruptedException e)
                {
                    Console.WriteLine(e);
                }
            }

            _halt = true;
            Console.WriteLine(">>> DQSInit Ending!");
        }

        public void ThreadRun()
        {
            this.DQSInit();
        }

        /* Post player locations to all clients */
        public void WriteAll(Vector2 pos)
        {
            int i;
            for (i = 0; i < clients.Length(); i++)
            {
                TcpClient c = clients.Get(i);

                //post location
            }
        }

        public void Halt()
        {
            _halt = true;
            srv.Stop();
        }

    }


    class Handler
    {
        TcpClient c;
        DegreeQuest dq;
        public volatile Boolean _halt2 = false;

        //threading and locks on clients variable 
        public Handler(TcpClient client, DegreeQuest mainDQ, Boolean _halt)
        {
            c = client;
            dq = mainDQ;
            _halt2 = _halt;
        }

        public void ThreadRun()
        {
            Console.WriteLine(">>> Handler Thread Started!");

            while (!_halt2)
            {
                //dq.dungeon.checkRoomSwitch();
                string str = dq.dungeon.currentRoom.num.ToString()+"#"+dq.dungeon.currentRoom.num_item.ToString()+ "#" + dq.dungeon.index_x + "#" + dq.dungeon.index_y + "@";

                int i;
                for (i = 0; i < dq.dungeon.currentRoom.num; i++)
                {
                    str += dq.dungeon.currentRoom.members[i].Position.ToString() + "#" + dq.dungeon.currentRoom.members[i].GetTexture() + "@";
                    Console.Write("PC: " + i + "; pos: " + dq.dungeon.currentRoom.members[i].Position.ToString());
                }
                for (i = 0; i < dq.dungeon.currentRoom.num_item; i++)
                {
                    str += dq.dungeon.currentRoom.items[i].Position.ToString() + "#" + dq.dungeon.currentRoom.items[i].GetTexture() + "@";
                }
                //2#2#2@pos#tex@pos2#tex2@ipos#itex@ipos2#itex2@
                Console.WriteLine(">>> STR IS: " + str);

                NetworkStream networkStream = c.GetStream();
 
                Byte[] byt2 = Util.stb(str);

                try
                {
                    networkStream.Write(byt2, 0, byt2.Length);

                    networkStream.Flush();
                } catch(Exception e)
                {
                    Console.WriteLine(">>> Exception on write, client disconnected...ending Handler...");
                    break;
                }

                Thread.Sleep(5);
            }

            Console.WriteLine(">>> Handler Ending! ");
        }

        public void Halt()
        {
            _halt2 = true;
            c.Close();
        }
    }


    /* New list type to handle locking/sychronization cleaner (internally) */
    class ClientList
    {
        private ArrayList clients;


        public ClientList()
        {
            this.clients = new ArrayList();
        }

        /* lock-able methods with try/catch (for pulse) */

        public TcpClient Get(int i)
        {
            TcpClient s = default(TcpClient);


            lock (this)
            {
                //do things here
                s = ((TcpClient)clients.ToArray()[i]);

                Monitor.Pulse(this);   // Pulse tells we are done reading
            }

            return s;
        }

        public int Add(TcpClient s)
        {
            int size = this.Length();

            lock (this)
            {
                clients.Add(s);

                Monitor.Pulse(this);
            }

            return size;
        }

        public int Length()
        {
            return (clients.ToArray().Length);
        }
    }


    class DQPostSrv
    {
        ClientList clients;
        DegreeQuest srvDQ;
        public volatile Boolean _halt = false;
        TcpListener srv;
        int comSize;
        int postPort;

        public DQPostSrv(DegreeQuest hostDQ, Config conf)
        {
            srvDQ = hostDQ;
            comSize = conf.getComSize();
            postPort = Convert.ToInt32(conf.get("postPort"));
        }

        public void PostInit()
        {
            srv = new TcpListener(postPort);
            clients = new ClientList();

            srv.Start();
            Console.WriteLine(">>> POST Server Started");

            while (!_halt)
            {
                TcpClient client;
                try
                {
                    client = srv.AcceptTcpClient();
                }
                catch (SocketException e)
                {
                    Console.WriteLine(">>> POST Server got Socket Exception on Accept...probably Halt message...POST Server ending...");
                    _halt = true;
                    return;
                }

                clients.Add(client);
                PostHandler h = new PostHandler(client, srvDQ, _halt, comSize);

                //handle concurrently 
                Thread handler = new Thread(new ThreadStart(h.ThreadRun));


                try
                {
                    handler.Start();
                    //handler.Join();
                }
                catch (ThreadStateException e)
                {
                    Console.WriteLine(e);
                }
                catch (ThreadInterruptedException e)
                {
                    Console.WriteLine(e);
                }
            }

            _halt = true;
            Console.WriteLine(">>> DQSInit Ending!");
        }

        public void ThreadRun()
        {
            this.PostInit();
        }

        public void Halt()
        {
            _halt = true;
            srv.Stop();
        }

        /* Manages communications with a client on port :13338 for movement/deltas and changes and such */
        class PostHandler
        {
            TcpClient c;
            PC cc; //client character
            DegreeQuest srvDQ;
            public volatile Boolean _halt2 = false;
            int comSize;

            public PostHandler(TcpClient client, DegreeQuest hostDQ, Boolean _halt, int comSiz)
            {
                c = client;
                cc = null;
                srvDQ = hostDQ;
                _halt2 = _halt;
                comSize = comSiz;
            }

            public void ThreadRun()
            {
                Console.WriteLine(">>> POST Handler Thread Started!");
                cc = new PC();



                NetworkStream cStream = c.GetStream();
                byte[] inStream = new byte[comSize];

                //establish locations/init client "player" object
                srvDQ.dungeon.currentRoom.Add(cc);

                srvDQ.LoadPC(cc, cc.GetTexture());



                Console.WriteLine(">>> POST Handler Entering Primary Loop!");

                var js = new JavaScriptSerializer();
                BinaryFormatter bin = new BinaryFormatter();

                Microsoft.Xna.Framework.Input.Keys[] lastkb = cc.kbState;
                Location lastm = cc.mLoc;

                while (!_halt2)
                {
                    try
                    {

                        //do things here
                        //katie was here
                        PC tc = (PC)bin.Deserialize(cStream);
                        cc.Position = tc.Position;
                        cc.SetTexture( tc.GetTexture());
                        //cc = tc;

                        /* read from client and then do processing things, probably with tc.LastAction */
                        //Console.WriteLine("KB State: " + tc.kbState.ToString());

                        
                        if (tc.kbState != null)
                        {
                            //Console.WriteLine("KB State: " + tc.kbState.ToString());

                            foreach (var k in tc.kbState)
                            {
                                if(k == Microsoft.Xna.Framework.Input.Keys.F10 && !lastkb.Contains(Microsoft.Xna.Framework.Input.Keys.F10))
                                {
                                    //shoot command
                                    Projectile proj = new Projectile(cc, new Location(tc.mLoc.X, tc.mLoc.Y), 2, PType.Dot, new Location(tc.Position.X, tc.Position.Y));
                                    proj.Initialize("dot", cc.Position.toVector2());
                                    srvDQ.dungeon.currentRoom.Add(proj);
                                }
                            }
                        }

                        lastkb = tc.kbState;

                        if(tc.mLoc != null)
                        {
                            //mouse things
                            //Console.WriteLine("Mouse State: " + tc.mLoc.ToString());

                        }

                        lastm = tc.mLoc;

                        

                        /* write the (potentially modified) temporary character back to the client */

                        // disabled due to temporary performance issues
                        //bin.Serialize(cStream, tc);

                        cStream.Flush();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        break;
                    }

                    Thread.Sleep(5);
                }

                srvDQ.dungeon.currentRoom.Delete(cc);

                Console.WriteLine(">>> POST Handler Ending! ");
            }

        }
        //danger?
    }
}
