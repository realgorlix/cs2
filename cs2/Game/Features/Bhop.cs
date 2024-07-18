using cs2.Config;
using cs2.Game.Objects;
using cs2.Offsets;

namespace cs2.Game.Features
{
    internal class Bhop
    {
        public static void Start()
        {
            _localPlayer = new LocalPlayer();
            _key = new Input.Key(32);
            new Thread(() =>
            {
                for (; ; )
                {
                    if (!Config.Configuration.Current.Bhop)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }
                    _key.Update();
                    Update();
                    Thread.Sleep(Configuration.Current.THR_DELAY_BHOP);
                }
            }).Start();
        }

        public static void Update()
        {
            _localPlayer.AddressBase = _localPlayer.ReadAddressBase();
            int flags = Memory.Read<int>(_localPlayer.AddressBase + OffsetsLoader.C_BaseEntity.m_fFlags);
            if (_key.state == Input.KeyState.DOWN)
                if (flags == 65665 || flags == 65667)
                    Input.MouseMiddle();
        }

        private static Input.Key _key = null!;
        private static LocalPlayer _localPlayer = null!;
    }
}
