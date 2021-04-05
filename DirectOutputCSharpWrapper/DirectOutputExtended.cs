using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DirectOutputCSharpWrapper
{
    public enum Leds
    {
        FireIllumination,
        FireARed,
        FireAGreen,
        FireBRed,
        FireBGreen,
        FireDRed,
        FireDGreen,
        FireERed,
        FireEGreen,
        Toggle12Red,
        Toggle12Green,
        Toggle34Red,
        Toggle34Green,
        Toggle56Red,
        Toggle56Green,
        POV2Red,
        POV2Green,
        ClutchIRed,
        ClutchIGreen,
        ThrottleIllumination
    }

    public enum LedState
    {
        Off,
        On
    }

    public enum MFDLines
    {
        FirstLine,
        SecondLine,
        ThirdLine
    }

    /// <summary>
    /// An extended high level api for DirectOutput
    /// </summary>
    public class DirectOutputExtended : DirectOutput
    {
        /// <summary>
        /// Overload of SetLed that accepts enumerators
        /// </summary>
        /// <param name="device"></param>
        /// <param name="page"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void SetLed(IntPtr device, int page, Leds index, LedState value)
        {
            SetLed(device, page, (int) index, (int) value);
        }

        public void SetString(IntPtr device, int page, MFDLines index, string text)
        {
            SetString(device, page, (int) index, text);
        }

        public List<IntPtr> ListDevices()
        {
            List<IntPtr> devices = new List<IntPtr>();
            Enumerate((device, target) =>
            {
                devices.Add(device);
                return target;
            });

            return devices;
        }
    }
}