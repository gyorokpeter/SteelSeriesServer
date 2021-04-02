using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenRGB.NET;
using OpenRGB.NET.Models;

namespace SteelSeriesServer
{
    /*

var deviceCount = openRgbClient.GetControllerCount();
var devices = openRgbClient.GetAllControllerData();

for (int i = 0; i < devices.Length; i++)
{
    Console.WriteLine(devices[i].Type);
    if (devices[i].Type == OpenRGB.NET.Enums.DeviceType.Keyboard)
    {
        var colors = devices[i].Colors;
        for (int j=0; j<colors.Length; ++j)
        {
            colors[j] = new OpenRGB.NET.Models.Color(0, 0, 0);
        }
        colors[2] = new OpenRGB.NET.Models.Color(255, 0, 255);
        openRgbClient.UpdateLeds(i, colors);
    }
}
Console.ReadLine();
return;*/
    class OpenRGBSender : Sender
    {
        private OpenRGBClient openRgbClient;
        private int keyboardDeviceIndex;
        private Color[] colors;
        public static readonly Dictionary<int, int> bitmap2OpenRgbKey = new Dictionary<int, int> {
                { keyboardWidth*-1+4,  107 },    //Corsair K70 MK2 "brightness"
                { keyboardWidth*-1+5,    8 },    //Corsair K70 MK2 "lock"
                { keyboardWidth*-1+17,  16 },    //Corsair K70 MK2 "mute"
                { keyboardWidth*0+17,   26 },    //Corsair K70 MK2 "stop"
                { keyboardWidth*0+18,   35 },    //Corsair K70 MK2 "previous track"
                { keyboardWidth*0+19,   44 },    //Corsair K70 MK2 "play/pause"
                { keyboardWidth*0+20,   53 },    //Corsair K70 MK2 "next track"
                { keyboardWidth*3+1,    13 },    //a
                { keyboardWidth*4+6,    59 },    //b
                { keyboardWidth*4+4,    40 },    //c
                { keyboardWidth*3+3,    31 },    //d
                { keyboardWidth*2+3,    30 },    //e
                { keyboardWidth*3+4,    39 },    //f
                { keyboardWidth*3+5,    49 },    //g
                { keyboardWidth*3+6,    58 },    //h
                { keyboardWidth*2+8,    76 },    //i
                { keyboardWidth*3+7,    67 },    //j
                { keyboardWidth*3+8,    77 },    //k
                { keyboardWidth*3+9,    87 },    //l
                { keyboardWidth*4+8,    78 },    //m
                { keyboardWidth*4+7,    68 },    //n
                { keyboardWidth*2+9,    86 },    //o
                { keyboardWidth*2+10,   95 },    //p
                { keyboardWidth*2+1,    12 },    //q
                { keyboardWidth*2+4,    38 },    //r
                { keyboardWidth*3+2,    21 },    //s
                { keyboardWidth*2+5,    48 },    //t
                { keyboardWidth*2+7,    66 },    //u
                { keyboardWidth*4+5,    50 },    //v
                { keyboardWidth*2+2,    20 },    //w
                { keyboardWidth*4+3,    32 },    //x
                { keyboardWidth*2+6,    57 },    //y
                { keyboardWidth*4+2,    22 },    //z
                { keyboardWidth*1+1,    11 },    //1
                { keyboardWidth*1+2,    19 },    //2
                { keyboardWidth*1+3,    29 },    //3
                { keyboardWidth*1+4,    37 },    //4
                { keyboardWidth*1+5,    47 },    //5
                { keyboardWidth*1+6,    56 },    //6
                { keyboardWidth*1+7,    65 },    //7
                { keyboardWidth*1+8,    75 },    //8
                { keyboardWidth*1+9,    85 },    //9
                { keyboardWidth*1+10,   94 },    //0
                { keyboardWidth*3+13,   98 },    //Enter
                { keyboardWidth*0+0,   112 },    //Esc
                { keyboardWidth*1+13,   25 },    //Backspace
                { keyboardWidth*2+0,     2 },    //Tab
                { keyboardWidth*5+5,    41 },    //Space
                { keyboardWidth*1+11,  103 },    //-/_
                { keyboardWidth*1+12,    7 },    //+/=
                { keyboardWidth*2+11,  104 },    //[/{
                { keyboardWidth*2+12,   70 },    //]/}
             // {keyboardWidth*3+12,   ??? },    //US Backslash, UK ~/#
             // {keyboardWidth*4+1,    ??? },    //UK Backslash/|
                { keyboardWidth*3+10,   96 },    //;/:
                { keyboardWidth*3+11,  105 },    //'/"
                { keyboardWidth*1+0,     1 },    //`/~
                { keyboardWidth*4+9,    88 },    //,/<
                { keyboardWidth*4+10,   97 },    //./>
                { keyboardWidth*4+11,  106 },    //?//
                { keyboardWidth*3+0,     3 },    //Caps Lock
                { keyboardWidth*0+1,    10 },    //F1
                { keyboardWidth*0+2,    18 },    //F2
                { keyboardWidth*0+3,    28 },    //F3
                { keyboardWidth*0+4,    36 },    //F4
                { keyboardWidth*0+5,    46 },    //F5
                { keyboardWidth*0+6,    55 },    //F6
                { keyboardWidth*0+7,    64 },    //F7
                { keyboardWidth*0+8,    74 },    //F8
                { keyboardWidth*0+9,    84 },    //F9
                { keyboardWidth*0+10,   93 },    //F10
                { keyboardWidth*0+11,  102 },    //F11
                { keyboardWidth*0+12,    6 },    //F12
                { keyboardWidth*0+13,   15 },    //PrtScr
                { keyboardWidth*0+14,   24 },    //Scroll Lock
                { keyboardWidth*0+15,   33 },    //Pause
                { keyboardWidth*1+14,   42 },    //Insert
                { keyboardWidth*1+15,   51 },    //Home
                { keyboardWidth*1+16,   60 },    //PgUp
                { keyboardWidth*2+14,   34 },    //Delete
                { keyboardWidth*2+15,   43 },    //End
                { keyboardWidth*2+16,   52 },    //PgDn
                { keyboardWidth*5+17,  108 },    //Right Arrow
                { keyboardWidth*5+15,   90 },    //Left Arrow
                { keyboardWidth*5+16,   99 },    //Down Arrow
                { keyboardWidth*4+15,   81 },    //Up Arrow
                { keyboardWidth*1+17,   62 },    //Num Lock
                { keyboardWidth*1+18,   72 },    //Numpad /
                { keyboardWidth*1+19,   82 },    //Numpad *
                { keyboardWidth*1+20,   91 },    //Numpad -
                { keyboardWidth*2+20,  100 },    //Numpad +
                { keyboardWidth*4+20,  109 },    //Numpad Enter
                { keyboardWidth*4+17,   73 },    //Numpad 1/End
                { keyboardWidth*4+18,   83 },    //Numpad 2/Down
                { keyboardWidth*4+19,   92 },    //Numpad 3/PgDn
                { keyboardWidth*3+17,   45 },    //Numpad 4/Left
                { keyboardWidth*3+18,   54 },    //Numpad 5
                { keyboardWidth*3+19,   63 },    //Numpad 6/Right
                { keyboardWidth*2+17,    9 },    //Numpad 7/Home
                { keyboardWidth*2+18,   17 },    //Numpad 8/Up
                { keyboardWidth*2+19,   27 },    //Numpad 9/PgUp
                { keyboardWidth*5+18,  101 },    //Numpad 0/Ins
                { keyboardWidth*5+19,  110 },    //Numpad ./Del
                { keyboardWidth*5+0,     5 },    //Left Ctrl
                { keyboardWidth*4+0,     4 },    //Left Shift
                { keyboardWidth*5+2,    23 },    //Left Alt
                { keyboardWidth*5+1,    14 },    //Left GUI
                { keyboardWidth*5+14,   71 },    //Right Ctrl
                { keyboardWidth*4+13,   61 },    //Right Shift
                { keyboardWidth*5+11,   69 },    //Right Alt
                { keyboardWidth*5+12,   79 },    //Right GUI
                { keyboardWidth*5+13,   89 },    //Menu
        };

        public override void Start()
        {
            openRgbClient = new OpenRGBClient(name: "SteelSeriesServer", autoconnect: true, timeout: 1000);
            var devices = openRgbClient.GetAllControllerData();
            keyboardDeviceIndex = -1;
            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i].Type == OpenRGB.NET.Enums.DeviceType.Keyboard)
                {
                    keyboardDeviceIndex = i;
                    colors = devices[i].Colors;
                    break;
                }
            }
            if (keyboardDeviceIndex == -1) throw new Exception("no keyboard found");
        }

        public override void Stop()
        {
            openRgbClient.Dispose();
        }

        public override void SetGameName(string game)
        {
            //NOP
        }

        public override void SetColor(int key, byte r, byte g, byte b)
        {
            if (bitmap2OpenRgbKey.ContainsKey(key))
            {
                colors[bitmap2OpenRgbKey[key]] = new OpenRGB.NET.Models.Color(r, g, b);
            }
        }

        public override void ApplyChanges()
        {
            openRgbClient.UpdateLeds(keyboardDeviceIndex, colors);
        }

    }
}
