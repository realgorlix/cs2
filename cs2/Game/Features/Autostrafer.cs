using System;
using System.Numerics;
using cs2.Game.Objects;
using cs2.Offsets;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace cs2.Game.Features
{
    internal class Autostrafer
    {
        private static Input.Key _key = null!;
        private static LocalPlayer _localPlayer = null!;
        private static ViGEmClient _vigemClient;
        private static IXbox360Controller _controller;
        private static Input.Key _W, _A, _S, _D;
        private static bool _connected = false;

        public static void Start()
        {
            //cheat infrastructure crap
            _localPlayer = new LocalPlayer();
            _key = new Input.Key(32);
            _W = new Input.Key(87);
            _A = new Input.Key(65);
            _S = new Input.Key(83);
            _D = new Input.Key(68);
            //initializign vigem client that emulates controller
            _vigemClient = new ViGEmClient();
            _controller = _vigemClient.CreateXbox360Controller();
            new Thread(() => //cheat infrastructure things
            {
                for (; ; )
                {
                    if (!Config.Configuration.Current.AutoStrafer)
                    {
                        if(_connected)
                        {
                            _connected = false;
                            _controller.Disconnect();
                        }
                        Thread.Sleep(1000);
                        continue;
                    }
                    if(!_connected)
                    {
                        _connected = true;
                        _controller.Connect(); // connecting joy only when function is enabled
                    }
                    _W.Update(); _A.Update(); _S.Update(); _D.Update();
                    _key.Update();
                    Update();
                    Thread.Sleep(16); //16 cuz 1/64 = 0.015625 rounded to 0.016 in miliseconds is 16 lol
                }
            }).Start();
        }

        private static float yawClean = 0f;

        public static void Update()
        {
            _localPlayer.AddressBase = _localPlayer.ReadAddressBase();
            var yaw = Memory.Read<float>(_localPlayer.AddressBase + OffsetsLoader.C_CSPlayerPawnBase.m_angEyeAngles + 4);
            yawClean = (float)Math.IEEERemainder((double)yaw + 360d, 360.0d);
            short forwardMove = 0, sideMove = 0;
            if (_key.state == Input.KeyState.DOWN)
            {
                //get player input for joystick shenanigans
                forwardMove += _W.state == Input.KeyState.DOWN ? (short)1f : (short)0;
                sideMove += _A.state == Input.KeyState.DOWN ? (short)-1f : (short)0;
                forwardMove += _S.state == Input.KeyState.DOWN ? (short)-1f : (short)0;
                sideMove += _D.state == Input.KeyState.DOWN ? (short)1f : (short)0;

                int flags = Memory.Read<int>(_localPlayer.AddressBase + OffsetsLoader.C_BaseEntity.m_fFlags);
                if (!(flags == 65665 || flags == 65667)) // not on ground
                {
                    AftoStreiv(forwardMove, sideMove, out float newfmove, out float newsmove);
                    //adjust for joystick input
                    forwardMove = (short)(newfmove * short.MaxValue);
                    sideMove = (short)(newsmove * short.MaxValue);
                }
            }
            else
            {
                //avoid random running (bad hack)
                forwardMove -= (short)(new Random().Next() % 10);
                sideMove -= (short)(new Random().Next() % 10);
            }
            _controller.SetAxisValue(Xbox360Axis.LeftThumbY, forwardMove);
            _controller.SetAxisValue(Xbox360Axis.LeftThumbX, sideMove);
            Program.Log($"X: {sideMove}, Y: {forwardMove}", ConsoleColor.DarkBlue);
        }

        private static void AftoStreiv(float forwardmove, float sidemove,out float fmove, out float smove)
        {
            //get player velocity vars
            Vector3 vel3World = Memory.Read<Vector3>(_localPlayer.AddressBase + OffsetsLoader.C_BaseEntity.m_vecVelocity);
            Vector2 vel2World = new Vector2
            {
                X = vel3World.X,
                Y = vel3World.Y
            };
            float vel = vel2World.Length();
            var vel2WorldNorm = Vector2.Normalize(vel2World);

            //get desired direction vars
            var wish2Player = new Vector2
            {
                Y = forwardmove,
                X = sidemove
            };
            var wish2PlayerNorm = Vector2.Normalize(wish2Player);
            var wish2WorldNorm = RotateVector2(wish2PlayerNorm,-yawClean);

            // >0 left; <0 right
            var dot = Vector2.Dot(wish2WorldNorm,vel2WorldNorm);
            
            // pasted
            var strafeAngle = Math.Clamp(RAD2DEG(Math.Atan(15.0f / vel)), 0.0f, 90.0f);

            //determine which side to strafe
            float mult = dot > 0f ? 1f : -1f;

            //out var for moving - velocity normal as reference direction
            Vector2 move = RotateVector2(vel2WorldNorm,(float)strafeAngle * mult);

            //air strafe in our desired direction
            move = RotateVector2(move,90f * mult);

            //need to translate global move to player move
            //...

            fmove = move.Y;
            smove = move.X;
        }

        private static double RAD2DEG(double val)
        {
            return val * 180.0f / Math.PI;
        }

        private static double DEG2RAD(double val)
        {
            return val * Math.PI / 180.0f ;
        }

        public static Vector2 RotateVector2(Vector2 v,float rot)
        {
            double a = DEG2RAD(rot);
            double x = Math.Cos(a) * v.X - Math.Sin(a) * v.Y;
            double y = Math.Sin(a) * v.X + Math.Cos(a) * v.Y;
            return new Vector2((float)x,(float)y);
        }
    }
}
