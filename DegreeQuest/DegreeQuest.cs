using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DegreeQuest;
using System;
using System.Threading;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace DegreeQuest
{
    /// <summary>
    /// 2D rogue-like game for CS 309 with Mitra
    /// By Sean Hinchee, Zach Boe, Zach Turley, and Dennis Xu
    /// Team 102
    /// </summary>

    public class DegreeQuest : Game
    {
        public static int North = 0;
        public static int South = 900 - 68;
        public static int West = 0;
        public static int East = 1600;
        public static Vector2 NW = new Vector2(West, North);
        public static Vector2 NE = new Vector2(East-64, North);
        public static Vector2 SW = new Vector2(West, South - 64);
        public static Vector2 SE = new Vector2(East-64, South-64);
        public static Vector2 MN = new Vector2((East -64 - West) / 2, North);
        public static Vector2 MS = new Vector2((East - 64 - West) / 2, South - 64);
        public static Vector2 ME = new Vector2(East-64, (South - 64 - North)/2);
        public static Vector2 MW = new Vector2(West, (South - 64 - North)/2);

        public String message = "";
        public static Vector2 message_loc = new Vector2(East / 2, South+10);

        double ClickTimer;
        const double TimerDelay = 500;
        DQServer srv = null;
        DQClient client = null;
        bool clientMode = false;
        bool serverMode = false;
        bool debugMode = false;
        string debugString = "nil";
        public Queue actions = new Queue();
        public Config conf;
        DQPostClient pclient = null;
        DQPostSrv psrv = null;
        public Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();

        public static string root = System.AppDomain.CurrentDomain.BaseDirectory + "..\\..\\..\\..";
        public static string state = "start";

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        public volatile PC pc;

        public volatile Dungeon dungeon;



        //states to determine keypresses
        KeyboardState currentKeyboardState;
        KeyboardState previousKeyboardState;
        //states for mouse
        MouseState currentMouseState;
        MouseState previousMouseState;

        int lastNum = -1;

        SpriteFont sf;

        /** End Variables **/

        public DegreeQuest()
        {
            graphics = new GraphicsDeviceManager(this);

            /* window resize code */
            IsMouseVisible = true;
            graphics.IsFullScreen = false;
            graphics.PreferredBackBufferWidth = 1600;
            graphics.PreferredBackBufferHeight = 900;
            graphics.ApplyChanges();


            Content.RootDirectory = "Content";
        }


        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            pc = new PC();

            dungeon = new Dungeon(pc);
            //dungeon.AddRoom(dungeon.index_x, dungeon.index_y  + 1);

            // initialise texture index
            sf = Content.Load<SpriteFont>("mono");

            string[] files = System.IO.Directory.GetFiles(root + "\\Content\\Bin\\DesktopGL\\Images");

            Console.WriteLine("Loading Textures...");
            foreach (string fname in files)
            {
                //Console.WriteLine(fname);
                var s = fname.Split('\\');
                var n = s[s.Length - 1].Split('.');
                var t = Content.Load<Texture2D>(root + "\\Content\\Bin\\DesktopGL\\Images\\" + n[0]);
                Textures.Add(n[0], t);
            }
            Console.WriteLine("Done loading Textures...");


            // server init logic ;; always serving atm
            conf = new Config();

            serverMode = conf.bget("server");
            clientMode = !serverMode;

            if (serverMode)
            {
                srv = new DQServer(this, conf);

                Thread srvThread = new Thread(new ThreadStart(srv.ThreadRun));
                srvThread.IsBackground = true;
                srvThread.Start();
                //srvThread.Join();
                Console.WriteLine("> Server Initialistion Complete!");

                //post
                psrv = new DQPostSrv(this, conf);

                Thread psrvThread = new Thread(new ThreadStart(psrv.ThreadRun));
                psrvThread.IsBackground = true;
                psrvThread.Start();
                Console.WriteLine("> POST Server Initialisation Complete!");

            }

            // client init logic
            if (clientMode)
            {
                client = new DQClient(this, conf);

                Thread clientThread = new Thread(new ThreadStart(client.ThreadRun));
                clientThread.Start();
                Console.WriteLine("> Client Initialisation Complete!");

                //post
                pclient = new DQPostClient(pc, this, conf);
                Thread pclientThread = new Thread(new ThreadStart(pclient.ThreadRun));
                pclientThread.Start();
                Console.WriteLine("> POST Client Initialisation Complete!");
            }

            pc.MoveSpeed = 8.0f;

            base.Initialize();
        }


        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here

            Vector2 playerPosition = new Vector2(GraphicsDevice.Viewport.TitleSafeArea.X + GraphicsDevice.Viewport.TitleSafeArea.Width / 2,
                GraphicsDevice.Viewport.TitleSafeArea.Y + GraphicsDevice.Viewport.TitleSafeArea.Height / 2);

            pc.Initialize(pc.GetTexture(), playerPosition);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }


        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Halt();


            // TODO: Add your update logic here

            if (state == "start")
            {
                Rectangle hostClick = new Rectangle(629, 400, 343, 67);
                Rectangle joinClick = new Rectangle(629, 500, 343, 67);
                previousMouseState = currentMouseState;
                currentMouseState = Mouse.GetState();
                if (currentMouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
                {
                    Point mouseloc = new Point(currentMouseState.X, currentMouseState.Y);
                    if (hostClick.Contains(mouseloc))
                    {
                        state = "game";
                    }
                    else if (joinClick.Contains(mouseloc))
                    {
                        state = "game";
                    }

                }
            }
            if (state == "login")
            {
                //TODO
            }
            if (state == "signup")
            {
                //TODO
            }
            if (state == "game")
            {
                previousKeyboardState = currentKeyboardState;
                currentKeyboardState = Keyboard.GetState();
                if (serverMode)
                {
                    UpdateServer(gameTime);
                }
                UpdatePlayer(gameTime);
            }
            else if (state == "inventory")
            {
                previousKeyboardState = currentKeyboardState;
                currentKeyboardState = Keyboard.GetState();
                previousMouseState = currentMouseState;
                currentMouseState = Mouse.GetState();
                if (currentKeyboardState.IsKeyDown(Keys.M) && previousKeyboardState.IsKeyUp(Keys.M))
                    state = "game";
                if(currentMouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
                {
                    UpdateInventory(gameTime);
                }

            }

            base.Update(gameTime);
        }

        private void UpdateInventory(GameTime gameTime)
        {
            ClickTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
            if(Mouse.GetState().LeftButton== ButtonState.Pressed)
            {
                if(ClickTimer < TimerDelay)
                {
                    //double click
                    Point mouseloc = new Point(currentMouseState.X, currentMouseState.Y);
                    Rectangle bag = new Rectangle(647, 272, 175, 350);
                    Rectangle head = new Rectangle(950, 318, 32, 32);
                    Rectangle chest = new Rectangle(950, 389, 32, 32);
                    Rectangle legs = new Rectangle(950, 462,32,32);
                    Rectangle rh = new Rectangle(871, 411, 32,32);
                    Rectangle lh = new Rectangle(1026, 411, 32, 32);
                    if(bag.Contains(mouseloc))
                    {
                        int i = (mouseloc.X - 647) / 35;
                        int j = (mouseloc.Y - 272) / 35;
                        if(pc.bag[i + (j * 10)] != null)
                            pc.bag[i + (j * 10)] = pc.equip(pc.bag[i + (j * 10)]);
                    }
                    if(head.Contains(mouseloc))
                    {
                        if(pc.equipment[0] != null)
                        {
                            pc.unequip(0);
                        }
                    }
                    if(chest.Contains(mouseloc))
                    {
                        pc.unequip(1);
                    }
                    if(legs.Contains(mouseloc))
                    {
                        pc.unequip(2);
                    }
                    if(rh.Contains(mouseloc))
                    {
                        pc.unequip(4);
                    }
                    if(lh.Contains(mouseloc))
                    {
                        pc.unequip(5);
                    }
                    
                }
                else
                {
                    ClickTimer = 0;
                }
            }

        }

        private void UpdateServer(GameTime gameTime)
        {
            NPC[] npcs = dungeon.currentRoom.GetNPCs();
            for (int i = 0; i < npcs.Length; i++)
            {


                //move npcs
                if (npcs[i].Active)
                {
                    npcs[i].Move(dungeon.currentRoom);
                    npcs[i].Position.X = MathHelper.Clamp(npcs[i].Position.X, West + 64, East - npcs[i].GetWidth() - 64);
                    npcs[i].Position.Y = MathHelper.Clamp(npcs[i].Position.Y, West + 64, East - npcs[i].GetWidth() - 64);
                }
            }
        }

        private void UpdatePlayer(GameTime gameTime)
        {
            previousMouseState = currentMouseState;
            currentMouseState = Mouse.GetState();
            Vector2 mousePos = currentMouseState.Position.ToVector2();

            if (currentKeyboardState.IsKeyDown(Keys.Left) || currentKeyboardState.IsKeyDown(Keys.A))
            {
                if(pc.CanMove(Bear.W, dungeon.currentRoom, conf))
                {
                    pc.Position.X -= pc.MoveSpeed;
                }
            }

            if (currentKeyboardState.IsKeyDown(Keys.Right) || currentKeyboardState.IsKeyDown(Keys.D))
            {
                if (pc.CanMove(Bear.E, dungeon.currentRoom, conf))
                    pc.Position.X += pc.MoveSpeed;
            }

            if (currentKeyboardState.IsKeyDown(Keys.Up) || currentKeyboardState.IsKeyDown(Keys.W))
            {
                if (pc.CanMove(Bear.N, dungeon.currentRoom, conf))
                    pc.Position.Y -= pc.MoveSpeed;
            }

            if (currentKeyboardState.IsKeyDown(Keys.Down) || currentKeyboardState.IsKeyDown(Keys.S))
            {
                if (pc.CanMove(Bear.S, dungeon.currentRoom, conf))
                    pc.Position.Y += pc.MoveSpeed;
            }
            //going to inventory screen
            if(currentKeyboardState.IsKeyDown(Keys.M) && previousKeyboardState.IsKeyUp(Keys.M))
            {
                state = "inventory";
            }
            // toggle player and npc sprites (for testing)
            if (currentKeyboardState.IsKeyDown(Keys.F5) && !previousKeyboardState.IsKeyDown(Keys.F5))
            {
                pc.number = (pc.number % 4) + 1;
                pc.SetTexture( "Player" + pc.number.ToString());
            }

            // toggle debug mode display (shows some debug information)
            if (currentKeyboardState.IsKeyDown(Keys.F2) && !previousKeyboardState.IsKeyDown(Keys.F2))
            {
                if (debugMode)
                    debugMode = false;
                else
                    debugMode = true;
            }
            //for changing rooms
            if (currentKeyboardState.IsKeyDown(Keys.F12) && !previousKeyboardState.IsKeyDown(Keys.F12) && serverMode == true)
            {
                //for testing purposes
                if (dungeon.index_x == 128 && dungeon.index_y == 128)
                {
                    dungeon.switchRooms(Direction.North);
                }
                else
                {
                    dungeon.switchRooms(Direction.South);
                }
            }

            /* spawn test item */
            if (currentKeyboardState.IsKeyDown(Keys.F3) && !previousKeyboardState.IsKeyDown(Keys.F3) && serverMode == true)
            {
                Item item = Item.Random();
                item.Initialize(item.name, Mouse.GetState().Position.ToVector2());
                dungeon.currentRoom.Add(item);
            }

            /* spawn test NPC */
            if (currentKeyboardState.IsKeyDown(Keys.F4) && !previousKeyboardState.IsKeyDown(Keys.F4) && serverMode == true)
            {

                NPC npc = NPC.Random();
                npc.Initialize(npc.name, Mouse.GetState().Position.ToVector2());
                if (npc.TryMove(dungeon.currentRoom,npc.GetPos()))
                {
                    dungeon.currentRoom.Add(npc);
                }
            }

            /* spawn test projectile that goes to 0,0 */
            if(((currentKeyboardState.IsKeyDown(Keys.F10) && !previousKeyboardState.IsKeyDown(Keys.F10)) ||
                    (currentKeyboardState.IsKeyDown(Keys.Space) && !previousKeyboardState.IsKeyDown(Keys.Space))) || (currentMouseState.RightButton == ButtonState.Pressed &&
                previousMouseState.RightButton == ButtonState.Released) && serverMode == true)
            {
                Projectile proj = new Projectile(pc, new Location(currentMouseState.X, currentMouseState.Y), 2, PType.Dot, new Location(pc.Position.X, pc.Position.Y));
               // Console.WriteLine("Mouse: " + currentMouseState.X + " : " + currentMouseState.Y);
               // Console.WriteLine("Bearing: " + proj.Bearing);

                proj.Initialize("dot", pc.Position.toVector2());
                dungeon.currentRoom.Add(proj);
            }

            
            if(currentMouseState.LeftButton == ButtonState.Pressed &&
                previousMouseState.LeftButton == ButtonState.Released &&
                Math.Abs(mousePos.X-(pc.GetPos().X+32))< 32 + PC.PC_MELEE_RANGE &&
                Math.Abs(mousePos.Y - (pc.GetPos().Y + 32)) < 32+PC.PC_MELEE_RANGE)
            {
                //Console.WriteLine("click");
                Actor target = dungeon.currentRoom.Occupying(mousePos);
                if (target != null)
                {
                    message = pc.Attack(target);
                    if (!target.Active)
                    {
                        dungeon.currentRoom.Delete(target);
                    }
                }
            }


            pc.kbState = currentKeyboardState.GetPressedKeys();
            pc.mLoc = new Location(currentMouseState.X, currentMouseState.Y);

            if (pc.Position.Y >= 320 && pc.Position.Y <= 448 || pc.Position.X >= 704 && pc.Position.X <= 832)
            {
                pc.Position.X = MathHelper.Clamp(pc.Position.X, West + 41, East - pc.GetWidth() - 41);
                pc.Position.Y = MathHelper.Clamp(pc.Position.Y, North + 41, South - pc.GetHeight() - 41);
            }
            else
            {
                pc.Position.X = MathHelper.Clamp(pc.Position.X, West + 64, East - pc.GetWidth() - 64);
                pc.Position.Y = MathHelper.Clamp(pc.Position.Y, North + 64, South - pc.GetHeight() - 64);
            }



            String pickup = dungeon.currentRoom.Pickup(pc);
            if(pickup != null) { message = pickup; }
            int i;
            if (serverMode)
            {
                int l = conf.iget("spriteLen");
                dungeon.checkRoomSwitch(pc);
                for (i = 0; i < dungeon.currentRoom.num; i++)
                {
                    var a = dungeon.currentRoom.members[i];


                    //move projectiles
                    if (a.GetAType() == AType.Projectile)
                    {
                        var p = (Projectile)a;
                        Actor who = dungeon.currentRoom.Occupying(p.GetPos());
                        if (Math.Abs(p.Position.X - p.Bearing.X) <= 1.5 && Math.Abs(p.Position.Y - p.Bearing.Y) <= 1.5)
                        {
                            //close enough to target
                            p.Active = false;
                            dungeon.currentRoom.Delete(a);
                        }
                        else if(who!= null && who.GetAType() == AType.NPC)
                        {
                            if (!((NPC)who).TakeHit(5))
                            {
                                message = "Killed enemy " + ((NPC)who).name + "!";
                                dungeon.currentRoom.Delete(who);
                            }
                            p.Active = false;
                            dungeon.currentRoom.Delete(p);
                        }
                        else
                        {
                            //move towards target
                            if (p.Position.X < p.Bearing.X)
                                a.Position.X += p.MoveSpeed;
                            else if (p.Position.X > p.Bearing.X)
                                a.Position.X -= p.MoveSpeed;

                            if (p.Position.Y < p.Bearing.Y)
                                a.Position.Y += p.MoveSpeed;
                            else if (p.Position.Y > p.Bearing.Y)
                                a.Position.Y -= p.MoveSpeed;

                        }
                    }
                }
            }
            

            /* system checks */
            if (serverMode)
            {
                if (psrv._halt || srv._halt)
                {
                    Halt();
                }
            }
            else if (clientMode)
            {
                if (pclient._halt || client._halt)
                {
                    Halt();
                }
            }
            
        }


        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // start drawing
            spriteBatch.Begin();

            if (state == "start")
            {
                Texture2D backgound = Textures["ISU-campus"];
                spriteBatch.Draw(backgound, new Vector2(0, 0), Color.White);
                Texture2D hostb = Textures["HostButton"];
                Texture2D joinb = Textures["joinButton"];
                spriteBatch.Draw(hostb, new Vector2(629, 400), Color.White);
                spriteBatch.Draw(joinb, new Vector2(629, 500), Color.White);
            }
            if (state == "login")
            {
                //TODO
            }
            if (state == "signup")
            {
                //TODO
            }
            if (state == "inventory")
            {
                lock (dungeon.currentRoom)
                {
                    Room r = dungeon.currentRoom;


                    for (int x = West; x < East; x += 64)
                    {
                        for (int y = North; y < South; y += 64)
                        {
                            spriteBatch.Draw(Textures["Floor" + r.floor.ToString()], new Vector2(x, y), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                        }
                    }
                    spriteBatch.Draw(Textures["NW" + r.walls.ToString()], NW, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    spriteBatch.Draw(Textures["NE" + r.walls.ToString()], NE, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    spriteBatch.Draw(Textures["SW" + r.walls.ToString()], SW, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    spriteBatch.Draw(Textures["SE" + r.walls.ToString()], SE, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    for (int x = West + 64; x < East - 64; x += 64)
                    {
                        if (x < MN.X - 128 || x > MN.X + 128)
                        {
                            spriteBatch.Draw(Textures["H" + r.walls.ToString()], new Vector2(x, North), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                            spriteBatch.Draw(Textures["H" + r.walls.ToString()], new Vector2(x, South - 64), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                        }
                    }
                    for (int y = North + 64; y < South - 64; y += 64)
                    {
                        if (y < ME.Y - 128 || y > ME.Y + 128)
                        {
                            spriteBatch.Draw(Textures["V" + r.walls.ToString()], new Vector2(West, y), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                            spriteBatch.Draw(Textures["V" + r.walls.ToString()], new Vector2(East - 64, y), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                        }
                    }

                    //spriteBatch.Draw(Textures["HDoor" + r.walls.ToString()], MN, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    for (int offset = -64; offset <= 64; offset += 64)
                    {
                        spriteBatch.Draw(Textures["HDoor" + r.walls.ToString()], new Vector2(MN.X + offset, MN.Y), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                        spriteBatch.Draw(Textures["HDoor" + r.walls.ToString()], new Vector2(MS.X + offset, MS.Y), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                        spriteBatch.Draw(Textures["VDoor" + r.walls.ToString()], new Vector2(ME.X, ME.Y + offset), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                        spriteBatch.Draw(Textures["VDoor" + r.walls.ToString()], new Vector2(MW.X, MW.Y + offset), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    }
                    spriteBatch.Draw(Textures["W" + r.walls.ToString()], new Vector2(MN.X + 128, North), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    spriteBatch.Draw(Textures["W" + r.walls.ToString()], new Vector2(MN.X + 128, South - 64), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    spriteBatch.Draw(Textures["E" + r.walls.ToString()], new Vector2(MN.X - 128, North), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    spriteBatch.Draw(Textures["E" + r.walls.ToString()], new Vector2(MN.X - 128, South - 64), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    spriteBatch.Draw(Textures["N" + r.walls.ToString()], new Vector2(East - 64, ME.Y + 128), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    spriteBatch.Draw(Textures["N" + r.walls.ToString()], new Vector2(West, ME.Y + 128), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    spriteBatch.Draw(Textures["S" + r.walls.ToString()], new Vector2(East - 64, ME.Y - 128), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    spriteBatch.Draw(Textures["S" + r.walls.ToString()], new Vector2(West, ME.Y - 128), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

                    //draw player
                    //pc.Draw(spriteBatch);
                    for (int i = 0; i < dungeon.currentRoom.num_item && i < dungeon.currentRoom.items.Length; i++) { DrawSprite(dungeon.currentRoom.items[i], spriteBatch); }
                    for (int i = 0; i < dungeon.currentRoom.num && i < dungeon.currentRoom.members.Length; i++) { DrawSprite(dungeon.currentRoom.members[i], spriteBatch); }
                    //current message
                    spriteBatch.DrawString(sf, message, message_loc, Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 0f);

                    float percentHP = (float)pc.HP / (float)pc.HPMax;
                    //current HP, damage
                    Texture2D rect = new Texture2D(graphics.GraphicsDevice, 400, 900 - South);
                    Color[] data = new Color[400 * (900 - South)];
                    for (int j = 0; j < data.Length * percentHP; j++) data[j] = Color.Green;
                    for (int j = (int)(data.Length * percentHP); j < data.Length; j++) data[j] = Color.Red;
                    rect.SetData(data);
                    spriteBatch.Draw(rect, new Vector2(0, South), Color.White);
                }

                /* debug mode draw */
                if (debugMode)
                {
                    string str = "";
                    if (clientMode)
                        str += "\nMode: Client";
                    if (serverMode)
                        str += "\nMode: Server";

                    debugString += str + "\nRoom cords: " + dungeon.index_x + "," + dungeon.index_y + "\n";

                    for (int i = 0; i < dungeon.currentRoom.num; i++)
                    {
                        debugString += i + ": " + dungeon.currentRoom.members[i].Position.ToString() + " ";
                    }

                    foreach (Room room in dungeon.Rooms)
                    {
                        //WARNING the debug for the client might not be 100% accurate since it doesn't get the entire Dungeon class                 
                        if (room != null)
                        {
                            debugString += "\nitem #: " + room.num_item + "\nactors: " + room.num;
                        }
                    }

                    spriteBatch.DrawString(sf, debugString, new Vector2(0, 2), Color.Black);

                    debugString = "nil";
                }

                Texture2D inv = new Texture2D(graphics.GraphicsDevice, 650, 400);
                Color[] color = new Color[650 * 430];
                for (int i = 0; i < color.Length; i++) color[i] = Color.LightGray;
                inv.SetData(color);
                spriteBatch.Draw(inv, new Vector2(445, 250), Color.White);
                spriteBatch.DrawString(sf, "Name: " + pc.Name, new Vector2(465, 270), Color.Black);
                spriteBatch.DrawString(sf, "HP: " + pc.HP + "/" + pc.HPMax, new Vector2(465, 300), Color.Black);
                spriteBatch.DrawString(sf, "EP: " + pc.EP + "/" + pc.EPMax, new Vector2(465, 330), Color.Black);
                spriteBatch.DrawString(sf, "Dept: " + pc.Debt, new Vector2(465, 360), Color.Black);
                spriteBatch.DrawString(sf, "Logic: " + pc.Stats[Stat.LOGIC], new Vector2(465, 390), Color.Black);
                spriteBatch.DrawString(sf, "Life: " + pc.Stats[Stat.LIFE], new Vector2(465, 420), Color.Black);
                spriteBatch.DrawString(sf, "Chem: " + pc.Stats[Stat.CHEM], new Vector2(465, 450), Color.Black);
                spriteBatch.DrawString(sf, "Tech: " + pc.Stats[Stat.TECH], new Vector2(465, 480), Color.Black);
                spriteBatch.DrawString(sf, "Math: " + pc.Stats[Stat.NUM], new Vector2(465, 510), Color.Black);
                spriteBatch.Draw(Textures["inventory"], new Vector2(645, 270));
                for (int i = 0; i < pc.bag.Length; i++)
                {
                    if (pc.bag[i] == null){ }
                    else { 
                        int x = 647 + ((i % 5) * 35);
                        int y = 272 + ((i / 5) * 35);
                        Console.WriteLine(pc.bag[i].name);
                        spriteBatch.Draw(Textures[pc.bag[i].name], new Vector2(x, y));
                    } 
                }
                for (int i = 0; i < pc.equipment.Length; i++)
                {
                    if(pc.equipment[i] != null)
                    {
                        if(i == 0)
                        {
                            spriteBatch.Draw(Textures[pc.equipment[i].name], new Vector2(950, 318));
                        }
                        else if (i == 1)
                        {
                            spriteBatch.Draw(Textures[pc.equipment[i].name], new Vector2(950, 389));
                        } 
                        else if(i == 2)
                        {
                            spriteBatch.Draw(Textures[pc.equipment[i].name], new Vector2(950, 462));
                        }
                        else if( i == 5 || i == 7)
                        {
                            spriteBatch.Draw(Textures[pc.equipment[i].name], new Vector2(871, 411));
                        }
                        else if( i == 6)
                        {
                            spriteBatch.Draw(Textures[pc.equipment[i].name], new Vector2(1026, 411));
                        }
                    }
                }

            }
            if (state == "game")
            {
                //Texture2D rect = new Texture2D(graphics.GraphicsDevice, 1600, 720);
                //Color[] data = new Color[1600* 720];
                //for (int j = 0; j < data.Length; j++) data[j] = Color.Green;
                //rect.SetData(data);
                //spriteBatch.Draw(rect, new Vector2(0, 90), Color.White);
                int floor, walls;
                lock (dungeon.currentRoom)
                {
                    floor = dungeon.currentRoom.floor;
                    walls = dungeon.currentRoom.walls;
                }
                    

                   for (int x = West; x < East; x += 64)
                    {
                        for (int y = North; y < South; y += 64)
                        {
                            spriteBatch.Draw(Textures["Floor" + floor.ToString()], new Vector2(x, y), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                        }
                    }
                    spriteBatch.Draw(Textures["NW" + walls.ToString()], NW, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    spriteBatch.Draw(Textures["NE" + walls.ToString()], NE, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    spriteBatch.Draw(Textures["SW" + walls.ToString()], SW, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    spriteBatch.Draw(Textures["SE" + walls.ToString()], SE, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    for (int x = West+64; x < East-64; x += 64)
                    {
                        if (x < MN.X-128 || x > MN.X + 128)
                        {
                            spriteBatch.Draw(Textures["H" + walls.ToString()], new Vector2(x, North), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                            spriteBatch.Draw(Textures["H" + walls.ToString()], new Vector2(x, South - 64), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                        }
                    }
                    for (int y = North + 64; y < South - 64; y += 64)
                    {
                        if (y < ME.Y - 128 || y > ME.Y + 128) {
                            spriteBatch.Draw(Textures["V" + walls.ToString()], new Vector2(West, y), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                            spriteBatch.Draw(Textures["V" + walls.ToString()], new Vector2(East - 64, y), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                        }
                    }

                    //spriteBatch.Draw(Textures["HDoor" + walls.ToString()], MN, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    for (int offset = -64; offset <= 64; offset += 64) {
                        spriteBatch.Draw(Textures["HDoor" + walls.ToString()], new Vector2(MN.X + offset, MN.Y), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                        spriteBatch.Draw(Textures["HDoor" + walls.ToString()], new Vector2(MS.X + offset, MS.Y), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                        spriteBatch.Draw(Textures["VDoor" + walls.ToString()], new Vector2(ME.X, ME.Y + offset), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                        spriteBatch.Draw(Textures["VDoor" + walls.ToString()], new Vector2(MW.X, MW.Y + offset), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    }
                    spriteBatch.Draw(Textures["W" + walls.ToString()], new Vector2(MN.X + 128, North), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    spriteBatch.Draw(Textures["W" + walls.ToString()], new Vector2(MN.X + 128, South - 64), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    spriteBatch.Draw(Textures["E" + walls.ToString()], new Vector2(MN.X - 128, North), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    spriteBatch.Draw(Textures["E" + walls.ToString()], new Vector2(MN.X - 128, South - 64), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    spriteBatch.Draw(Textures["N" + walls.ToString()], new Vector2(East-64, ME.Y + 128), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    spriteBatch.Draw(Textures["N" + walls.ToString()], new Vector2(West, ME.Y + 128), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    spriteBatch.Draw(Textures["S" + walls.ToString()], new Vector2(East-64, ME.Y - 128), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    spriteBatch.Draw(Textures["S" + walls.ToString()], new Vector2(West, ME.Y - 128), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

                    //draw player
                    //pc.Draw(spriteBatch);
                    for (int i = 0; i < dungeon.currentRoom.num_item && i < dungeon.currentRoom.items.Length; i++) { DrawSprite(dungeon.currentRoom.items[i], spriteBatch); }
                    for (int i = 0; i < dungeon.currentRoom.num && i < dungeon.currentRoom.members.Length; i++){ DrawSprite(dungeon.currentRoom.members[i], spriteBatch); }

                    //Bottom Bar:
                    Vector2 barSize = new Vector2(1600, 900 - South);
                    Texture2D rect = new Texture2D(graphics.GraphicsDevice, 1600, 900 - South);
                    Color[] data = new Color[1600 * (900 - South)];
                    for (int j = 0; j < data.Length; j++) data[j] = Color.Black;
                    rect.SetData(data);
                    spriteBatch.Draw(rect, new Vector2(0, South), Color.White);

                    float percentHP = (float)pc.HP / (float)pc.HPMax;
                    int middle = (int)(percentHP * 400);
                    //current HP
                    rect = new Texture2D(graphics.GraphicsDevice, 400, 900-South);
                    data = new Color[400*(900-South)];
                    for (int j = 0; j < data.Length; j++)
                    {
                        if (j % 400 < middle) { data[j] = Color.Green; }
                        else { data[j] = Color.Red; }
                    }
                    rect.SetData(data);
                    spriteBatch.Draw(rect, new Vector2(0, South), Color.White);
                    String HPString = pc.HP + "/" + pc.HPMax;
                    Vector2 HPSize = 3f*sf.MeasureString(HPString);
                    Vector2 HPLoc = new Vector2(200, South+ barSize.Y/2) - HPSize/2;
                    spriteBatch.DrawString(sf, HPString, HPLoc, Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 0f);

                //currentWeapon+Damage
                spriteBatch.DrawString(sf, "Weapon: ", new Vector2(420,South), Color.White, 0f, Vector2.Zero, 32f/sf.MeasureString("Weapon: ").Y, SpriteEffects.None, 0f);
                Vector2 weapon_loc = new Vector2(420 + sf.MeasureString("Weapon: ").X* 32f / sf.MeasureString("Weapon: ").Y, South);
                Item weapon = null;
                weapon = pc.equipment[(int)Item.IType.TwoHand];
                if (weapon == null) { weapon = pc.equipment[(int)Item.IType.OneHand]; }
                if (weapon == null) {
                    rect = new Texture2D(graphics.GraphicsDevice, 32, 32);
                    data = new Color[32 * 32];
                    for (int j = 0; j < data.Length; j++)
                    {
                        data[j] = Color.White;
                    }
                    rect.SetData(data);
                    spriteBatch.Draw(rect, weapon_loc, Color.White);
                }
                else { spriteBatch.Draw(Textures[weapon.name], weapon_loc, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f); }
                    
                    //current message
                    spriteBatch.DrawString(sf, message, message_loc, Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 0f);

                

                /* debug mode draw */
                if (debugMode)
                {
                    string str = "";
                    if (clientMode)
                        str += "\nMode: Client";
                    if (serverMode)
                        str += "\nMode: Server";
                
                    debugString += str + "\nRoom cords: " + dungeon.index_x + "," + dungeon.index_y + "\n";
                    
                    //for(int i = 0; i < dungeon.currentRoom.num; i++)
                    //{
                    //    debugString +=  i + ": " +dungeon.currentRoom.members[i].Position.ToString() + " ";
                    //}

                    foreach(Room room in dungeon.Rooms)
                    {
                        //WARNING the debug for the client might not be 100% accurate since it doesn't get the entire Dungeon class                 
                        if (room != null)
                        {
                            debugString += "\nitem #: " + room.num_item + "\nactors: " + room.num;
                        }
                    }

                    spriteBatch.DrawString(sf, debugString, new Vector2(0, 2), Color.Black);

                    debugString = "nil";
                }

            }
            base.Draw(gameTime);

            //stop draw
            spriteBatch.End();
        }


        /* Performs the shutdown routines */
        public void Halt()
        {

            if (serverMode)
            {
                psrv.Halt();
                srv.Halt();
            }
            if (clientMode)
            {
                client.Halt();
                pclient.Halt();
            }


            Exit();
        }


        /* loads another PC in */
        public void LoadPC(PC c, string texture)
        {
            // TODO: use this.Content to load your game content here

            Vector2 playerPosition;

            try
            {
                playerPosition = new Vector2(GraphicsDevice.Viewport.TitleSafeArea.X + GraphicsDevice.Viewport.TitleSafeArea.Width / 2,
                GraphicsDevice.Viewport.TitleSafeArea.Y + GraphicsDevice.Viewport.TitleSafeArea.Height / 2);
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine("Faulty LoadPC, null exception, not loading requested PC...");
                return;
            }



            c.Initialize(texture, playerPosition);
        }


        /* fetches the relevant Texture2D for a Texture string */
        public Texture2D LoadTexture(Actor a)
        {
            //this works, probably
            //return Content.Load<Texture2D>(root + "\\Content\\Graphics\\" + a.Texture);
            return Textures[a.GetTexture()];
        }

        /* acquires width of a sprite */
        public int Width(Actor a)
        {
            return LoadTexture(a).Width;
        }

        /* acquires width of a sprite */
        public int Height(Actor a)
        {
            return LoadTexture(a).Height;
        }

        /* Draw method for a Sprite */
        public void DrawSprite(Actor a, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(LoadTexture(a), a.Position.toVector2(), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        }

    }

    [Serializable()]
    public class Location
    {
        public float X;
        public float Y;

        public Location(float x, float y)
        {
            X = x;
            Y = y;
        }

        public Location(Vector2 v)
        {
            X = v.X;
            Y = v.Y;
        }

        public Location(string s)
        {
            string[] str = s.Split(';');
            if (str.GetLength(0) != 2)
            {
                Console.WriteLine("Error converting string to Location!");
                X = 1;
                Y = 1;
            }
            else
            {
                X = float.Parse(str[0]);
                Y = float.Parse(str[1]);
            }
        }

        public override string ToString()
        {
            string str = "";
            str += X.ToString();
            str += ";";
            str += Y.ToString();

            return str;
        }

        public Vector2 toVector2()
        {
            Vector2 v = new Vector2(X, Y);
            return v;
        }

    }
}
