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
using System.Threading.Tasks;
using TrafficSym2D.LBM;
using TrafficSym2D.Enums;
//using Microsoft.Xna.Framework.Net;
//using Microsoft.Xna.Framework.Storage;

namespace TrafficSym2D
{
    public class Car
    {
        TrafficSymGame parent;

        public int Id { get; set; }

        //index tabLBM przydzielony do niego
        public int tabLBMIndex = 0;

        //agresywnosc auta
        private float _aggressiveness = 0f;
        public float aggressiveness
        {
            get
            {
                if (_aggressiveness < 0f) _aggressiveness = 0f;
                if (_aggressiveness > 1f) _aggressiveness = 1f;
                return _aggressiveness;
            }
            set
            {
                _aggressiveness = value;
            }
        }

        //punkty okreslajace ramki auta
        public Vector2 framePointFL = new Vector2();
        public Vector2 framePointFR = new Vector2();
        public Vector2 framePointRL = new Vector2();
        public Vector2 framePointRR = new Vector2();
        public Vector2 framePointF = new Vector2();
        public Vector2 framePointFF = new Vector2();

        public float desiredAngle;//przechowuje kat wynikajacy z tabLBM pod autem

        //malowanie
        private static float rotationModel = (float)(Math.PI / 2f);//stala obracajaca obrazek auta by sie poruszalo w ta sama strone co kierunek ruchu
        private static float pixelToMeterRatio = 5.3f;//stosunek metr/pixel 3m==16px
        private static float scale = 0.3f;// 64pix*0.21==13px~=2,5m przecietna dlugosc auta

        public Texture2D sprite;
        int spriteHeight;
        int spriteWidth;

        public Vector2 position;
        private Vector2 prevPosition;
        public float rotation;
        public Vector2 center;

        //fizyka
        private static float max_steer = (float)(Math.PI / 5.14f);//maksymalny skret kol przednich(teraz 35 stopni)
        private float force_acc = 5f;// 5 m/s^2 == 0.5G
        private float force_braking = 8f;// 8 m/s^2 == 0.8G
        private float friction = 0.2f;// 0.2 m/s^2 == 0.02G
        private static float some_steer_param = 1f;//parametr uzalezniajacy obrot auta w zaleznosci od kata skreconych kol

        public float steer;
        public float velocity;
        public float acc;
        public float maxSpeed;

        //sterowanie
        private float _userAcc;
        public float userAcc
        {
            set
            {
                _userAcc = value;
                if (_userAcc > 1.0f) _userAcc = 1.0f;
                if (_userAcc < -1.0f) _userAcc = -1.0f;
            }
            get
            { return _userAcc; }
        }

        private float _userSteer;
        public float userSteer
        {
            set
            {
                _userSteer = value;
                if (_userSteer > 1.0f) _userSteer = 1.0f;
                if (_userSteer < -1.0f) _userSteer = -1.0f;
            }
            get
            { return _userSteer; }
        }

        //dodatki do wyswietlania
        private bool braking = false;
        private bool accelerating = false;

        //konstruktor
        public Car(TrafficSymGame _parent, Texture2D _sprite, Vector2 _position, float rotation, int tabLBMIndex, float aggressiveness)
        {
            parent = _parent;
            sprite = _sprite;
            spriteHeight = _sprite.Height;
            spriteWidth = _sprite.Width;
            position = _position;
            this.rotation = rotation;
            center = new Vector2(spriteWidth / 2.0f, spriteHeight * 0.75f);
            this.tabLBMIndex = tabLBMIndex;
            steer = 0f;
            acc = 0f;
            steer = 0f;
            velocity = 0f;
            _aggressiveness = aggressiveness;
            //ustalanie fizyki na bazie wielkosci sprite'a
            if (spriteHeight > 70)
            {
                force_acc *= (70f / (float)spriteHeight);
                friction *= ((float)spriteHeight / 70f);
            }
        }

        public void DoAI(LBMElement[,] tabLBM)
        {
            userAcc = 0;
            userSteer = 0;
            bool intersectsSmth = false;

            Vector2 leftSeeker = new Vector2(((float)Math.Cos(rotation - MathHelper.PiOver2)), ((float)Math.Sin(rotation - MathHelper.PiOver2)));
            Vector2 frontSeeker = new Vector2(((float)Math.Cos(rotation)), ((float)Math.Sin(rotation)));

            desiredAngle = rotation;//przechowuje sugerowany kierunek ruchu wynikajacy z tabLBM

            #region following LBM vector map
            int tx = (int)(framePointF.X / (float)parent.elementSize);
            int ty = (int)(framePointF.Y / (float)parent.elementSize);
            //on the border use center car position
            if (!(tx < (parent.countX - 2) && ty < (parent.countY - 2) && tx>1 && ty>1))
            {
                tx = (int)(position.X / (float)parent.elementSize);
                ty = (int)(position.Y / (float)parent.elementSize);
            }
            
            float vx, vy;//przechowuja kierunek

            //jako sugestie kierunku bierzemy pole na ktorym auto jest z waga *4 + cztery okoliczne pola
            if (tx < (parent.countX - 1) && ty < (parent.countY - 1) && tx>0 && ty>0)
            {
                vx = tabLBM[tx, ty].x * 4f; vy = tabLBM[tx, ty].y * 4f;
                vx += tabLBM[tx + 1, ty].x; vy += tabLBM[tx + 1, ty].y;
                vx += tabLBM[tx - 1, ty].x; vy += tabLBM[tx - 1, ty].y;
                vx += tabLBM[tx, ty + 1].x; vy += tabLBM[tx, ty + 1].y;
                vx += tabLBM[tx, ty - 1].x; vy += tabLBM[tx, ty - 1].y;
            }
            else
            {
                vx = tabLBM[tx, ty].x; vy = tabLBM[tx, ty].y;
            }

            if ((Math.Abs(vx) > 0.0001f) || (Math.Abs(vy) > 0.0001f))
            {
                //zamiana wektora z gazu na kątowy kierunek
                float vectorAngle = (float)Math.Atan2(vy, vx);
                desiredAngle = vectorAngle;
                //obliczenie roznicy kierunku jazdy oraz kata z gazu
                float angleDiff = vectorAngle - rotation;

                angleDiff = GeneralHelper.NormalizeAngleSteering(angleDiff);
                ////odpowiednie skrecenie kol w aucie
                if (angleDiff < 0)
                {
                    //skrecamy w prawo
                    if (angleDiff < -(float)(Math.PI / 4.0))
                        userSteer = -1;
                    else
                        userSteer = (float)(angleDiff * 4.0 / Math.PI);
                }
                else
                {
                    if (angleDiff > (float)(Math.PI / 4.0))
                        userSteer = 1;
                    else
                        userSteer = (float)(angleDiff * 4.0 / Math.PI);
                }
                float desiredSpeed = 1 + (1f - Math.Abs(userSteer)) * maxSpeed;
                userAcc = (desiredSpeed - velocity) / 2f;
                userAcc = MathHelper.Clamp(userAcc, -(0.7f + 0.3f * aggressiveness), 0.3f + 0.7f * aggressiveness);
            }
            else if (tabLBM[tx, ty].isSource)
            {
                userAcc = 1;
            }
            //drive to closest road
            else if (tabLBM[tx, ty].isWall)
            {
                intersectsSmth = true;

                var target = FindClosestNormalCell(tabLBM, position, parent.elementSize);
                var angle = GeneralHelper.NormalizeAngleSteering((float)Math.Atan2(target.Y - position.Y, target.X - position.X)-rotation);

                userSteer = MathHelper.Clamp(angle, -(float)(Math.PI / 4.0), (float)(Math.PI / 4.0));
                userAcc = MathHelper.Clamp(1f - velocity, 0, 1);
                return;
            }
            #endregion

            ///////////////////////// Sekcja swiadomosci pasow oraz chodnikow /////////////////////////////////////////
            //czyli nie wjezdzanie na chodniki + nie przekraczanie pasow jesli nie ma potrzeby (odpowiednio mocny user steer) z poprzedniej sekcji

            //kontrola gazem jak jedziemy z duza predkoscia i sie do czegos zblizamy
            //zalezne od predkosc - 30f to ok 100kmh, hamowanie z 0.8G
            //jak cos znajdzie sie miedzy wyliczonym punktem a autem to trzeba na maksa hamowac
            // d = v^2 / a

            //punktowy vektor rownolegly z przednim zderzakiem

            #region acceleration control: walkway, traffic lights, other cars + giving way to the right (or left)
            float dist = velocity * velocity / force_braking;
            if (velocity < 0)
                if (dist > 1f) dist = 1f;
            dist /= (1f + aggressiveness / 2f);

            #region inni uczestnicy ruchu
            {
                float dist2 = (dist * pixelToMeterRatio) + parent.elementSize;

                Vector2 posBumperLeftCar = new Vector2();//do wykrywania innych aut
                Vector2 posBumperRightCar = new Vector2();
                Vector2 posBumperFront = new Vector2();

                if (velocity > 0.05f)
                {
                    posBumperLeftCar.X = framePointFL.X + ((float)Math.Cos(rotation) * dist2) + (leftSeeker.X * scale * spriteWidth * 0.1f);
                    posBumperLeftCar.Y = framePointFL.Y + ((float)Math.Sin(rotation) * dist2) + (leftSeeker.Y * scale * spriteWidth * 0.1f);
                    posBumperRightCar.X = framePointFR.X + ((float)Math.Cos(rotation) * dist2) - (leftSeeker.X * scale * spriteWidth * 0.2f);
                    posBumperRightCar.Y = framePointFR.Y + ((float)Math.Sin(rotation) * dist2) - (leftSeeker.Y * scale * spriteWidth * 0.2f);
                }
                else
                {
                    posBumperLeftCar.X = framePointFL.X + ((float)Math.Cos(rotation) * dist2) + (leftSeeker.X * scale * spriteWidth * 0.05f);
                    posBumperLeftCar.Y = framePointFL.Y + ((float)Math.Sin(rotation) * dist2) + (leftSeeker.Y * scale * spriteWidth * 0.05f);
                    posBumperRightCar.X = framePointFR.X + ((float)Math.Cos(rotation) * dist2) - (leftSeeker.X * scale * spriteWidth * 0.1f);
                    posBumperRightCar.Y = framePointFR.Y + ((float)Math.Sin(rotation) * dist2) - (leftSeeker.Y * scale * spriteWidth * 0.1f);
                }

                posBumperFront.X = framePointF.X + ((float)Math.Cos(rotation) * dist2);
                posBumperFront.Y = framePointF.Y + ((float)Math.Sin(rotation) * dist2);

                Parallel.ForEach(parent.cars, otherCar =>
                {
                    if (userAcc == -1 || otherCar.Equals(this)) return;

                    //sprawdzanie czy punkt sprawdzajacy jest wewnatrz ramek auta
                    //wystarczy sprawdzic czy przecina sie z ktoras z ramek auta

                    //sprawdzamy jak nasze i(czujka)+dlugosc auta jest wieksze od roznicy odleglosci aut
                    if ((otherCar.position - this.position).Length() <= (dist + 2 * spriteHeight))
                    {
                        if ((OrientationHelper.Intersection(posBumperLeftCar, position, otherCar.framePointFL, otherCar.framePointFR))
                            || (OrientationHelper.Intersection(posBumperLeftCar, position, otherCar.framePointFR, otherCar.framePointRR))
                            || (OrientationHelper.Intersection(posBumperLeftCar, position, otherCar.framePointRR, otherCar.framePointRL))
                            || (OrientationHelper.Intersection(posBumperLeftCar, position, otherCar.framePointRL, otherCar.framePointFL))
                            || (OrientationHelper.Intersection(posBumperRightCar, position, otherCar.framePointFL, otherCar.framePointFR))
                            || (OrientationHelper.Intersection(posBumperRightCar, position, otherCar.framePointFR, otherCar.framePointRR))
                            || (OrientationHelper.Intersection(posBumperRightCar, position, otherCar.framePointRR, otherCar.framePointRL))
                            || (OrientationHelper.Intersection(posBumperRightCar, position, otherCar.framePointRL, otherCar.framePointFL))
                            || (OrientationHelper.Intersection(posBumperFront, position, otherCar.framePointRR, otherCar.framePointRL)))
                        {
                            intersectsSmth = true;
                            userAcc = (velocity == 0 ? -1 : -velocity);
                            if (this.IntersectsOtherCar(otherCar))
                                SeparateCars(this, otherCar);
                        }

                        #region giving way to the right (or left)
                        else
                        {
                            float revGiveWay = Config.GiveWayToLeft ? -1f : 1f;

                            float relativeAngleOtherCar = revGiveWay * ((float)Math.Atan2(otherCar.position.Y - posBumperFront.Y, otherCar.position.X - posBumperFront.X) - desiredAngle);
                            relativeAngleOtherCar = GeneralHelper.NormalizeAngle(relativeAngleOtherCar);

                            //if it is on the right (left) side of car...
                            if ((relativeAngleOtherCar > 0f) && (relativeAngleOtherCar < Math.PI))//MathHelper.PiOver2))
                            {
                                float angleRelative = revGiveWay * (desiredAngle - otherCar.rotation);
                                angleRelative = GeneralHelper.NormalizeAngle(angleRelative);
                                float perpendicularity = (float)Math.Sin(angleRelative);

                                float minLengthDiff = Config.GiveWayToLeft ?
                                    GeneralHelper.Min((framePointFL - otherCar.framePointFR).Length(), (framePointFL - otherCar.framePointRR).Length())
                                    : GeneralHelper.Min((framePointFR - otherCar.framePointFL).Length(), (framePointFR - otherCar.framePointRL).Length());

                                float otherCarVel = ((otherCar.velocity < 3f) && (otherCar.velocity > 0.1f)) ? 3f : otherCar.velocity;

                                float addParam = spriteWidth / 2f;
                                if ((otherCarVel * velocity) < 1f) addParam *= (otherCarVel * velocity);

                                //Calculating whether other car braking distance is bigger than perpendicular distance to current car
                                if (((otherCarVel * velocity * pixelToMeterRatio / (otherCar.force_braking * 0.7f) + addParam) * perpendicularity) > minLengthDiff)
                                {
                                    intersectsSmth = true;
                                    var newAcc = (velocity <= 0 ? 0 : -velocity);
                                    userAcc = newAcc < userAcc ? newAcc : userAcc;
                                }
                            }
                        }
                        #endregion
                    }
                });
            }
            #endregion

            //parametrem bedzie userAcc czy == -1
            for (int i = (int)(dist * pixelToMeterRatio) + parent.elementSize; i >= 0; i -= parent.elementSize)
            {
                if (userAcc == -1) break;
                Vector2 posBumperLeft = new Vector2();
                Vector2 posBumperRight = new Vector2();

                Vector2 posBumperFront = new Vector2();
                //lewy rog przedniego zderzaka: pozycja + dlugosc + pol szerokosci auta w lewo wzg osi + dlugosc czujki
                posBumperLeft.X = framePointFL.X + ((float)Math.Cos(rotation) * i);
                posBumperLeft.Y = framePointFL.Y + ((float)Math.Sin(rotation) * i);
                posBumperRight.X = framePointFR.X + ((float)Math.Cos(rotation) * i);
                posBumperRight.Y = framePointFR.Y + ((float)Math.Sin(rotation) * i);

                posBumperFront.X = framePointF.X + ((float)Math.Cos(rotation) * i);
                posBumperFront.Y = framePointF.Y + ((float)Math.Sin(rotation) * i);

                posBumperLeft = GeneralHelper.NormalizeVector(posBumperLeft);
                posBumperRight = GeneralHelper.NormalizeVector(posBumperRight);

                #region braking for walkway
                if (velocity > 5f)
                {
                    Color retrievedColor = parent.GetColorFromLogicMapAtPoint((int)posBumperLeft.X, (int)posBumperLeft.Y);

                    tx = (int)(posBumperLeft.X / (float)parent.elementSize);
                    ty = (int)(posBumperLeft.Y / (float)parent.elementSize);

                    if (((retrievedColor.A > 254) && (retrievedColor.G > 128)) || (tabLBM[tx, ty].isWall))
                    {
                        intersectsSmth = true;
                        userAcc = -1;
                    }
                    retrievedColor = parent.GetColorFromLogicMapAtPoint((int)posBumperRight.X, (int)posBumperRight.Y);

                    tx = (int)(posBumperRight.X / (float)parent.elementSize);
                    ty = (int)(posBumperRight.Y / (float)parent.elementSize);

                    if (((retrievedColor.A > 254) && (retrievedColor.G > 128)) || (tabLBM[tx, ty].isWall))
                    {
                        intersectsSmth = true;
                        userAcc = -1;
                    }
                }
                #endregion

                #region sciany ze swiatlami
                if (velocity >= -0.1f)
                {
                    if (parent.lightTabLBM[(int)posBumperLeft.X / parent.elementSize, (int)posBumperLeft.Y / parent.elementSize].isWall)
                        userAcc = -1;
                    else if (parent.lightTabLBM[(int)posBumperRight.X / parent.elementSize, (int)posBumperRight.Y / parent.elementSize].isWall)
                        userAcc = -1;
                    else if (parent.lightTabLBM[(int)framePointF.X / parent.elementSize, (int)framePointF.Y / parent.elementSize].isWall)
                        userAcc = -1;
                    else if (parent.lightTabLBM[(int)framePointFL.X / parent.elementSize, (int)framePointFL.Y / parent.elementSize].isWall)
                        userAcc = -1;
                    else if (parent.lightTabLBM[(int)framePointFR.X / parent.elementSize, (int)framePointFR.Y / parent.elementSize].isWall)
                        userAcc = -1;
                }
                #endregion

            }
            #endregion

            #region turning to avoid crossing lane markings - correction after vector map base
            //starting from center of front bumper, seek lane marking outwards
            var maxSeekWidth = (scale * spriteWidth * 0.55f);
            for (float i = 1f; i <= maxSeekWidth; i += 1f)
            {
                Color c = parent.GetColorFromLogicMapAtPoint(framePointF + (leftSeeker * i));
                if (c.A > 254 && c.B > 128)
                {
                    userSteer = 0.75f * (maxSeekWidth - i) / maxSeekWidth;
                    break;
                }
                else
                {
                    c = parent.GetColorFromLogicMapAtPoint(framePointF - (leftSeeker * i));
                    if (c.A > 254 && c.B > 128)
                    {
                        userSteer = -0.75f * (maxSeekWidth - i) / maxSeekWidth;
                        break;
                    }
                }
            }

            #endregion

            #region turning to overtake slow cars
            if (velocity < 4f)
            {
                float frontDetector = dist * pixelToMeterRatio + parent.elementSize + 1f;
                Vector2 frontBumperL = framePointF + frontDetector * frontSeeker + (leftSeeker * (scale * spriteWidth * 0.55f + dist * 3f));
                Vector2 frontBumperR = framePointF + frontDetector * frontSeeker - (leftSeeker * (scale * spriteWidth * 0.55f + dist * 3f));

                bool frontBumterLintersects = false;
                bool frontBumterRintersects = false;

                Parallel.ForEach(parent.cars, otherCar =>
                {
                    if (frontBumterLintersects && frontBumterRintersects)
                        return;

                    if (!otherCar.Equals(this) && ((otherCar.position - this.position).Length() <= (2 * spriteHeight)))
                    {
                        if (OrientationHelper.Intersection(frontBumperL, position, otherCar.framePointRL, otherCar.framePointRR)
                            || OrientationHelper.Intersection(frontBumperL, position, otherCar.framePointRL, otherCar.framePointFL)
                            || OrientationHelper.Intersection(frontBumperL, position, otherCar.framePointRR, otherCar.framePointRR))
                            frontBumterLintersects = true;
                        if (OrientationHelper.Intersection(frontBumperR, position, otherCar.framePointRL, otherCar.framePointRR)
                            || OrientationHelper.Intersection(frontBumperR, position, otherCar.framePointRL, otherCar.framePointFL)
                            || OrientationHelper.Intersection(frontBumperR, position, otherCar.framePointRR, otherCar.framePointRR))
                            frontBumterRintersects = true;
                    }
                });

                //jak tylko jeden z nich zostal wykryty to mozna zaczac wymijanie
                //XOR :)
                if (frontBumterLintersects ^ frontBumterRintersects)
                {
                    if (frontBumterLintersects) userSteer = 1;
                    else userSteer = -1;
                }
            }
            #endregion

            #region skrecanie - ograniczenie kierunku ruchu jesli zbyt odstaje od pozadanego
            {
                //obliczenie roznicy kierunku jazdy oraz kata z gazu
                float angleDiff = desiredAngle - rotation;

                while (angleDiff > (float)(Math.PI))
                    angleDiff -= (float)(2.0 * Math.PI);
                while (angleDiff < -(float)(Math.PI))
                    angleDiff += (float)(2.0 * Math.PI);
                ////odpowiednie skrecenie kol w aucie
                if (angleDiff < -(float)(Math.PI / 3.0))
                    userSteer = -1;
                if (angleDiff > (float)(Math.PI / 3.0))
                    userSteer = 1;
            }
            #endregion

            #region turning to avoid other cars
            Parallel.ForEach(parent.cars, otherCar =>
            {
                if (!otherCar.Equals(this) && ((otherCar.position - this.position).Length() <= (2 * spriteHeight)))
                {
                    Vector2 posBumperLeft = new Vector2(), posBumperRight = new Vector2();
                    //specialnie poszerzony przedni zderzak by najpierw odbil a potem hamowal
                    for (int i = 0; i < 2; i++)
                    {
                        if (i == 1)
                        {
                            posBumperLeft.X = framePointF.X + ((float)Math.Cos(rotation - MathHelper.PiOver2) * scale * spriteWidth * 0.9f);
                            posBumperLeft.Y = framePointF.Y + ((float)Math.Sin(rotation - MathHelper.PiOver2) * scale * spriteWidth * 0.9f);
                            posBumperRight.X = framePointF.X + ((float)Math.Cos(rotation + MathHelper.PiOver2) * scale * spriteWidth * 0.9f);
                            posBumperRight.Y = framePointF.Y + ((float)Math.Sin(rotation + MathHelper.PiOver2) * scale * spriteWidth * 0.9f);
                        }
                        else
                        {
                            posBumperLeft.X = framePointF.X + ((float)Math.Cos(rotation - MathHelper.PiOver2) * scale * spriteWidth * 0.9f) - (frontSeeker.X * spriteHeight * scale * 0.25f);
                            posBumperLeft.Y = framePointF.Y + ((float)Math.Sin(rotation - MathHelper.PiOver2) * scale * spriteWidth * 0.9f) - (frontSeeker.Y * spriteHeight * scale * 0.25f);
                            posBumperRight.X = framePointF.X + ((float)Math.Cos(rotation + MathHelper.PiOver2) * scale * spriteWidth * 0.9f) - (frontSeeker.X * spriteHeight * scale * 0.25f);
                            posBumperRight.Y = framePointF.Y + ((float)Math.Sin(rotation + MathHelper.PiOver2) * scale * spriteWidth * 0.9f) - (frontSeeker.Y * spriteHeight * scale * 0.25f);
                        }

                        //sprawdzanie czy punkt sprawdzajacy jest wewnatrz ramek auta
                        //wystarczy sprawdzic czy przecina sie z ktoras z ramek auta
                        if ((OrientationHelper.Intersection(posBumperRight, position, otherCar.framePointFL, otherCar.framePointFR))
                            || (OrientationHelper.Intersection(posBumperRight, position, otherCar.framePointFR, otherCar.framePointRR))
                            || (OrientationHelper.Intersection(posBumperRight, position, otherCar.framePointRR, otherCar.framePointRL))
                            || (OrientationHelper.Intersection(posBumperRight, position, otherCar.framePointRL, otherCar.framePointFL)))
                            userSteer = -1;
                        else if ((OrientationHelper.Intersection(posBumperLeft, position, otherCar.framePointFL, otherCar.framePointFR))
                            || (OrientationHelper.Intersection(posBumperLeft, position, otherCar.framePointFR, otherCar.framePointRR))
                            || (OrientationHelper.Intersection(posBumperLeft, position, otherCar.framePointRR, otherCar.framePointRL))
                            || (OrientationHelper.Intersection(posBumperLeft, position, otherCar.framePointRL, otherCar.framePointFL)))
                            userSteer = 1;
                    }
                }
            });
            #endregion

            #region turning to avoid walkways and LBM walls
            {
                var startSeekWidth = (scale * spriteWidth * 0.25f);
                maxSeekWidth = (scale * spriteWidth * 0.75f);
                var steerToAvoid = 0f;

                for (float i = startSeekWidth; i <= maxSeekWidth; i += 0.5f)
                {
                    var frontLeft = framePointF + (leftSeeker * i);
                    var c = parent.GetColorFromLogicMapAtPoint(frontLeft);
                    if (parent.IsWalkway(c) || LBMTypeAtPosition(tabLBM, frontLeft) == LBMNodeType.Wall)
                    {
                        steerToAvoid += 0.75f * (maxSeekWidth - i) / (maxSeekWidth - startSeekWidth);
                        break;
                    }
                }

                for (float i = startSeekWidth; i <= maxSeekWidth; i += 0.5f)
                {
                    var frontRight = framePointF - (leftSeeker * i);
                    var c = parent.GetColorFromLogicMapAtPoint(frontRight);
                    if (parent.IsWalkway(c) || LBMTypeAtPosition(tabLBM, frontRight) == LBMNodeType.Wall)
                    {
                        steerToAvoid -= 0.75f * (maxSeekWidth - i) / (maxSeekWidth - startSeekWidth);
                        break;
                    }
                }

                if(steerToAvoid!=0)
                {
                    userSteer = steerToAvoid;
                }
            }
            #endregion

            #region skrecanie - nie wyjezdzanie z toru jazdy (poza nim brak danych gdzie jechac
            //{
            //    Vector2 posBumperLeft = new Vector2(), posBumperRight = new Vector2();
            //    //jak najezdza przednim zderzakiem na sciane to zeby odpowiednio skrecil by dalej nie najezdzac
            //    posBumperLeft = framePointFL + (leftSeeker * scale * spriteWidth * 0.25f);
            //    posBumperRight = framePointFR - (leftSeeker * scale * spriteWidth * 0.25f);
            //    Vector2 posBumperLeft2 = posBumperLeft - frontSeeker * scale * spriteWidth * 0.50f;
            //    Vector2 posBumperRight2 = posBumperRight - frontSeeker * scale * spriteWidth * 0.50f;

            //    if (
            //        ((posBumperLeft.X / parent.elementSize) < parent.countX - 1) &&
            //        (posBumperLeft.X > parent.elementSize) &&
            //        ((posBumperRight.X / parent.elementSize) < parent.countX - 1) &&
            //        (posBumperRight.X > parent.elementSize) &&
            //        ((posBumperLeft.Y / parent.elementSize) < parent.countY - 1) &&
            //        (posBumperLeft.Y > parent.elementSize) &&
            //        ((posBumperRight.Y / parent.elementSize) < parent.countY - 1) &&
            //        (posBumperRight.Y > parent.elementSize)
            //    )
            //    {
            //        if (tabLBM[(int)posBumperLeft.X / parent.elementSize, (int)posBumperLeft.Y / parent.elementSize].isWall)
            //            userSteer = 1;//by odbil w prawo
            //        else if (tabLBM[(int)posBumperRight.X / parent.elementSize, (int)posBumperRight.Y / parent.elementSize].isWall)
            //            userSteer = -1;//by odbil w lewo
            //    }

            //    posBumperLeft = posBumperLeft2; posBumperRight = posBumperRight2;
            //    if (
            //       ((posBumperLeft.X / parent.elementSize) < parent.countX - 1) &&
            //       (posBumperLeft.X > parent.elementSize) &&
            //       ((posBumperRight.X / parent.elementSize) < parent.countX - 1) &&
            //       (posBumperRight.X > parent.elementSize) &&
            //       ((posBumperLeft.Y / parent.elementSize) < parent.countY - 1) &&
            //       (posBumperLeft.Y > parent.elementSize) &&
            //       ((posBumperRight.Y / parent.elementSize) < parent.countY - 1) &&
            //       (posBumperRight.Y > parent.elementSize)
            //   )
            //        if (tabLBM[(int)posBumperLeft.X / parent.elementSize, (int)posBumperLeft.Y / parent.elementSize].isWall)
            //            userSteer = 1;//by odbil w prawo
            //        else if (tabLBM[(int)posBumperRight.X / parent.elementSize, (int)posBumperRight.Y / parent.elementSize].isWall)
            //            userSteer = -1;//by odbil w lewo

            //}
            #endregion


            //jak jedzie do tylu to zeby skrecil na odwrot...
            if (velocity < 0)
                userSteer *= -1f;
        }

        private LBMNodeType LBMTypeAtPosition(LBMElement[,] tabLBM, Vector2 vec)
        {
            var x = MathHelper.Clamp((int)(vec.X / parent.elementSize), 1, parent.countX - 2);
            var y = MathHelper.Clamp((int)(vec.Y / parent.elementSize), 1, parent.countY - 2);
            return tabLBM[x,y].nodeType;
        }

        private Vector2 FindClosestNormalCell(LBMElement[,] tabLBM, Vector2 position, int elementSize)
        {
            var tx = (int)(position.X / elementSize);
            var ty = (int)(position.Y / elementSize);

            for (var distance = 1; distance < Math.Min(parent.countX, parent.countY); distance++)
            {
                for (int i = 0; i < distance * distance; i++)
                {
                    int dx = i % distance - distance / 2;
                    int dy = i / distance - distance / 2;
                    int x = MathHelper.Clamp(tx + dx, 0, parent.countX-1);
                    int y = MathHelper.Clamp(ty + dy, 0, parent.countY-1);
                    if (tabLBM[x, y].isNormal)
                    {
                        return new Vector2(x * elementSize, y * elementSize);
                    }
                }

            }
            return new Vector2(0, 0);
        }

        /// <summary>
        /// Moves both cars from eachother to separate them
        /// </summary>
        const float separateDistance = 0.5f;
        private void SeparateCars(Car car1, Car car2)
        {
            var car1ToCar2Angle = Math.Atan2(car2.position.Y - car1.position.Y, car2.position.X - car1.position.X);
            car2.position.X += (float)(Math.Cos(car1ToCar2Angle) * separateDistance);
            car2.position.Y += (float)(Math.Sin(car1ToCar2Angle) * separateDistance);
            car1.position.X -= (float)(Math.Cos(car1ToCar2Angle) * separateDistance);
            car1.position.Y -= (float)(Math.Sin(car1ToCar2Angle) * separateDistance);
        }

        /// <summary>
        /// przesuwa auto na podstawie aktualnych parametrow skretu oraz predkosci
        /// </summary>
        public void Update(GameTime gameTime)
        {
            //skret
            float temp_max_steer = max_steer;//maksymalny skret uzalezniony od predkosci;
            if (velocity != 0f) temp_max_steer = MathHelper.ToRadians(900f / (Math.Abs(velocity) * 4f));

            if (temp_max_steer > max_steer) temp_max_steer = max_steer;
            steer = userSteer * temp_max_steer;

            //przyspieszanie w jezdzie do przodu
            if (velocity >= 0f)
            {
                if (userAcc < 0f) acc = userAcc * force_braking;
                else
                {
                    //1szy bieg
                    force_acc = 4.5f;//0.45G
                    //2 bieg
                    if (velocity > 14f) force_acc = 3f;//>50kmh 0.3G
                    //3 bieg
                    if (velocity > 25f) force_acc = 2.2f;//>90kmh 0.22G
                    //4 bieg
                    if (velocity > 33f) force_acc = 1.2f;//>120kmh 0.12G
                    //5 bieg
                    if (velocity > 41f) force_acc = 0.5f;//>150kmh 0.5G
                    //ograniczenie predkosci do 180kmh
                    if (velocity > 50f) force_acc = 0f;

                    acc = userAcc * force_acc;
                }
            }
            else //przyspieszenie w jezdzie do tylu
            {
                if (userAcc > 0f) acc = userAcc * force_braking;
                else
                {
                    //wsteczny bieg
                    force_acc = 4.5f;//0.45G
                    if (velocity < -14f) force_acc = 0f;//ograniczenie predkosci do 50kmh na wstecznym

                    acc = userAcc * force_acc;
                }
            }

            //do wyswietlania
            braking = (acc < 0f);
            accelerating = (acc > 0f);

            //update fizyki
            float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            // predkosc
            velocity += acc * elapsedSeconds;
            bool forward = (velocity > 0f);
            velocity += (velocity > 0f ? -1f : 1f) * friction * elapsedSeconds; //opor ruchu w zaleznosci od kierunku jazdy
            if ((velocity > 0f) != forward) //jesli po dodaniu oporu nagle zmieni sie nam kierunek ruchu to auto powinno sie zatrzymac
                velocity = 0f;
            // obrot
            rotation += steer * velocity * elapsedSeconds * some_steer_param;
            rotation = GeneralHelper.NormalizeAngle(rotation);
            //pozycja
            prevPosition = position;
            position.X += (float)(Math.Cos(rotation) * velocity * elapsedSeconds * pixelToMeterRatio);
            position.Y += (float)(Math.Sin(rotation) * velocity * elapsedSeconds * pixelToMeterRatio);

            //zmienne pomocnicze by nie liczyc wszystkiego pare razy...
            Vector2 front = new Vector2(), rear = new Vector2(), left = new Vector2();
            front.X = ((float)Math.Cos(rotation) * scale * spriteHeight * 0.75f);
            front.Y = ((float)Math.Sin(rotation) * scale * spriteHeight * 0.75f);
            rear.X = ((float)Math.Cos(rotation) * scale * spriteHeight * 0.25f);
            rear.Y = ((float)Math.Sin(rotation) * scale * spriteHeight * 0.25f);
            left.X = ((float)Math.Cos(rotation - MathHelper.PiOver2) * scale * spriteWidth * 0.5f);
            left.Y = ((float)Math.Sin(rotation - MathHelper.PiOver2) * scale * spriteWidth * 0.5f);

            //lewy rog przedniego zderzaka: pozycja + dlugosc + pol szerokosci auta w lewo wzg osi
            framePointFL = GeneralHelper.NormalizeVector(position + front + left);
            framePointFR = GeneralHelper.NormalizeVector(position + front - left);

            framePointRL = GeneralHelper.NormalizeVector(position - rear + left);
            framePointRR = GeneralHelper.NormalizeVector(position - rear - left);

            framePointF = GeneralHelper.NormalizeVector(position + front);
            framePointFF = GeneralHelper.NormalizeVector(position + front + front);//specialnie by 2 auta nie mogly sie w sobie pojawic
        }

        public void Draw(SpriteBatch spritebatch)
        {
            spritebatch.Draw(sprite, position, null, Color.White, rotation + rotationModel, center, scale, SpriteEffects.None, 0.5f);
            //malowanie hamulca
            if (braking)
                spritebatch.Draw(parent.texBrake, position, null, Color.White, rotation + rotationModel, center, scale, SpriteEffects.None, 0.5f);
            //malowanie acc
            if (accelerating)
                spritebatch.Draw(parent.texAcc, position, null, Color.White, rotation + rotationModel, center, scale, SpriteEffects.None, 0.5f);
            //malowanie skretu
            spritebatch.Draw(parent.texSteer, position, null, Color.White, rotation + rotationModel + steer, center, scale, SpriteEffects.None, 0.5f);

            //dane...
            //spritebatch.DrawString(parent.defaultFont, "V(m/s): " + velocity, new Vector2(1f, 20f), Color.White);
            //spritebatch.DrawString(parent.defaultFont, "Acc(G): " + acc / 10f, new Vector2(1f, 40f), Color.White);
            //spritebatch.DrawString(parent.defaultFont, "Skret(deg): " + MathHelper.ToDegrees(steer), new Vector2(1f, 60f), Color.White);

            //spritebatch.DrawString(parent.defaultFont, "x: " + position.X.ToString() + " y: " + position.Y.ToString(), new Vector2(position.X, position.Y), Color.White);
            //parent.DrawLine(parent.texWall, position, temp1, Color.Red);
            //parent.DrawLine(parent.texWall, position, temp2, Color.Red);
        }

        public bool IntersectsOtherCar(Car car)
        {
            if (!car.Equals(this))
            {
                //sprawdzanie czy punkt sprawdzajacy jest wewnatrz ramek auta
                //wystarczy sprawdzic czy przecina sie z ktoras z ramek auta
                for (int i = 0; i < 5; i++)
                {
                    Vector2 frameCheck = framePointF;
                    switch (i)
                    {
                        //case 0:
                        case 1: frameCheck = framePointFR; break;
                        case 2: frameCheck = framePointRR; break;
                        case 3: frameCheck = framePointRL; break;
                        case 4: frameCheck = framePointFL; break;
                    }

                    if ((OrientationHelper.Intersection(frameCheck, position, car.framePointFL, car.framePointFR))
                    || (OrientationHelper.Intersection(frameCheck, position, car.framePointFR, car.framePointRR))
                    || (OrientationHelper.Intersection(frameCheck, position, car.framePointRR, car.framePointRL))
                    || (OrientationHelper.Intersection(frameCheck, position, car.framePointRL, car.framePointFL)))
                        return true;
                }
            }
            return false;
        }

        public bool IntersectsOtherCarStart(Car car)
        {
            if (!car.Equals(this))
            {
                //sprawdzanie czy punkt sprawdzajacy jest wewnatrz ramek auta
                //wystarczy sprawdzic czy przecina sie z ktoras z ramek auta
                for (int i = 0; i < 5; i++)
                {
                    Vector2 frameCheck = framePointFF;
                    switch (i)
                    {
                        //case 0:
                        case 1: frameCheck = framePointFR; break;
                        case 2: frameCheck = framePointRR; break;
                        case 3: frameCheck = framePointRL; break;
                        case 4: frameCheck = framePointFL; break;
                    }

                    if (OrientationHelper.Intersection(frameCheck, position, car.framePointFL, car.framePointFR))
                        return true;
                    else if (OrientationHelper.Intersection(frameCheck, position, car.framePointFR, car.framePointRR))
                        return true;
                    else if (OrientationHelper.Intersection(frameCheck, position, car.framePointRR, car.framePointRL))
                        return true;
                    else if (OrientationHelper.Intersection(frameCheck, position, car.framePointRL, car.framePointFL))
                        return true;
                }
            }
            return false;
        }

        public bool IntersectsOtherCarWithBack(Car car)
        {
            if (!car.Equals(this))
            {
                //sprawdzanie czy punkt sprawdzajacy jest wewnatrz ramek auta
                //wystarczy sprawdzic czy przecina sie z ktoras z ramek auta
                for (int i = 0; i < 2; i++)
                {
                    Vector2 frameCheck = framePointRR;
                    switch (i)
                    {
                        //case 0:
                        case 1: frameCheck = framePointRR; break;
                    }

                    if (OrientationHelper.Intersection(frameCheck, framePointF, car.framePointFL, car.framePointFR))
                        return true;
                    else if (OrientationHelper.Intersection(frameCheck, framePointF, car.framePointFR, car.framePointRR))
                        return true;
                    else if (OrientationHelper.Intersection(frameCheck, framePointF, car.framePointRR, car.framePointRL))
                        return true;
                    else if (OrientationHelper.Intersection(frameCheck, framePointF, car.framePointRL, car.framePointFL))
                        return true;
                }
            }
            return false;
        }

        public bool IntersectsOtherCarWithFront(Car car)
        {
            if (!car.Equals(this))
            {
                //sprawdzanie czy punkt sprawdzajacy jest wewnatrz ramek auta
                //wystarczy sprawdzic czy przecina sie z ktoras z ramek auta
                for (int i = 0; i < 2; i++)
                {
                    Vector2 frameCheck = framePointFL;
                    switch (i)
                    {
                        //case 0:
                        case 1: frameCheck = framePointFR; break;
                    }

                    if (OrientationHelper.Intersection(frameCheck, position, car.framePointFL, car.framePointFR))
                        return true;
                    else if (OrientationHelper.Intersection(frameCheck, position, car.framePointFR, car.framePointRR))
                        return true;
                    else if (OrientationHelper.Intersection(frameCheck, position, car.framePointRR, car.framePointRL))
                        return true;
                    else if (OrientationHelper.Intersection(frameCheck, position, car.framePointRL, car.framePointFL))
                        return true;
                }
            }
            return false;
        }

        public void DoManualSteer(KeyboardState keybstate)
        {
            if (keybstate.IsKeyDown(Keys.Left))
                userSteer = -1;
            else if (keybstate.IsKeyDown(Keys.Right))
                userSteer = 1;
            if (keybstate.IsKeyDown(Keys.Up))
                userAcc = 1;
            else if (keybstate.IsKeyDown(Keys.Down))
                userAcc = -1;
            else if (keybstate.IsKeyDown(Keys.Space))
                if (velocity > 0) userAcc = -1;
                else if (velocity < 0) userAcc = 1;
        }
    }
}
