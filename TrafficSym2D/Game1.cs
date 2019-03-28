using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using System.Xml;
using System.IO;

namespace TrafficSym2D
{
    /*
     * Komentarze:
     * Czas w sekundach
     * Odleglosc w m
     * 
     * Predkosc liczona w m/s
     * Przyspieszenie 1g = 10m/s^2
     */

    /// <summary>
    /// Tomasz Su³kowski
    /// Created: 19.04.2009
    /// Symulator ruchu ulicznego 2D
    /// </summary>
    public class Game1 : Game
    {
        const int resX = 1024;
        const int resY = 768;
        public int resNotStaticX = resX;
        public int resNotStaticY = resY;

        private int mapsLoadedState = 0;

        public static int elementSize = 5;
        public int elementSize2 = elementSize;
        public static float vectorLength = 5f; //sugerowana wartosc to polowa powyzszego

        public static int countX = (resX / elementSize) + 1;
        public static int countY = (resY / elementSize) + 1;
        public int countNotStaticX = countX;//potrzebne dla klas z referencjami do tej;
        public int countNotStaticY = countY;//potrzebne dla klas z referencjami do tej;

        public static float FLOW_MAX = 0.05f;
        public static float COEF = 0.24f;
        public static float SQRT2 = (float)Math.Sqrt(2.0);

        public Random rand = new Random();
        /// <summary>
        /// stan symulacji, 0-pauza? 1-symulacja lattice 2-symulacja auta
        /// </summary>
        public int gameState = 0;

        public bool doAi = true;
        public bool doLBM = false;
        public bool doAutomaticCars = true;

        //grafika
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        //font
        public SpriteFont defaultFont;

        //mapa
        Texture2D mapBackgroundTexture;
        public Texture2D mapLogicTexture;
        Rectangle viewportRect;

        //xml z danymi poczatkowymi itd
        XmlDocument xdoc;

        //obiekt przechowujacy dane do sciezek
        List<RouteConfig> routeConfigList = new List<RouteConfig>();

        //lista przechowujaca dane do swiatel
        List<RouteWall> lightList = new List<RouteWall>();
        List<LightConfig> lightConfigList = new List<LightConfig>();

        //auta
        public static int carSpritesCount = 4;
        public List<Car> cars = new List<Car>();
        public Car selectedCar;

        //implementacja LBM
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

        //do wyswietlania grafiki
        public Texture2D texBrake;
        public Texture2D texAcc;
        public Texture2D texSteer;

        public Texture2D texWall;
        public Texture2D texVector;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            //29fps
            //TargetElapsedTime = new TimeSpan(0, 0, 0, 0, 34);
        }

        protected override void Initialize()
        {
            LGAHelper.parent = this;
            GeneralHelper.parent = this;
            TabLBMSerializer.parent = this;
            graphics.PreferredBackBufferWidth = resX;
            graphics.PreferredBackBufferHeight = resY;
            graphics.IsFullScreen = false;
            graphics.ApplyChanges();
            base.Initialize();
        }

        protected void LoadDataFromXML()
        {
            //ladowanie xml'a z danymi
            xdoc = new XmlDocument();
            try
            {
                if (File.Exists("..\\..\\..\\settings.xml"))
                    xdoc.Load("..\\..\\..\\settings.xml");
            }
            catch { }
            try
            {
                if (File.Exists("settings.xml"))
                    xdoc.Load("settings.xml");
            }
            catch { }

            //XmlElement xmlConfig = xdoc["TrafficSym2D"]["routeConfigs"]["config"];
            //x1 = Convert.ToInt32(xmlConfig["start"].Attributes["x1"].Value);

            #region ladowanie routeConfigs
            if (xdoc["TrafficSym2D"] == null)
            {
                System.Windows.Forms.MessageBox.Show("settings.xml nie zaladowany!");
                xdoc.LoadXml(GeneralHelper.settingsxml);
            }
            XmlElement xmlConfig = xdoc["TrafficSym2D"]["routeConfigs"];
            for (int i = 0; i < xmlConfig.ChildNodes.Count; i++)
            {
                XmlNode xmlNodeMain = xmlConfig.ChildNodes.Item(i);
                RouteConfig routeConfig = new RouteConfig();
                routeConfig.timeBetweenCarsMs = Convert.ToInt32(xmlNodeMain.Attributes["timeBetweenCarsMs"].Value);

                for (int i2 = 0; i2 < xmlNodeMain.ChildNodes.Count; i2++)
                {
                    XmlNode xmlNode = xmlNodeMain.ChildNodes.Item(i2);

                    int x1 = Convert.ToInt32(xmlNode.Attributes["x1"].Value);
                    int y1 = Convert.ToInt32(xmlNode.Attributes["y1"].Value);
                    int x2 = Convert.ToInt32(xmlNode.Attributes["x2"].Value);
                    int y2 = Convert.ToInt32(xmlNode.Attributes["y2"].Value);

                    if (xmlNode.Name.Equals("start") || xmlNode.Name.Equals("end"))
                    {
                        int temp;
                        if (x1 > x2)
                        {
                            temp = x1;
                            x1 = x2;
                            x2 = temp;
                        }
                        if (y1 > y2)
                        {
                            temp = y1;
                            y1 = y2;
                            y2 = temp;
                        }
                    }

                    if (x1 > (resX - elementSize)) x1 = (resX - elementSize);
                    if (x2 > (resX - elementSize)) x2 = (resX - elementSize);
                    if (y1 > (resY - elementSize)) y1 = (resY - elementSize);
                    if (y2 > (resY - elementSize)) y2 = (resY - elementSize);
                    if (x1 < elementSize) x1 = elementSize;
                    if (x2 < elementSize) x2 = elementSize;
                    if (y1 < elementSize) y1 = elementSize;
                    if (y2 < elementSize) y2 = elementSize;

                    switch (xmlNode.Name)
                    {
                        case "start":
                            {
                                routeConfig.routeStart.Add(new RouteStart(
                                    x1, y1, x2, y2,
                                    (float)Convert.ToInt32(xmlNode.Attributes["direction"].Value)
                                    ));
                            }; break;

                        case "end":
                            {
                                routeConfig.routeEnd.Add(new RouteEnd(
                                    x1, y1, x2, y2
                                    ));
                            }; break;

                        case "wall":
                            {
                                routeConfig.routeWall.Add(new RouteWall(
                                    x1, y1, x2, y2
                                    ));
                            }; break;
                    }
                }
                routeConfigList.Add(routeConfig);
            }
            #endregion

            if (xdoc["TrafficSym2D"]["lightConfig"] != null)
            {
                #region ladowanie lightConfig.lights
                xmlConfig = xdoc["TrafficSym2D"]["lightConfig"]["lights"];
                if (xmlConfig != null)
                    for (int i = 0; i < xmlConfig.ChildNodes.Count; i++)
                    {
                        XmlNode xmlNode = xmlConfig.ChildNodes.Item(i);
                        int x1 = Convert.ToInt32(xmlNode.Attributes["x1"].Value);
                        int y1 = Convert.ToInt32(xmlNode.Attributes["y1"].Value);
                        int x2 = Convert.ToInt32(xmlNode.Attributes["x2"].Value);
                        int y2 = Convert.ToInt32(xmlNode.Attributes["y2"].Value);

                        if (x1 > (resX - elementSize)) x1 = (resX - elementSize);
                        if (x2 > (resX - elementSize)) x2 = (resX - elementSize);
                        if (y1 > (resY - elementSize)) y1 = (resY - elementSize);
                        if (y2 > (resY - elementSize)) y2 = (resY - elementSize);
                        if (x1 < elementSize) x1 = elementSize;
                        if (x2 < elementSize) x2 = elementSize;
                        if (y1 < elementSize) y1 = elementSize;
                        if (y2 < elementSize) y2 = elementSize;

                        lightList.Add(new RouteWall(x1, y1, x2, y2));
                    }
                #endregion

                #region ladowanie lightConfig.configs
                xmlConfig = xdoc["TrafficSym2D"]["lightConfig"]["configs"];
                if (xmlConfig != null)
                    for (int i = 0; i < xmlConfig.ChildNodes.Count; i++)
                    {
                        XmlNode xmlNodeMain = xmlConfig.ChildNodes.Item(i);
                        LightConfig lc = new LightConfig();
                        lc.timeToWaitMs = Convert.ToInt32(xmlNodeMain.Attributes["timeToWaitMs"].Value);

                        for (int i2 = 0; i2 < xmlNodeMain.ChildNodes.Count; i2++)
                        {
                            XmlNode xmlNode = xmlNodeMain.ChildNodes.Item(i2);
                            lc.lightId.Add(Convert.ToInt32(xmlNode.Attributes["id"].Value));
                        }
                        lightConfigList.Add(lc);
                    }
                #endregion
            }
        }

        protected override void LoadContent()
        {
            this.IsMouseVisible = true;
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            //
            viewportRect = new Rectangle(0, 0, graphics.GraphicsDevice.Viewport.Width, graphics.GraphicsDevice.Viewport.Height);

            //mapa
            if (File.Exists("map_b.bmp"))
            {
                FileStream fs = File.Open("map_b.bmp", FileMode.Open);
                mapBackgroundTexture = Texture2D.FromFile(GraphicsDevice, fs);
                fs.Close();
            }
            else
            {
                mapBackgroundTexture = Content.Load<Texture2D>("maps\\map_b");
            }

            if (File.Exists("map_l.tga"))
            {
                FileStream fs = File.Open("map_l.tga", FileMode.Open);
                mapLogicTexture = Texture2D.FromFile(GraphicsDevice, fs);
                fs.Close();
            }
            else
            {
                mapLogicTexture = Content.Load<Texture2D>("maps\\map_l");
            }

            //font
            defaultFont = Content.Load<SpriteFont>("DefaultFont");

            //dodatki do auta
            texAcc = Content.Load<Texture2D>("car_gadgets\\acc");
            texBrake = Content.Load<Texture2D>("car_gadgets\\brake");
            texSteer = Content.Load<Texture2D>("car_gadgets\\steer");

            //wyswietlanie gazu
            texWall = Content.Load<Texture2D>("gas\\wall");

            //ladowanie z xml
            LoadDataFromXML();

            //inicjacja tabLBM
            lightTabLBM = new LBMElement[countX, countY];
            tabLBM = new LBMElement[routeConfigList.Count][,];
            for (int i = 0; i < routeConfigList.Count; i++)
                tabLBM[i] = new LBMElement[countX, countY];

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
                            Rectangle sourceRectangle = new Rectangle(tx, ty, 1, 1);
                            Color[] retrievedColor = new Color[1];
                            mapLogicTexture.GetData<Color>(0, sourceRectangle, retrievedColor, 0, 1);
                            if ((retrievedColor[0].A > 254) && (retrievedColor[0].G > 128))
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
                    DrawLineLBM(tabLBM[i], rs.x1, rs.y1, rs.x2, rs.y2, 2);
                foreach (RouteEnd re in rc.routeEnd)
                    DrawLineLBM(tabLBM[i], re.x1, re.y1, re.x2, re.y2, 3);
                foreach (RouteWall rw in rc.routeWall)
                    DrawLineLBM(tabLBM[i], rw.x1, rw.y1, rw.x2, rw.y2, 1);
            }

            base.LoadContent();
        }

        protected override void UnloadContent()
        {
        }

        float[,] fx = new float[countX, countY];
        float[,] fy = new float[countX, countY];
        TimeSpan lightLastChangeTime = new TimeSpan(0);
        int lightConfigId = -1;

        MouseState prevMouseState;
        int posx, posy;

        protected override void Update(GameTime gameTime)
        {
            GameTime customGameTime = gameTime;
            customGameTime = new GameTime(gameTime.TotalRealTime, new TimeSpan(200000), gameTime.TotalGameTime, gameTime.ElapsedGameTime);

            // Allows the game to exit
            KeyboardState keybstate = Keyboard.GetState();
            if (keybstate.IsKeyDown(Keys.Escape))
                this.Exit();

            //stany symulacji
            foreach (Keys k in keybstate.GetPressedKeys())
                switch (k)
                {
                    case Keys.D0: gameState = 0; break;
                    case Keys.D1: gameState = 1; break;
                    case Keys.D2: gameState = 2; break;
                }

            if (gameState == 0)
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
            if ((gameState == 1) && doLBM)
            {
                Array.Clear(fx, 0, fx.Length);
                Array.Clear(fy, 0, fy.Length);
                //2. Pochodne cisnienia
                for (int y = 0; y < countY - 1; y++)
                    for (int x = 0; x < countX - 1; x++)
                        if (!tabLBM[currentTabLBMIndex][x, y].isWall)
                        {
                            if (!tabLBM[currentTabLBMIndex][x + 1, y].isWall)
                                fx[x, y] = MathHelper.Clamp(COEF * (tabLBM[currentTabLBMIndex][x, y].density - tabLBM[currentTabLBMIndex][x + 1, y].density), -FLOW_MAX, FLOW_MAX);
                            else
                                fx[x, y] = 0f;

                            if (!tabLBM[currentTabLBMIndex][x, y + 1].isWall)
                                fy[x, y] = MathHelper.Clamp(COEF * (tabLBM[currentTabLBMIndex][x, y].density - tabLBM[currentTabLBMIndex][x, y + 1].density), -FLOW_MAX, FLOW_MAX);
                            else
                                fy[x, y] = 0f;
                        }
                        else
                        {
                            fx[x, y] = 0f;
                            fy[x, y] = 0f;
                        }

                float dens;//temp
                //3. Przeplyw
                for (int y = 0; y < countY; y++)
                    for (int x = 0; x < countX; x++)
                        if (!tabLBM[currentTabLBMIndex][x, y].isWall)
                        {
                            dens = tabLBM[currentTabLBMIndex][x, y].density;

                            if (x > 0) dens += fx[x - 1, y];
                            if (y > 0) dens += fy[x, y - 1];

                            dens -= fx[x, y];
                            dens -= fy[x, y];

                            tabLBM[currentTabLBMIndex][x, y].density = MathHelper.Clamp(dens, -10f, 10f);
                        }

                float vx, vy;
                //4. do graficznego pokazania przeplywu - roznice cisnien
                for (int y = 1; y < countY - 1; y++)
                    for (int x = 1; x < countX - 1; x++)
                    {
                        if (!tabLBM[currentTabLBMIndex][x, y].isWall)
                        {
                            vx = 0f; vy = 0f;
                            if (!tabLBM[currentTabLBMIndex][x - 1, y].isWall) vx += (tabLBM[currentTabLBMIndex][x - 1, y].density - tabLBM[currentTabLBMIndex][x, y].density);
                            if (!tabLBM[currentTabLBMIndex][x + 1, y].isWall) vx += (tabLBM[currentTabLBMIndex][x, y].density - tabLBM[currentTabLBMIndex][x + 1, y].density);
                            tabLBM[currentTabLBMIndex][x, y].x = MathHelper.Clamp(vx * 100f, ((float)-elementSize), ((float)elementSize));
                            if (!tabLBM[currentTabLBMIndex][x, y - 1].isWall) vy += (tabLBM[currentTabLBMIndex][x, y - 1].density - tabLBM[currentTabLBMIndex][x, y].density);
                            if (!tabLBM[currentTabLBMIndex][x, y + 1].isWall) vy += (tabLBM[currentTabLBMIndex][x, y].density - tabLBM[currentTabLBMIndex][x, y + 1].density);
                            tabLBM[currentTabLBMIndex][x, y].y = MathHelper.Clamp(vy * 100f, ((float)-elementSize), ((float)elementSize));
                        }
                    }
            }

            if (gameState == 2)
            {
                //jak jeszcze nie zainicjowane to ustawiamy pierwszy config
                if (lightConfigList.Count > 0)
                {
                    if (lightConfigId == -1)
                    {
                        lightConfigId = 0;
                        lightLastChangeTime = gameTime.TotalRealTime;
                        for (int x = 0; x < lightList.Count; x++)
                            if (!lightConfigList[lightConfigId].lightId.Contains(x)) //jak zawiera oznacza ze ma byc to swiatlo zielone i nie malowac...
                            {
                                RouteWall rw = lightList[x];
                                DrawLineLBM(lightTabLBM, rw.x1, rw.y1, rw.x2, rw.y2, 1);
                            }
                    }

                    //dodawanie nowych aut automatycznie
                    if (doAutomaticCars)
                        for (int i = 0; i < routeConfigList.Count; i++)
                        {
                            RouteConfig rc = routeConfigList[i];
                            if (gameTime.TotalRealTime.TotalMilliseconds > (rc.lastCarOutTime.TotalMilliseconds + rc.timeBetweenCarsMs))
                            {
                                rc.lastCarOutTime = gameTime.TotalRealTime;
                                AddNewCar(i, gameTime);
                            }
                        }

                    //zmiana swiatel
                    if (gameTime.TotalRealTime.TotalMilliseconds > (lightLastChangeTime.TotalMilliseconds + lightConfigList[lightConfigId].timeToWaitMs))
                    {
                        //malowanie powrotne pustych kratek
                        for (int x = 0; x < lightList.Count; x++)
                        {
                            RouteWall rw = lightList[x];
                            DrawLineLBM(lightTabLBM, rw.x1, rw.y1, rw.x2, rw.y2, 0);
                        }
                        //malowanie nowych scian
                        lightConfigId++;
                        if (lightConfigId > (lightConfigList.Count - 1)) lightConfigId = 0;
                        lightLastChangeTime = gameTime.TotalRealTime;
                        for (int x = 0; x < lightList.Count; x++)
                            if (!lightConfigList[lightConfigId].lightId.Contains(x)) //jak zawiera oznacza ze ma byc to swiatlo zielone i nie malowac...
                            {
                                RouteWall rw = lightList[x];
                                DrawLineLBM(lightTabLBM, rw.x1, rw.y1, rw.x2, rw.y2, 1);
                            }
                    }
                }

                List<Car> carsToRemove = new List<Car>();

                //cars
                foreach (Car car in cars)
                {
                    //sterowanie ai
                    if (doAi)
                    {
                        car.DoAI(tabLBM[car.tabLBMIndex]);
                    }
                    if (car.Equals(selectedCar)) car.DoManualSteer(keybstate);

                    car.Update(customGameTime);
                    //wywalanie z listy jak dojechal do konca
                    if (((car.position.X / elementSize) >= countX - 1) || ((car.position.Y / elementSize) >= countY - 1) || ((car.position.X / elementSize) <= 1) || ((car.position.Y / elementSize) <= 1))
                        carsToRemove.Add(car);
                    else if (tabLBM[car.tabLBMIndex][(int)car.position.X / elementSize, (int)car.position.Y / elementSize].isHole)
                        carsToRemove.Add(car);
                }

                foreach (Car car in carsToRemove)
                    cars.Remove(car);
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(SpriteBlendMode.AlphaBlend);

            KeyboardState keybstate = Keyboard.GetState();

            //jak mapy nie za³adowane to za³aduj...
            if (mapsLoadedState < 2)
            {
                mapsLoadedState++;

                spriteBatch.DrawString(defaultFont, "Loading maps...", new Vector2(10, (float)(resNotStaticY / 2.0)), Color.White);

                base.Draw(gameTime);
                spriteBatch.End();

                if (mapsLoadedState > 1)
                {
                    for (int i = 0; i < routeConfigList.Count; i++)
                    {
                        LBMElement[,] temp = TabLBMSerializer.LoadTabLBM(i);
                        if (temp != null) tabLBM[i] = temp;
                    }
                }

                if (tabLBM.Count() > 0) gameState = 2;

                return;
            }

            //background
            spriteBatch.Draw(mapBackgroundTexture, viewportRect, Color.White);
            //logic map?
            spriteBatch.Draw(mapLogicTexture, viewportRect, Color.White);

            //LGA
            if (gameState == 1)
            {
                if (keybstate.IsKeyDown(Keys.Up))
                    currentTabLBMIndex++;
                else if (keybstate.IsKeyDown(Keys.Down))
                    currentTabLBMIndex--;

                if (keybstate.IsKeyDown(Keys.A))
                    doLBM = !doLBM;

                //zapis / odczyt
                if (keybstate.IsKeyDown(Keys.S))
                    TabLBMSerializer.SaveTabLBM(tabLBM[currentTabLBMIndex], currentTabLBMIndex);
                else
                    if (keybstate.IsKeyDown(Keys.L))
                        tabLBM[currentTabLBMIndex] = TabLBMSerializer.LoadTabLBM(currentTabLBMIndex);
                    else
                        if (keybstate.IsKeyDown(Keys.K))
                        {
                            for (int i = 0; i < routeConfigList.Count; i++)
                            {
                                LBMElement[,] temp = TabLBMSerializer.LoadTabLBM(i);
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
                                    else if (l.isHole)
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
                spriteBatch.DrawString(defaultFont, "tabLBM index: " + currentTabLBMIndex.ToString(), new Vector2(1, 20), Color.White);
                spriteBatch.DrawString(defaultFont, "doLBM: " + doLBM.ToString(), new Vector2(1, 40), Color.White);

                MouseState mousestate = Mouse.GetState();
                if ((mousestate.X > 0) && (mousestate.X < resX) && (mousestate.Y > 0) && (mousestate.Y < resY))
                {
                    LBMElement l = tabLBM[currentTabLBMIndex][mousestate.X / elementSize, mousestate.Y / elementSize];
                    spriteBatch.DrawString(defaultFont, "x: " + l.x.ToString() + " y: " + l.y.ToString(), new Vector2(mousestate.X, mousestate.Y + 10), Color.White);
                    spriteBatch.DrawString(defaultFont, "d: " + l.density.ToString(), new Vector2(mousestate.X, mousestate.Y + 30), Color.White);
                    spriteBatch.DrawString(defaultFont, "x: " + mousestate.X.ToString() + " y: " + mousestate.Y.ToString(), new Vector2(mousestate.X, mousestate.Y + 50), Color.White);
                    DrawLine(texWall, new Vector2(mousestate.X, mousestate.Y), new Vector2(mousestate.X + tabLBM[currentTabLBMIndex][mousestate.X / elementSize, mousestate.Y / elementSize].x * 30f, mousestate.Y + tabLBM[currentTabLBMIndex][mousestate.X / elementSize, mousestate.Y / elementSize].y * 30f), new Color(0, 255, 0));
                }
            }

            //sim
            if (gameState == 2)
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

                //info o wybranym aucie
                if (selectedCar != null && cars.Contains(selectedCar))
                {
                    //wypisanie danych auta
                    spriteBatch.DrawString(defaultFont, "aggressiveness: " + selectedCar.aggressiveness.ToString(), new Vector2(1, 40), Color.White);
                    spriteBatch.DrawString(defaultFont, "Pos: x: " + selectedCar.position.X.ToString() + " y: " + selectedCar.position.X.ToString(), new Vector2(1, 60), Color.White);
                    spriteBatch.DrawString(defaultFont, "userSteer: " + selectedCar.userSteer.ToString(), new Vector2(1, 80), Color.White);
                    spriteBatch.DrawString(defaultFont, "userAcc: " + selectedCar.userAcc, new Vector2(1, 100), Color.White);
                    spriteBatch.DrawString(defaultFont, "V: " + selectedCar.velocity.ToString(), new Vector2(1, 120), Color.White);
                    //wyswietlanie sterowania
                    //tlo
                    spriteBatch.Draw(texWall, new Rectangle(1, 140, 50, 50), Color.Black);
                    //axis
                    spriteBatch.Draw(texWall, new Rectangle(1, 165, 50, 1), Color.White);
                    spriteBatch.Draw(texWall, new Rectangle(25, 140, 1, 50), Color.White);
                    //joystick
                    spriteBatch.Draw(texWall, new Rectangle((int)(20 * selectedCar.userSteer) + 20, (int)(20 * -selectedCar.userAcc) + 140 + 20, 10, 10), Color.HotPink);

                    //zaznaczenie auta?
                    DrawLine(texWall, selectedCar.framePointFL, selectedCar.framePointFR, Color.HotPink);
                    DrawLine(texWall, selectedCar.framePointFR, selectedCar.framePointRR, Color.HotPink);
                    DrawLine(texWall, selectedCar.framePointRR, selectedCar.framePointRL, Color.HotPink);
                    DrawLine(texWall, selectedCar.framePointRL, selectedCar.framePointFL, Color.HotPink);

                    //linia od kursora do auta
                    DrawLine(texWall, selectedCar.position, mouseVector, Color.HotPink);
                }
                spriteBatch.DrawString(defaultFont, "Auto: " + doAutomaticCars.ToString(), new Vector2(1, 20), Color.White);
                //id swiatel
                spriteBatch.DrawString(defaultFont, "lightConfigId: " + lightConfigId.ToString(), new Vector2(80, 20), Color.White);
            }

            //gameState
            for (int i = 0; i <= 2; i++)
                spriteBatch.DrawString(defaultFont, i.ToString(), new Vector2(10f * i + 1, 1f), (i == gameState ? Color.White : Color.Black));

            base.Draw(gameTime);
            spriteBatch.End();
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

        /// <summary>
        /// Umieszcza w tabeli lbm linie danego typu
        /// </summary>
        /// <param name="NodeType">1 - sciana, 2 - source, 3 - hole</param>
        void DrawLineLBM(LBMElement[,] tabLBM, int x1, int y1, int x2, int y2, byte nodeType)
        {
            x1 /= elementSize;
            y1 /= elementSize;
            x2 /= elementSize;
            y2 /= elementSize;

            if (x1 == x2)
            {
                if (y1 > y2)
                {
                    int temp = x2;
                    x2 = x1;
                    x1 = temp;
                    temp = y2;
                    y2 = y1;
                    y1 = temp;
                }

                for (int i = y1; i <= y2; i++)
                {
                    //if ((!tabLBM[x1, i].isWall) || (nodeType == 0))
                    tabLBM[x1, i].nodeType = nodeType;
                }
            }
            else if (y1 == y2)
            {
                if (x1 > x2)
                {
                    int temp = x2;
                    x2 = x1;
                    x1 = temp;
                    temp = y2;
                    y2 = y1;
                    y1 = temp;
                }

                for (int i = x1; i <= x2; i++)
                {
                    //if ((!tabLBM[x1, i].isWall) || (nodeType == 0))
                    tabLBM[i, y1].nodeType = nodeType;
                }
            }
            else if (Math.Abs(x2 - x1) > Math.Abs(y2 - y1))
            {
                if (x1 > x2)
                {
                    int temp = x2;
                    x2 = x1;
                    x1 = temp;
                    temp = y2;
                    y2 = y1;
                    y1 = temp;
                }

                for (int i = x1; i <= x2; i++)
                    //if ((!tabLBM[i, (((i - x1) * (y2 - y1)) / (x2 - x1)) + y1].isWall) || (nodeType == 0))
                    tabLBM[i, (((i - x1) * (y2 - y1)) / (x2 - x1)) + y1].nodeType = nodeType;
            }
            else
            {
                if (y1 > y2)
                {
                    int temp = x2;
                    x2 = x1;
                    x1 = temp;
                    temp = y2;
                    y2 = y1;
                    y1 = temp;
                }

                for (int i = y1; i <= y2; i++)
                    //if ((!tabLBM[(((i - y1) * (x2 - x1)) / (y2 - y1)) + x1, i].isWall) || (nodeType == 0))
                    tabLBM[(((i - y1) * (x2 - x1)) / (y2 - y1)) + x1, i].nodeType = nodeType;
            }
        }

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
                    new Vector2((float)rand.Next(rs.x1, rs.x2), (float)rand.Next(rs.y1, rs.y2)),
                    MathHelper.ToRadians(rs.directionDeg),
                    tabLBMIndex,
                    rand.Next(0, 101) / 100f
                );
                car.Update(gameTime);//po to by odswiezyl sobie punkty ramki

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
            Rectangle sourceRectangle = new Rectangle(x, y, 1, 1);
            Color[] retrievedColor = new Color[1];
            mapLogicTexture.GetData<Color>(0, sourceRectangle, retrievedColor, 0, 1);
            return retrievedColor[0];
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
