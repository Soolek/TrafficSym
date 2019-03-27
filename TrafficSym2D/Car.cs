﻿using System;
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

namespace TrafficSym2D
{
    public class Car
    {
        Game1 parent;

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
        public Vector2 position;
        private Vector2 prevPosition;
        private int stopCounter;
        private float rotation;
        public float getRotation()
        {
            return rotation;
        }
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
        public Car(Game1 _parent, Texture2D _sprite, Vector2 _position, float rotation, int tabLBMIndex, float aggressiveness)
        {
            parent = _parent;
            sprite = _sprite;
            position = _position;
            this.rotation = rotation;
            center = new Vector2(sprite.Width / 2.0f, sprite.Height * 0.75f);
            this.tabLBMIndex = tabLBMIndex;
            steer = 0f;
            acc = 0f;
            steer = 0f;
            velocity = 0f;
            _aggressiveness = aggressiveness;
            //ustalanie fizyki na bazie wielkosci sprite'a
            if (sprite.Height > 70)
            {
                force_acc *= (70f / (float)sprite.Height);
                friction *= ((float)sprite.Height / 70f);
            }
        }

        public void DoAI(LBMElement[,] tabLBM)
        {
            userAcc = 0;
            userSteer = 0;
            bool intersectsSmth = false;

            Vector2 leftSeeker = new Vector2(((float)Math.Cos(rotation - MathHelper.PiOver2)), ((float)Math.Sin(rotation - MathHelper.PiOver2)));
            Vector2 frontSeeker = new Vector2(((float)Math.Cos(rotation)), ((float)Math.Sin(rotation)));

            desiredAngle = getRotation();//przechowuje sugerowany kierunek ruchu wynikajacy z tabLBM

            #region dazenie do celu - skret jak i gaz
            //czyli jechanie po wektorach z gazu do celu + uwazanie na miejsca w ktorych tych wskazowek niema

            //wyciaganie kierunku jazdy z gazu
            int tx = (int)(position.X / (float)parent.elementSize2);
            int ty = (int)(position.Y / (float)parent.elementSize2);
            //Vector2 posBumperFrontTemp = new Vector2();
            //float disttemp = velocity * velocity / force_braking;
            //if (velocity < 0)
            //    if (disttemp > 1f) disttemp = 1f;
            //disttemp /= (1f + aggressiveness / 2f);
            //float dist2temp = (disttemp * pixelToMeterRatio) + parent.elementSize2;
            //posBumperFrontTemp.X = framePointF.X + ((float)Math.Cos(rotation) * dist2temp);
            //posBumperFrontTemp.Y = framePointF.Y + ((float)Math.Sin(rotation) * dist2temp);

            //int tx = (int)(posBumperFrontTemp.X / (float)parent.elementSize2);
            //int ty = (int)(posBumperFrontTemp.Y / (float)parent.elementSize2);
            float vx, vy;//przechowuja kierunek

            //jako sugestie kierunku bierzemy pole na ktorym auto jest z waga *4 + cztery okoliczne pola
            if ((tx < (parent.countNotStaticX - 1)) && (ty < (parent.countNotStaticY - 1)))
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
                float angleDiff = vectorAngle - getRotation();

                while (angleDiff > (float)(Math.PI))
                    angleDiff -= (float)(2.0 * Math.PI);
                while (angleDiff < -(float)(Math.PI))
                    angleDiff += (float)(2.0 * Math.PI);
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
                userAcc = ((1f - Math.Abs(userSteer)) + 0.05f) * (8f + 4f * aggressiveness) - velocity;
                userAcc = MathHelper.Clamp(userAcc, 0f, 0.3f + 0.7f * aggressiveness);
            }
            else //jak dane nie sa wystarczajace
            {
                if (tabLBM[tx, ty].isSource) userAcc = 1;
            }
            //zabezpieczenie jak w sciane wjedzie
            if (tabLBM[tx, ty].isWall)
            {
                intersectsSmth = true;
                if (velocity > 0) userAcc = -1;
                else userAcc = -0.5f;
            }
            #endregion

            ///////////////////////// Sekcja swiadomosci pasow oraz chodnikow /////////////////////////////////////////
            //czyli nie wjezdzanie na chodniki + nie przekraczanie pasow jesli nie ma potrzeby (odpowiednio mocny user steer) z poprzedniej sekcji

            //kontrola gazem jak jedziemy z duza predkoscia i sie do czegos zblizamy
            //zalezne od predkosc - 30f to ok 100kmh, hamowanie z 0.8G
            //jak cos znajdzie sie miedzy wyliczonym punktem a autem to trzeba na maksa hamowac
            // d = v^2 / a

            //punktowy vektor rownolegly z przednim zderzakiem


            #region przyspieszanie - sprawdzanie chodnika, swiatel oraz pozostalych uczestnikow ruchu by w nich nie wjechac, takze puszczanie z prawej
            float dist = velocity * velocity / force_braking;
            if (velocity < 0)
                if (dist > 1f) dist = 1f;
            dist /= (1f + aggressiveness / 2f);

            #region inni uczestnicy ruchu
            {
                float dist2 = (dist * pixelToMeterRatio) + parent.elementSize2;

                Vector2 posBumperLeftCar = new Vector2();//do wykrywania innych aut
                Vector2 posBumperRightCar = new Vector2();
                Vector2 posBumperFront = new Vector2();

                if (velocity > 0.05f)
                {
                    posBumperLeftCar.X = framePointFL.X + ((float)Math.Cos(rotation) * dist2) + (leftSeeker.X * scale * sprite.Width * 0.1f);
                    posBumperLeftCar.Y = framePointFL.Y + ((float)Math.Sin(rotation) * dist2) + (leftSeeker.Y * scale * sprite.Width * 0.1f);
                    posBumperRightCar.X = framePointFR.X + ((float)Math.Cos(rotation) * dist2) - (leftSeeker.X * scale * sprite.Width * 0.2f);
                    posBumperRightCar.Y = framePointFR.Y + ((float)Math.Sin(rotation) * dist2) - (leftSeeker.Y * scale * sprite.Width * 0.2f);
                }
                else
                {
                    posBumperLeftCar.X = framePointFL.X + ((float)Math.Cos(rotation) * dist2) + (leftSeeker.X * scale * sprite.Width * 0.05f);
                    posBumperLeftCar.Y = framePointFL.Y + ((float)Math.Sin(rotation) * dist2) + (leftSeeker.Y * scale * sprite.Width * 0.05f);
                    posBumperRightCar.X = framePointFR.X + ((float)Math.Cos(rotation) * dist2) - (leftSeeker.X * scale * sprite.Width * 0.1f);
                    posBumperRightCar.Y = framePointFR.Y + ((float)Math.Sin(rotation) * dist2) - (leftSeeker.Y * scale * sprite.Width * 0.1f);
                }

                posBumperFront.X = framePointF.X + ((float)Math.Cos(rotation) * dist2);
                posBumperFront.Y = framePointF.Y + ((float)Math.Sin(rotation) * dist2);

                foreach (Car car in parent.cars)
                {
                    if (userAcc == -1) break;
                    if (!car.Equals(this))
                    {
                        //sprawdzanie czy punkt sprawdzajacy jest wewnatrz ramek auta
                        //wystarczy sprawdzic czy przecina sie z ktoras z ramek auta

                        //sprawdzamy jak nasze i(czujka)+dlugosc auta jest wieksze od roznicy odleglosci aut
                        if ((car.position - this.position).Length() <= (dist + 2 * sprite.Height))
                        {
                            #region lewy
                            if (OrientationHelper.Intersection(posBumperLeftCar, position, car.framePointFL, car.framePointFR))
                            {
                                intersectsSmth = true;
                                userAcc = (velocity == 0 ? -1 : -velocity);
                            }
                            else if (OrientationHelper.Intersection(posBumperLeftCar, position, car.framePointFR, car.framePointRR))
                            {
                                intersectsSmth = true;
                                userAcc = (velocity == 0 ? -1 : -velocity);
                            }
                            else if (OrientationHelper.Intersection(posBumperLeftCar, position, car.framePointRR, car.framePointRL))
                            {
                                intersectsSmth = true;
                                userAcc = (velocity == 0 ? -1 : -velocity);
                            }
                            else if (OrientationHelper.Intersection(posBumperLeftCar, position, car.framePointRL, car.framePointFL))
                            {
                                intersectsSmth = true;
                                userAcc = (velocity == 0 ? -1 : -velocity);
                            }
                            #endregion
                            #region prawy
                            else if (OrientationHelper.Intersection(posBumperRightCar, position, car.framePointFL, car.framePointFR))
                            {
                                intersectsSmth = true;
                                userAcc = (velocity == 0 ? -1 : -velocity);
                            }
                            else if (OrientationHelper.Intersection(posBumperRightCar, position, car.framePointFR, car.framePointRR))
                            {
                                intersectsSmth = true;
                                userAcc = (velocity == 0 ? -1 : -velocity);
                            }
                            else if (OrientationHelper.Intersection(posBumperRightCar, position, car.framePointRR, car.framePointRL))
                            {
                                intersectsSmth = true;
                                userAcc = (velocity == 0 ? -1 : -velocity);
                            }
                            else if (OrientationHelper.Intersection(posBumperRightCar, position, car.framePointRL, car.framePointFL))
                            {
                                intersectsSmth = true;
                                userAcc = (velocity == 0 ? -1 : -velocity);
                            }
                            #endregion
                            #region na koncu sprawdzamy poprostu od przodu tylko tylni zderzak
                            else if (OrientationHelper.Intersection(posBumperFront, position, car.framePointRR, car.framePointRL))
                            {
                                intersectsSmth = true;
                                userAcc = (velocity == 0 ? -1 : -velocity);
                            }
                            #endregion
                            #region puszczanie z prawej...
                            else
                            {
                                float angleOtherCar = (float)Math.Atan2(car.position.Y - position.Y, car.position.X - position.X);
                                angleOtherCar -= desiredAngle;
                                angleOtherCar = GeneralHelper.NormalizeAngle(angleOtherCar);

                                if ((angleOtherCar > 0f) && (angleOtherCar < MathHelper.PiOver2)) //jak miedzy 0 a 90 stopni i wystarczajaco blisko
                                {
                                    float angleRelative = GeneralHelper.NormalizeAngle(desiredAngle - car.rotation);
                                    float param = (float)Math.Sin(angleRelative);

                                    Vector2 tempv = framePointFR - car.framePointFL;
                                    float lengthDiff = tempv.Length();
                                    tempv = framePointFR - car.position;
                                    if (tempv.Length() < lengthDiff) lengthDiff = tempv.Length();
                                    tempv = framePointFR - car.framePointRL;
                                    if (tempv.Length() < lengthDiff) lengthDiff = tempv.Length();

                                    float vel = car.velocity;
                                    if ((vel < 3f) && (vel > 0.1f)) vel = 3f;

                                    float addParam = sprite.Width / 2f;
                                    if ((vel * velocity) < 1f) addParam *= (vel * velocity);

                                    if (((vel * velocity * pixelToMeterRatio / car.force_braking + addParam) * param) > lengthDiff)
                                    {
                                        intersectsSmth = true;
                                        userAcc = (velocity <= 0 ? 0 : -velocity);
                                    }
                                }
                            }
                            #endregion
                        }
                    }
                }
            }
            #endregion

            //parametrem bedzie userAcc czy == -1
            for (int i = (int)(dist * pixelToMeterRatio) + parent.elementSize2; i >= 0; i -= parent.elementSize2)
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

                #region chodnik olewamy jak jedziemy powoli
                if (velocity > 0.5f)
                {
                    Rectangle sourceRectangle = new Rectangle((int)posBumperLeft.X, (int)posBumperLeft.Y, 1, 1);
                    Color[] retrievedColor = new Color[1];
                    parent.mapLogicTexture.GetData<Color>(0, sourceRectangle, retrievedColor, 0, 1);

                    tx = (int)(posBumperLeft.X / (float)parent.elementSize2);
                    ty = (int)(posBumperLeft.Y / (float)parent.elementSize2);

                    if (((retrievedColor[0].A > 254) && (retrievedColor[0].G > 128)) || (tabLBM[tx, ty].isWall))
                    {
                        intersectsSmth = true;
                        userAcc = -1;
                    }
                    sourceRectangle = new Rectangle((int)posBumperRight.X, (int)posBumperRight.Y, 1, 1);
                    parent.mapLogicTexture.GetData<Color>(0, sourceRectangle, retrievedColor, 0, 1);

                    tx = (int)(posBumperRight.X / (float)parent.elementSize2);
                    ty = (int)(posBumperRight.Y / (float)parent.elementSize2);

                    if (((retrievedColor[0].A > 254) && (retrievedColor[0].G > 128)) || (tabLBM[tx, ty].isWall))
                    {
                        intersectsSmth = true;
                        userAcc = -1;
                    }
                }
                #endregion

                #region sciany ze swiatlami
                if ((aggressiveness < 0.9) && (velocity >= -0.1f))
                {
                    if (parent.lightTabLBM[(int)posBumperLeft.X / parent.elementSize2, (int)posBumperLeft.Y / parent.elementSize2].isWall)
                        userAcc = -1;
                    else if (parent.lightTabLBM[(int)posBumperRight.X / parent.elementSize2, (int)posBumperRight.Y / parent.elementSize2].isWall)
                        userAcc = -1;
                    else if (parent.lightTabLBM[(int)framePointF.X / parent.elementSize2, (int)framePointF.Y / parent.elementSize2].isWall)
                        userAcc = -1;
                    else if (parent.lightTabLBM[(int)framePointFL.X / parent.elementSize2, (int)framePointFL.Y / parent.elementSize2].isWall)
                        userAcc = -1;
                    else if (parent.lightTabLBM[(int)framePointFR.X / parent.elementSize2, (int)framePointFR.Y / parent.elementSize2].isWall)
                        userAcc = -1;
                }
                #endregion

            }
            #endregion

            #region przyspieszenie - taki trick by jak stoja dlugo to by cos sie ruszylo w sytuacji patowej
            if (intersectsSmth && (stopCounter + (int)(10 * aggressiveness) > 10) && (velocity < 0.5f))
            {
                bool isInSmth = false;
                if (parent.IsWalkway(parent.GetColorFromLogicMapAtPoint(framePointFL)))
                    isInSmth = true;
                else if (parent.IsWalkway(parent.GetColorFromLogicMapAtPoint(framePointFR)))
                    isInSmth = true;
                else if (parent.IsWalkway(parent.GetColorFromLogicMapAtPoint(framePointRR)))
                    isInSmth = true;
                else if (parent.IsWalkway(parent.GetColorFromLogicMapAtPoint(framePointRL)))
                    isInSmth = true;

                if (isInSmth)
                    userAcc = 1;
                else //literowanie przez auta i jesli jest w ktoryms...
                {
                    foreach (Car car in parent.cars)
                        if ((car.position - this.position).Length() <= (2 * sprite.Height))
                        {
                            if (IntersectsOtherCarWithBack(car))
                            {
                                userAcc = 1;
                                break;
                            }
                            if (IntersectsOtherCarWithFront(car))
                            {
                                if (velocity > 0) userAcc = -velocity;
                                break;
                            }
                        }
                }
            }
            #endregion

            #region skrecanie - by nie wyjezdzac poza pasy - korekcja po dazeniu do celu
            bool laneDone = false;

            //jedziemy od zew krancow
            for (float i = 1f; i <= (scale * sprite.Width * 0.55f); i += 2f)
            {
                if (laneDone) break;
                Color c = parent.GetColorFromLogicMapAtPoint(framePointF + (leftSeeker * i));
                if (c.A > 254 && c.B > 128)
                {
                    userSteer = 1;
                    laneDone = true;
                }
                else
                {
                    c = parent.GetColorFromLogicMapAtPoint(framePointF - (leftSeeker * i));
                    if (c.A > 254 && c.B > 128)
                    {
                        userSteer = -1;
                        laneDone = true;
                    }
                }
            }

            #endregion

            #region skrecanie - jak jedzie wolno to jesli moze niech wyminie auto przed nim
            if (velocity < 4f) //distance przy jakim bedzie zwalniac to max 8px
            {
                float frontDetector = dist * pixelToMeterRatio + parent.elementSize2 + 1f;
                Vector2 frontBumperL = framePointF + frontDetector * frontSeeker + (leftSeeker * (scale * sprite.Width * 0.55f + dist * 3f));
                Vector2 frontBumperR = framePointF + frontDetector * frontSeeker - (leftSeeker * (scale * sprite.Width * 0.55f + dist * 3f));

                bool frontBumterLintersects = false;
                bool frontBumterRintersects = false;

                foreach (Car car in parent.cars)
                {
                    if (frontBumterLintersects && frontBumterRintersects) break;
                    if (!car.Equals(this) && ((car.position - this.position).Length() <= (2 * sprite.Height)))
                    {
                        if (OrientationHelper.Intersection(frontBumperL, position, car.framePointRL, car.framePointRR)
                            || OrientationHelper.Intersection(frontBumperL, position, car.framePointRL, car.framePointFL)
                            || OrientationHelper.Intersection(frontBumperL, position, car.framePointRR, car.framePointRR))
                            frontBumterLintersects = true;
                        if (OrientationHelper.Intersection(frontBumperR, position, car.framePointRL, car.framePointRR)
                            || OrientationHelper.Intersection(frontBumperR, position, car.framePointRL, car.framePointFL)
                            || OrientationHelper.Intersection(frontBumperR, position, car.framePointRR, car.framePointRR))
                            frontBumterRintersects = true;
                    }
                }

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
                float angleDiff = desiredAngle - getRotation();

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
            
            #region skrecanie - by nie wjechac w inne auta
            foreach (Car car in parent.cars)
            {
                if (!car.Equals(this) && ((car.position - this.position).Length() <= (2 * sprite.Height)))
                {
                    Vector2 posBumperLeft = new Vector2(), posBumperRight = new Vector2();
                    //specialnie poszerzony przedni zderzak by najpierw odbil a potem hamowal
                    for (int i = 0; i < 2; i++)
                    {
                        if (i == 1)
                        {
                            posBumperLeft.X = framePointF.X + ((float)Math.Cos(rotation - MathHelper.PiOver2) * scale * sprite.Width * 0.9f);
                            posBumperLeft.Y = framePointF.Y + ((float)Math.Sin(rotation - MathHelper.PiOver2) * scale * sprite.Width * 0.9f);
                            posBumperRight.X = framePointF.X + ((float)Math.Cos(rotation + MathHelper.PiOver2) * scale * sprite.Width * 0.9f);
                            posBumperRight.Y = framePointF.Y + ((float)Math.Sin(rotation + MathHelper.PiOver2) * scale * sprite.Width * 0.9f);
                        }
                        else
                        {
                            posBumperLeft.X = framePointF.X + ((float)Math.Cos(rotation - MathHelper.PiOver2) * scale * sprite.Width * 0.9f) - (frontSeeker.X * sprite.Height * scale * 0.25f);
                            posBumperLeft.Y = framePointF.Y + ((float)Math.Sin(rotation - MathHelper.PiOver2) * scale * sprite.Width * 0.9f) - (frontSeeker.Y * sprite.Height * scale * 0.25f);
                            posBumperRight.X = framePointF.X + ((float)Math.Cos(rotation + MathHelper.PiOver2) * scale * sprite.Width * 0.9f) - (frontSeeker.X * sprite.Height * scale * 0.25f);
                            posBumperRight.Y = framePointF.Y + ((float)Math.Sin(rotation + MathHelper.PiOver2) * scale * sprite.Width * 0.9f) - (frontSeeker.Y * sprite.Height * scale * 0.25f);
                        }

                        //sprawdzanie czy punkt sprawdzajacy jest wewnatrz ramek auta
                        //wystarczy sprawdzic czy przecina sie z ktoras z ramek auta
                        if (OrientationHelper.Intersection(posBumperRight, position, car.framePointFL, car.framePointFR))
                            userSteer = -1;
                        else if (OrientationHelper.Intersection(posBumperRight, position, car.framePointFR, car.framePointRR))
                            userSteer = -1;
                        else if (OrientationHelper.Intersection(posBumperRight, position, car.framePointRR, car.framePointRL))
                            userSteer = -1;
                        else if (OrientationHelper.Intersection(posBumperRight, position, car.framePointRL, car.framePointFL))
                            userSteer = -1;
                        else if (OrientationHelper.Intersection(posBumperLeft, position, car.framePointFL, car.framePointFR))
                            userSteer = 1;
                        else if (OrientationHelper.Intersection(posBumperLeft, position, car.framePointFR, car.framePointRR))
                            userSteer = 1;
                        else if (OrientationHelper.Intersection(posBumperLeft, position, car.framePointRR, car.framePointRL))
                            userSteer = 1;
                        else if (OrientationHelper.Intersection(posBumperLeft, position, car.framePointRL, car.framePointFL))
                            userSteer = 1;
                    }
                }
            }
            #endregion

            #region skrecanie - nie wjezdzanie na chodnik
            {
                Vector2 posBumperLeft = new Vector2(), posBumperRight = new Vector2();
                posBumperLeft.X = position.X + ((float)Math.Cos(rotation) * scale * sprite.Height) + ((float)Math.Cos(rotation - MathHelper.PiOver2) * scale * sprite.Width * 0.75f);
                posBumperLeft.Y = position.Y + ((float)Math.Sin(rotation) * scale * sprite.Height) + ((float)Math.Sin(rotation - MathHelper.PiOver2) * scale * sprite.Width * 0.75f);
                posBumperRight.X = position.X + ((float)Math.Cos(rotation) * scale * sprite.Height) + ((float)Math.Cos(rotation + MathHelper.PiOver2) * scale * sprite.Width * 0.75f);
                posBumperRight.Y = position.Y + ((float)Math.Sin(rotation) * scale * sprite.Height) + ((float)Math.Sin(rotation + MathHelper.PiOver2) * scale * sprite.Width * 0.75f);

                if (parent.IsWalkway(parent.GetColorFromLogicMapAtPoint(posBumperRight)))
                    userSteer = -1;
                else if (parent.IsWalkway(parent.GetColorFromLogicMapAtPoint(posBumperLeft)))
                    userSteer = 1;
            }
            #endregion

            #region skrecanie - nie wyjezdzanie z toru jazdy (poza nim brak danych gdzie jechac
            {
                Vector2 posBumperLeft = new Vector2(), posBumperRight = new Vector2();
                //jak najezdza przednim zderzakiem na sciane to zeby odpowiednio skrecil by dalej nie najezdzac
                posBumperLeft = framePointFL + (leftSeeker * scale * sprite.Width * 0.25f);
                posBumperRight = framePointFR - (leftSeeker * scale * sprite.Width * 0.25f);
                Vector2 posBumperLeft2 = posBumperLeft - frontSeeker * scale * sprite.Width * 0.50f;
                Vector2 posBumperRight2 = posBumperRight - frontSeeker * scale * sprite.Width * 0.50f;

                if (
                    ((posBumperLeft.X / parent.elementSize2) < parent.countNotStaticX - 1) &&
                    (posBumperLeft.X > parent.elementSize2) &&
                    ((posBumperRight.X / parent.elementSize2) < parent.countNotStaticX - 1) &&
                    (posBumperRight.X > parent.elementSize2) &&
                    ((posBumperLeft.Y / parent.elementSize2) < parent.countNotStaticY - 1) &&
                    (posBumperLeft.Y > parent.elementSize2) &&
                    ((posBumperRight.Y / parent.elementSize2) < parent.countNotStaticY - 1) &&
                    (posBumperRight.Y > parent.elementSize2)
                )
                {
                    if (tabLBM[(int)posBumperLeft.X / parent.elementSize2, (int)posBumperLeft.Y / parent.elementSize2].isWall)
                        userSteer = 1;//by odbil w prawo
                    else if (tabLBM[(int)posBumperRight.X / parent.elementSize2, (int)posBumperRight.Y / parent.elementSize2].isWall)
                        userSteer = -1;//by odbil w lewo
                }

                posBumperLeft = posBumperLeft2; posBumperRight = posBumperRight2;
                if (
                   ((posBumperLeft.X / parent.elementSize2) < parent.countNotStaticX - 1) &&
                   (posBumperLeft.X > parent.elementSize2) &&
                   ((posBumperRight.X / parent.elementSize2) < parent.countNotStaticX - 1) &&
                   (posBumperRight.X > parent.elementSize2) &&
                   ((posBumperLeft.Y / parent.elementSize2) < parent.countNotStaticY - 1) &&
                   (posBumperLeft.Y > parent.elementSize2) &&
                   ((posBumperRight.Y / parent.elementSize2) < parent.countNotStaticY - 1) &&
                   (posBumperRight.Y > parent.elementSize2)
               )
                    if (tabLBM[(int)posBumperLeft.X / parent.elementSize2, (int)posBumperLeft.Y / parent.elementSize2].isWall)
                        userSteer = 1;//by odbil w prawo
                    else if (tabLBM[(int)posBumperRight.X / parent.elementSize2, (int)posBumperRight.Y / parent.elementSize2].isWall)
                        userSteer = -1;//by odbil w lewo

            }
            #endregion

            //jak jedzie do tylu to zeby skrecil na odwrot...
            if (velocity < 0)
                userSteer *= -1f;
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
            float elapsedSeconds = (float)gameTime.ElapsedRealTime.TotalSeconds;
            // predkosc
            velocity += acc * elapsedSeconds;
            bool forward = (velocity > 0f);
            velocity += (velocity > 0f ? -1f : 1f) * friction * elapsedSeconds; //opor ruchu w zaleznosci od kierunku jazdy
            if ((velocity > 0f) != forward) //jesli po dodaniu oporu nagle zmieni sie nam kierunek ruchu to auto powinno sie zatrzymac
                velocity = 0f;
            // obrot
            rotation += steer * velocity * elapsedSeconds * some_steer_param;
            while (rotation > (float)(2.0 * Math.PI))
                rotation -= (float)(2.0 * Math.PI);
            while (rotation < 0)
                rotation += (float)(2.0 * Math.PI);
            //pozycja
            prevPosition = position;
            position.X += (float)(Math.Cos(rotation) * velocity * elapsedSeconds * pixelToMeterRatio);
            position.Y += (float)(Math.Sin(rotation) * velocity * elapsedSeconds * pixelToMeterRatio);

            //liczniki stopu
            if (((int)(prevPosition.X) == (int)(position.X)) && ((int)(prevPosition.Y) == (int)(position.Y)))
                stopCounter++;
            else
                stopCounter = 0;

            //zmienne pomocnicze by nie liczyc wszystkiego pare razy...
            Vector2 front = new Vector2(), rear = new Vector2(), left = new Vector2();
            front.X = ((float)Math.Cos(rotation) * scale * sprite.Height * 0.75f);
            front.Y = ((float)Math.Sin(rotation) * scale * sprite.Height * 0.75f);
            rear.X = ((float)Math.Cos(rotation) * scale * sprite.Height * 0.25f);
            rear.Y = ((float)Math.Sin(rotation) * scale * sprite.Height * 0.25f);
            left.X = ((float)Math.Cos(rotation - MathHelper.PiOver2) * scale * sprite.Width * 0.5f);
            left.Y = ((float)Math.Sin(rotation - MathHelper.PiOver2) * scale * sprite.Width * 0.5f);

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