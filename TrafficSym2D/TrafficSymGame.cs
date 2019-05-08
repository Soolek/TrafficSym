using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
//using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
//using Microsoft.Xna.Framework.Net;
//using Microsoft.Xna.Framework.Storage;
using System.Xml;
using System.IO;
using System.Threading.Tasks;
using TrafficSym2D.Enums;
using TrafficSym2D.LBM;

namespace TrafficSym2D
{
    /*
     * Time in seconds
     * Distance in meters
     * 
     * Speed in meters/second
     * Acceleration 1g = 10m/s^2
     */

    /// <summary>
    /// Tomasz Sulkowski
    /// www.CodeAndDrive.com
    /// Created: 19.04.2009
    /// City traffic simulator 2D
    /// </summary>
    public class TrafficSymGame : Game
    {
        public int resX = 0;
        public int resY = 0;

        private int mapsLoadedState = 0;

        public int elementSize = 5; //Lattice grid size
        public static float vectorLength = 5f;

        public int countX = 0;
        public int countY = 0;

        public Random rand = new Random();
        public GameState gameState = 0;

        public bool doAi = true;
        public bool doAutomaticLBM = true;
        public bool doAutomaticCars = true;

        private TabLBMSerializer _tabLBMSerializer;
        private LBMController _lbmController;

        //XNA graphics
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        //font
        public SpriteFont defaultFont;

        //map
        public Texture2D mapTexture;
        Rectangle viewportRect;

        //route configs
        List<RouteConfig> routeConfigList;

        //lights configs
        List<RouteWall> lightList;
        List<LightConfig> lightConfigList;

        //cars
        public static int carSpritesCount = 4;
        public List<Car> cars = new List<Car>();
        public Car selectedCar;
        private int currentCarCount = 0;
        private long finishedCarCount = 0;

        //LBM implementation
        public LBMElement[,] lightTabLBM; //dodatkowe sciany do swiatel
        LBMElement[][,] tabLBM;
        private int _currentTabLBMIndex = 0;
        public int currentTabLBMIndex
        {
            get
            {
                if (_currentTabLBMIndex < 0) _currentTabLBMIndex = 0;
                if (_currentTabLBMIndex >= routeConfigList.Count) _currentTabLBMIndex = routeConfigList.Count - 1;
                return _currentTabLBMIndex;
            }
            set
            {
                _currentTabLBMIndex = value;
            }
        }

        //graphics drawing
        public Texture2D texBrake;
        public Texture2D texAcc;
        public Texture2D texSteer;

        public Texture2D texWall;
        public Texture2D texVector;

        //csv
        private CsvRecorder _csvRecorder;

        public TrafficSymGame(Dictionary<string, string> arguments)
        {
            Content.RootDirectory = "Content";
            Config.SetParameters(arguments);

            graphics = new GraphicsDeviceManager(this);
        }

        protected override void Initialize()
        {
            mapTexture = LoadMap(Path.Combine("Configs", Config.ConfigDir, "map.tga"));
            SetVariablesBasedOnMap(mapTexture);

            _tabLBMSerializer = new TabLBMSerializer(Path.Combine("Configs", Config.ConfigDir));
            _lbmController = new LBMController(countX, countY, elementSize);
            GeneralHelper.parent = this;

            graphics.PreferredBackBufferWidth = resX;
            graphics.PreferredBackBufferHeight = resY;
            graphics.IsFullScreen = false;
            graphics.ApplyChanges();

            if (Config.RecordToCsv)
            {
                var csvFileName = string.Format("TrafficSym_{0}-{1}.csv", Config.ConfigDir, DateTime.Now.ToString("dd_MM_yy_HH_mm"));
                _csvRecorder = new CsvRecorder(Path.Combine("Configs", Config.ConfigDir, csvFileName));
            }

            base.Initialize();
        }

        private void SetVariablesBasedOnMap(Texture2D mapTexture)
        {
            resX = mapTexture.Width;
            resY = mapTexture.Height;
            countX = (resX / elementSize) + 1;
            countY = (resY / elementSize) + 1;
        }

        private Texture2D LoadMap(string mapPath)
        {
            if (File.Exists(mapPath))
            {
                FileStream fs = File.Open(mapPath, FileMode.Open);
                var mapTexture = Texture2D.FromStream(GraphicsDevice, fs);
                fs.Close();

                return mapTexture;
            }
            else
            {
                throw new IOException(string.Format("Missing map file: {0}", mapPath));
            }
        }

        internal Color[] _colorMap;
        protected override void LoadContent()
        {
            this.IsMouseVisible = true;
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            viewportRect = new Rectangle(0, 0, graphics.GraphicsDevice.Viewport.Width, graphics.GraphicsDevice.Viewport.Height);

            //font
            defaultFont = Content.Load<SpriteFont>("arial12");

            //car textures
            texAcc = Content.Load<Texture2D>("car_gadgets\\acc");
            texBrake = Content.Load<Texture2D>("car_gadgets\\brake");
            texSteer = Content.Load<Texture2D>("car_gadgets\\steer");

            //LBM textures
            texWall = Content.Load<Texture2D>("gas\\wall");

            //config load
            var xmlSettings = XmlSettings.CreateFromFile(Path.Combine("Configs", Config.ConfigDir, "settings.xml"), this);
            this.routeConfigList = xmlSettings.RouteConfigList;
            this.lightList = xmlSettings.LightList;
            this.lightConfigList = xmlSettings.LightConfigList;

            //inicjacja tabLBM
            lightTabLBM = new LBMElement[countX, countY];
            tabLBM = new LBMElement[routeConfigList.Count][,];
            for (int i = 0; i < routeConfigList.Count; i++)
                tabLBM[i] = new LBMElement[countX, countY];

            //getting all color data beforehand
            _colorMap = new Color[resX * resY];
            mapTexture.GetData<Color>(0, new Rectangle(0, 0, resX, resY), _colorMap, 0, resX * resY);

            //okreslanie tabLogicMap + instancjonowanie tabeli elementow
            for (int x = 0; x < countX; x++)
                for (int y = 0; y < countY; y++)
                {
                    //wyciaganie koloru z logic map
                    bool wall = false;

                    if (x == 0 || y == 0 || x == countX - 1 || y == countY - 1)
                        wall = true;

                    for (int tx = x * elementSize; ((tx < x * elementSize + elementSize) && (tx < resX)); tx++)
                    {
                        if (wall) break;
                        for (int ty = y * elementSize; ((ty < y * elementSize + elementSize) && (ty < resY)); ty++)
                        {
                            if (wall) break;
                            var retrievedColor = _colorMap[ty * resX + tx];
                            if ((retrievedColor.A > 254) && (retrievedColor.G > 128))
                                wall = true;
                        }
                    }

                    lightTabLBM[x, y] = new LBMElement();
                    for (int i = 0; i < routeConfigList.Count; i++)
                    {
                        tabLBM[i][x, y] = new LBMElement();

                        if (wall)
                            tabLBM[i][x, y].isWall = true;
                        else
                            tabLBM[i][x, y].isNormal = true;
                    }
                }

            //wkladanie w odpowiednie tabLBM pkt startowe, koncowe i sciany
            for (int i = 0; i < routeConfigList.Count; i++)
            {
                RouteConfig rc = routeConfigList[i];
                foreach (RouteStart rs in rc.routeStart)
                    LBMController.DrawLineLBM(tabLBM[i], elementSize, rs.x1, rs.y1, rs.x2, rs.y2, LBMNodeType.Source);
                foreach (RouteEnd re in rc.routeEnd)
                    LBMController.DrawLineLBM(tabLBM[i], elementSize, re.x1, re.y1, re.x2, re.y2, LBMNodeType.Sink);
                foreach (RouteWall rw in rc.routeWall)
                    LBMController.DrawLineLBM(tabLBM[i], elementSize, rw.x1, rw.y1, rw.x2, rw.y2, LBMNodeType.Wall);
            }

            base.LoadContent();
        }

        TimeSpan lightLastChangeTime = new TimeSpan(0);
        int lightConfigId = -1;

        MouseState prevMouseState;
        int posx, posy;

        private long _frameCount = 0;

        protected override void Update(GameTime gameTime)
        {
            _frameCount++;
            //GameTime customGameTime = new GameTime(gameTime.TotalRealTime, new TimeSpan(200000), gameTime.TotalGameTime, gameTime.ElapsedGameTime);

            // Allows the game to exit
            KeyboardState keybstate = Keyboard.GetState();
            if (keybstate.IsKeyDown(Keys.Escape))
                this.Exit();

            //stany symulacji
            foreach (Keys k in keybstate.GetPressedKeys())
                switch (k)
                {
                    case Keys.D0: gameState = GameState.PauseEdit; break;
                    case Keys.D1: gameState = GameState.SimulateLBM; break;
                    case Keys.D2: gameState = GameState.SimulateTraffic; break;
                }

            if (gameState == GameState.PauseEdit)
            {
                MouseState mousestate = Mouse.GetState();
                if (prevMouseState == null) prevMouseState = mousestate;

                if ((prevMouseState.LeftButton == ButtonState.Released) && (mousestate.LeftButton == ButtonState.Pressed))
                {
                    posx = mousestate.X;
                    posy = mousestate.Y;
                }

                if ((prevMouseState.LeftButton == ButtonState.Pressed) && (mousestate.LeftButton == ButtonState.Released))
                {
                    if (!((mousestate.X == posx) && (mousestate.Y == posy)))
                    {
                        //zapis do pliku
                        FileStream fs = File.Open("temp.txt", FileMode.Append);
                        StreamWriter sw = new StreamWriter(fs);
                        String s = "<wall x1=\"" + posx.ToString() + "\" y1=\"" + posy.ToString() + "\" x2=\"" + mousestate.X.ToString() + "\" y2=\"" + mousestate.Y.ToString() + "\"/>";
                        sw.WriteLine(s);
                        sw.Close();
                        fs.Close();
                    }
                }
                //na koniec
                prevMouseState = mousestate;
            }

            //LBM
            if ((gameState == GameState.SimulateLBM))
            {
                _lbmController.Update(tabLBM[currentTabLBMIndex]);

                //Automatic LBM maps generation - stops when all vector maps have valid routes
                if (doAutomaticLBM && _lbmController.HasRouteVectorMapGenerated(tabLBM[currentTabLBMIndex], routeConfigList[currentTabLBMIndex]))
                {
                    _tabLBMSerializer.SaveTabLBM(tabLBM[currentTabLBMIndex], currentTabLBMIndex);
                    if (currentTabLBMIndex == routeConfigList.Count - 1)
                    {
                        doAutomaticLBM = false;
                    }
                    else
                    {
                        currentTabLBMIndex++;
                    }
                }
            }

            if (gameState == GameState.SimulateTraffic)
            {
                //Automatic car adding
                if (doAutomaticCars)
                {
                    for (int i = 0; i < routeConfigList.Count; i++)
                    {
                        RouteConfig rc = routeConfigList[i];
                        if (gameTime.TotalGameTime.TotalMilliseconds > (rc.lastCarOutTime.TotalMilliseconds + rc.timeBetweenCarsMs))
                        {
                            rc.lastCarOutTime = gameTime.TotalGameTime;

                            if (Config.EndAfterCarsSpawned > 0)
                            {
                                if (_carIndex > Config.EndAfterCarsSpawned)
                                {
                                    if (cars.Count == 0)
                                    {
                                        this.Exit();
                                    }
                                    break;
                                }
                            }

                            AddNewCar(i, gameTime);
                        }
                    }
                }

                //Lights handling
                if (lightConfigList.Count > 0)
                {
                    if (lightConfigId == -1)
                    {
                        lightConfigId = 0;
                        lightLastChangeTime = gameTime.TotalGameTime;
                        for (int x = 0; x < lightList.Count; x++)
                            if (!lightConfigList[lightConfigId].lightId.Contains(x)) //jak zawiera oznacza ze ma byc to swiatlo zielone i nie malowac...
                            {
                                RouteWall rw = lightList[x];
                                LBMController.DrawLineLBM(lightTabLBM, elementSize, rw.x1, rw.y1, rw.x2, rw.y2, LBMNodeType.Wall);
                            }
                    }

                    //zmiana swiatel
                    if (gameTime.TotalGameTime.TotalMilliseconds > (lightLastChangeTime.TotalMilliseconds + lightConfigList[lightConfigId].timeToWaitMs))
                    {
                        //malowanie powrotne pustych kratek
                        for (int x = 0; x < lightList.Count; x++)
                        {
                            RouteWall rw = lightList[x];
                            LBMController.DrawLineLBM(lightTabLBM, elementSize, rw.x1, rw.y1, rw.x2, rw.y2, 0);
                        }
                        //malowanie nowych scian
                        lightConfigId++;
                        if (lightConfigId > (lightConfigList.Count - 1)) lightConfigId = 0;
                        lightLastChangeTime = gameTime.TotalGameTime;
                        for (int x = 0; x < lightList.Count; x++)
                            if (!lightConfigList[lightConfigId].lightId.Contains(x)) //jak zawiera oznacza ze ma byc to swiatlo zielone i nie malowac...
                            {
                                RouteWall rw = lightList[x];
                                LBMController.DrawLineLBM(lightTabLBM, elementSize, rw.x1, rw.y1, rw.x2, rw.y2, LBMNodeType.Wall);
                            }
                    }
                }

                List<Car> carsToRemove = new List<Car>();

                //cars
                Parallel.ForEach(cars, car =>
                {
                    try
                    {
                        //sterowanie ai
                        if (doAi)
                        {
                            car.DoAI(tabLBM[car.tabLBMIndex]);
                        }
                        if (car.Equals(selectedCar)) car.DoManualSteer(keybstate);

                        car.Update(gameTime);

                        if (_csvRecorder != null)
                        {
                            _csvRecorder.AddData(_frameCount, gameTime.TotalGameTime, lightConfigId, car);
                        }

                        //wywalanie z listy jak dojechal do konca
                        if (
                            ((car.position.X / elementSize) >= countX - 1) || ((car.position.Y / elementSize) >= countY - 1) 
                            || ((car.position.X / elementSize) <= 1) || ((car.position.Y / elementSize) <= 1)
                            || (tabLBM[car.tabLBMIndex][(int)car.position.X / elementSize, (int)car.position.Y / elementSize].isSink)
                            || (tabLBM[car.tabLBMIndex][(int)car.framePointF.X / elementSize, (int)car.framePointF.Y / elementSize].isSink)
                            )
                            carsToRemove.Add(car);
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        throw;
#endif
                        carsToRemove.Add(car);
                    }
                });

                currentCarCount = cars.Count;
                finishedCarCount += carsToRemove.Count;

                if (_csvRecorder != null)
                {
                    _csvRecorder.Flush();
                }

                foreach (Car car in carsToRemove)
                    cars.Remove(car);
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(blendState: BlendState.AlphaBlend);

            KeyboardState keybstate = Keyboard.GetState();

            //jak mapy nie za³adowane to za³aduj...
            if (mapsLoadedState < 2)
            {
                mapsLoadedState++;

                spriteBatch.DrawString(defaultFont, "Loading maps...", new Vector2(10, (float)(resY / 2.0)), Color.White);

                base.Draw(gameTime);
                spriteBatch.End();

                if (mapsLoadedState > 1)
                {
                    for (int i = 0; i < routeConfigList.Count; i++)
                    {
                        LBMElement[,] temp = _tabLBMSerializer.LoadTabLBM(i);
                        if (temp != null) tabLBM[i] = temp;
                    }
                }

                if (tabLBM.Count() > 0)
                {
                    gameState = GameState.SimulateLBM;
                }

                if (AllConfigsGenerated())
                {
                    gameState = GameState.SimulateTraffic;
                }

                return;
            }

            //logic map?
            spriteBatch.Draw(mapTexture, viewportRect, Color.White);

            //LGA
            if (gameState == GameState.SimulateLBM)
            {
                if (keybstate.IsKeyDown(Keys.Up))
                    currentTabLBMIndex++;
                else if (keybstate.IsKeyDown(Keys.Down))
                    currentTabLBMIndex--;

                if (keybstate.IsKeyDown(Keys.A))
                    doAutomaticLBM = !doAutomaticLBM;

                //zapis / odczyt
                if (keybstate.IsKeyDown(Keys.S))
                    _tabLBMSerializer.SaveTabLBM(tabLBM[currentTabLBMIndex], currentTabLBMIndex);
                else
                    if (keybstate.IsKeyDown(Keys.L))
                        tabLBM[currentTabLBMIndex] = _tabLBMSerializer.LoadTabLBM(currentTabLBMIndex);
                    else
                        if (keybstate.IsKeyDown(Keys.K))
                        {
                            for (int i = 0; i < routeConfigList.Count; i++)
                            {
                                LBMElement[,] temp = _tabLBMSerializer.LoadTabLBM(i);
                                if (temp != null) tabLBM[i] = temp;
                            }
                        }
                        else
                            for (int x = 0; x < countX; x++)
                                for (int y = 0; y < countY; y++)
                                {
                                    int xelem = x * elementSize;
                                    int yelem = y * elementSize;

                                    LBMElement l = tabLBM[currentTabLBMIndex][x, y];
                                    if (l.isWall)
                                        spriteBatch.Draw(texWall, new Rectangle(xelem, yelem, elementSize, elementSize), Color.Gray);
                                    else if (l.isSource)
                                    {
                                        spriteBatch.Draw(texWall, new Rectangle(xelem, yelem, elementSize, elementSize), new Color(0, 255, 0));
                                        spriteBatch.Draw(texWall, new Rectangle(xelem + 1, yelem + 1, elementSize - 2, elementSize - 2), new Color(255, 0, 0));
                                    }
                                    else if (l.isSink)
                                    {
                                        spriteBatch.Draw(texWall, new Rectangle(xelem, yelem, elementSize, elementSize), new Color(0, 255, 0));
                                        spriteBatch.Draw(texWall, new Rectangle(xelem + 1, yelem + 1, elementSize - 2, elementSize - 2), new Color(0, 0, 255));
                                    }
                                    else
                                    {
                                        //tlo
                                        spriteBatch.Draw(texWall, new Rectangle(x * elementSize, y * elementSize, elementSize, elementSize), new Color((l.density > 0 ? l.density : 0f), 0, (l.density < 0 ? -l.density : 0f)));
                                        //strzalka
                                        int xelem2 = xelem + (elementSize / 2);
                                        int yelem2 = yelem + (elementSize / 2);
                                        DrawLine(texWall, new Vector2(xelem2, yelem2), new Vector2(xelem2 + l.x * vectorLength, yelem2 + l.y * vectorLength), Color.White);
                                        spriteBatch.Draw(texWall, new Rectangle(xelem2, yelem2, 1, 1), new Color(0, 255, 0));
                                    }
                                }

                //malowanie stanow symulacji gazu
                int linePos = 0;
                spriteBatch.DrawString(defaultFont, "tabLBM index: " + currentTabLBMIndex.ToString(), new Vector2(1, linePos+=20), Color.White);
                spriteBatch.DrawString(defaultFont, "doAutomaticLBM: " + doAutomaticLBM.ToString(), new Vector2(1, linePos+=20), Color.White);

                MouseState mousestate = Mouse.GetState();
                if ((mousestate.X > 0) && (mousestate.X < resX) && (mousestate.Y > 0) && (mousestate.Y < resY))
                {
                    LBMElement l = tabLBM[currentTabLBMIndex][mousestate.X / elementSize, mousestate.Y / elementSize];
                    spriteBatch.DrawString(defaultFont, "x: " + l.x.ToString() + " y: " + l.y.ToString(), new Vector2(mousestate.X, mousestate.Y + 10), Color.White);
                    spriteBatch.DrawString(defaultFont, "d: " + l.density.ToString(), new Vector2(mousestate.X, mousestate.Y + 30), Color.White);
                    spriteBatch.DrawString(defaultFont, "x: " + mousestate.X.ToString() + " y: " + mousestate.Y.ToString(), new Vector2(mousestate.X, mousestate.Y + 50), Color.White);
                    DrawLine(texWall, new Vector2(mousestate.X, mousestate.Y), new Vector2(mousestate.X + tabLBM[currentTabLBMIndex][mousestate.X / elementSize, mousestate.Y / elementSize].x * 30f, mousestate.Y + tabLBM[currentTabLBMIndex][mousestate.X / elementSize, mousestate.Y / elementSize].y * 30f), new Color(0, 255, 0));
                }

                //Check if all configs have generated
                if (!doAutomaticLBM)
                {
                    if (AllConfigsGenerated())
                    {
                        spriteBatch.DrawString(defaultFont, "Ready to simulate traffic", new Vector2(1, linePos+=20), Color.HotPink);
                    }
                }
            }

            //sim
            if (gameState == GameState.SimulateTraffic)
            {
                if (keybstate.IsKeyDown(Keys.Q))
                    AddNewCar(rand.Next(routeConfigList.Count), gameTime);

                if (keybstate.IsKeyDown(Keys.A))
                    doAutomaticCars = !doAutomaticCars;

                //cars
                foreach (Car car in cars)
                {
                    car.Draw(spriteBatch);
                }

                //malowanie wszystkich swiatel
                //kolor zalezy czy dane id sciany ktorej malujemy jest w aktualnym konfigu - jak jest to na czerwono
                if (lightConfigList.Count > 0)
                {
                    for (int x = 0; x < lightList.Count; x++)
                    {
                        RouteWall rw = lightList[x];
                        DrawLine(texWall, new Vector2(rw.x1, rw.y1), new Vector2(rw.x2, rw.y2), (lightConfigList[lightConfigId].lightId.Contains(x) ? Color.Green : Color.Red));
                    }
                }

                //wybieranie aut
                MouseState mousestate = Mouse.GetState();
                Vector2 mouseVector = new Vector2((float)mousestate.X, (float)mousestate.Y);
                if (mousestate.RightButton == ButtonState.Pressed)
                    selectedCar = null;
                else
                    if ((mousestate.X > 0) && (mousestate.X < resX) && (mousestate.Y > 0) && (mousestate.Y < resY) && (mousestate.LeftButton == ButtonState.Pressed) && cars.Count > 0)
                    {
                        selectedCar = cars[0];
                        float minLength = (float)resX;
                        foreach (Car car in cars)
                        {
                            Vector2 minus = mouseVector - car.position;
                            if (minus.Length() < minLength)
                            {
                                minLength = minus.Length();
                                selectedCar = car;
                            }
                        }
                    }

                int lineDraw = 0;
                //simulation state
                spriteBatch.DrawString(defaultFont, "Auto: " + doAutomaticCars.ToString(), new Vector2(1, lineDraw += 20), Color.White);
                //light state
                if (lightConfigList.Count > 0)
                {
                    spriteBatch.DrawString(defaultFont, string.Format("Light id:{0} c:{1}", lightConfigId, lightConfigList[lightConfigId].comment), new Vector2(1, lineDraw += 20), Color.White);
                }
                //car counter
                spriteBatch.DrawString(defaultFont, string.Format("cars:{0} finished:{1}", currentCarCount, finishedCarCount), new Vector2(1, lineDraw += 20), Color.White);

                //selected car info
                if (selectedCar != null && cars.Contains(selectedCar))
                {
                    //car data
                    spriteBatch.DrawString(defaultFont, "aggressiveness: " + selectedCar.aggressiveness.ToString(), new Vector2(1, lineDraw += 20), Color.White);
                    spriteBatch.DrawString(defaultFont, "Pos: x: " + selectedCar.position.X.ToString() + " y: " + selectedCar.position.X.ToString(), new Vector2(1, lineDraw += 20), Color.White);
                    spriteBatch.DrawString(defaultFont, "userSteer: " + selectedCar.userSteer.ToString(), new Vector2(1, lineDraw += 20), Color.White);
                    spriteBatch.DrawString(defaultFont, "userAcc: " + selectedCar.userAcc, new Vector2(1, lineDraw += 20), Color.White);
                    spriteBatch.DrawString(defaultFont, "V: " + selectedCar.velocity.ToString(), new Vector2(1, lineDraw += 20), Color.White);
                    //steering
                    lineDraw += 20;
                    //background
                    spriteBatch.Draw(texWall, new Rectangle(1, lineDraw, 50, 50), Color.Black);
                    //axis
                    spriteBatch.Draw(texWall, new Rectangle(1, lineDraw + 25, 50, 1), Color.White);
                    spriteBatch.Draw(texWall, new Rectangle(25, lineDraw, 1, 50), Color.White);
                    //joystick
                    spriteBatch.Draw(texWall, new Rectangle((int)(20 * selectedCar.userSteer) + 20, (int)(20 * -selectedCar.userAcc) + lineDraw + 20, 10, 10), Color.HotPink);

                    //car selection
                    DrawLine(texWall, selectedCar.framePointFL, selectedCar.framePointFR, Color.HotPink);
                    DrawLine(texWall, selectedCar.framePointFR, selectedCar.framePointRR, Color.HotPink);
                    DrawLine(texWall, selectedCar.framePointRR, selectedCar.framePointRL, Color.HotPink);
                    DrawLine(texWall, selectedCar.framePointRL, selectedCar.framePointFL, Color.HotPink);

                    //line from cursor to car
                    DrawLine(texWall, selectedCar.position, mouseVector, Color.HotPink);
                }
            }

            //gameState
            var drawVector = new Vector2(1f, 1f);
            for (int i = 0; i <= 2; i++)
            {
                spriteBatch.DrawString(defaultFont, i.ToString(), drawVector, (i == (int)gameState ? Color.White : Color.Black));
                drawVector.X += 10;
            }
            spriteBatch.DrawString(defaultFont, Enum.GetName(typeof(GameState), gameState), drawVector, Color.White);

            base.Draw(gameTime);
            spriteBatch.End();
        }

        private bool AllConfigsGenerated()
        {
            bool allConfigsGenerated = true;
            for (int lbmIndex = 0; lbmIndex < routeConfigList.Count; lbmIndex++)
            {
                allConfigsGenerated &= _lbmController.HasRouteVectorMapGenerated(tabLBM[lbmIndex], routeConfigList[lbmIndex]);
            }
            return allConfigsGenerated;
        }

        public void DrawLine(Texture2D spr, Vector2 a, Vector2 b, Color col)
        {
            Vector2 Origin = new Vector2(0.5f, 0.0f);
            Vector2 diff = b - a;
            float angle;
            Vector2 Scale = new Vector2(1.0f, diff.Length() / spr.Height);

            angle = (float)(Math.Atan2(diff.Y, diff.X)) - MathHelper.PiOver2;

            spriteBatch.Draw(spr, a, null, col, angle, Origin, Scale, SpriteEffects.None, 1.0f);
        }

        int _carIndex = 0;
        /// <summary>
        /// dodaje nowe auto na podstawie danych z route config o odpowiednim indexie
        /// </summary>
        /// <param name="tabLBMIndex"></param>
        void AddNewCar(int tabLBMIndex, GameTime gameTime)
        {
            RouteStart rs = routeConfigList[tabLBMIndex].routeStart[rand.Next(routeConfigList[tabLBMIndex].routeStart.Count)];

            Car car;
            bool ok = true;
            //10 prob wstawienia auta tak by nie przecinalo sie z pozostalymi
            for (int i = 0; i < 10; i++)
            {
                ok = true;

                car = new Car(
                    this,
                    Content.Load<Texture2D>("cars\\car" + rand.Next(1, carSpritesCount + 1).ToString()),
                    new Vector2((float)rand.Next(Math.Min(rs.x1, rs.x2),Math.Max(rs.x1, rs.x2)), (float)rand.Next(Math.Min(rs.y1, rs.y2),Math.Max(rs.y1, rs.y2))),
                    MathHelper.ToRadians(rs.directionDeg),
                    tabLBMIndex,
                    rand.Next(0, 101) / 100f
                );
                car.Update(gameTime);//po to by odswiezyl sobie punkty ramki
                car.velocity = routeConfigList[tabLBMIndex].initialSpeed;
                car.maxSpeed = routeConfigList[tabLBMIndex].maxSpeed;

                //sprawdzenie czy nie nachodzi na inne auta
                foreach (Car car2 in cars)
                {
                    if (car.IntersectsOtherCarStart(car2))
                        ok = false;
                }

                //sprawdzenie czy ktoryms rogiem nie jest na chodniku
                Color c = GetColorFromLogicMapAtPoint(GeneralHelper.NormalizeVector(car.framePointFL));
                if ((c.A > 254) && (c.G > 128))
                    ok = false;

                c = GetColorFromLogicMapAtPoint(GeneralHelper.NormalizeVector(car.framePointFR));
                if ((c.A > 254) && (c.G > 128))
                    ok = false;

                c = GetColorFromLogicMapAtPoint(GeneralHelper.NormalizeVector(car.framePointRR));
                if ((c.A > 254) && (c.G > 128))
                    ok = false;

                c = GetColorFromLogicMapAtPoint(GeneralHelper.NormalizeVector(car.framePointRL));
                if ((c.A > 254) && (c.G > 128))
                    ok = false;

                //jak wszystko ok to mamy miejsce
                if (ok)
                {
                    car.Id = _carIndex++;
                    cars.Add(car);
                    break;
                }
            }
        }

        /// <summary>
        /// pobiera kolor z mapy logicznej w danym punkcie
        /// </summary>
        public Color GetColorFromLogicMapAtPoint(int x, int y)
        {
            if (x >= resX) x = resX - 1; else if (x < 0) x = 0;
            if (y >= resY) y = resY - 1; else if (y < 0) y = 0;
            return _colorMap[y * resX + x];
        }

        /// <summary>
        /// pobiera kolor z mapy logicznej w danym punkcie
        /// </summary>
        public Color GetColorFromLogicMapAtPoint(Vector2 vec)
        {
            return GetColorFromLogicMapAtPoint((int)vec.X, (int)vec.Y);
        }

        /// <summary>
        /// sprawdza czy podany kolor odpowiada chodnikowi (stan logiczny)
        /// </summary>
        public bool IsWalkway(Color c)
        {
            if ((c.A > 254) && (c.G > 128))
                return true;
            return false;
        }
    }
}
