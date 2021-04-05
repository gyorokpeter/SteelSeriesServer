using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SteelSeriesServer
{
    class ArtemisSender : Sender
    {
        private String url;
        private String getLedsUrl;
        private String setLedsUrl;
        private HttpClient http;
        private JArray leds;
        private Dictionary<int, int> ledIndex;

        private static readonly Dictionary<int, String> bitmap2LedId = new Dictionary<int, String> {
                //{ keyboardWidth*-1+4,  ??? },    //Corsair K70 MK2 "brightness"
                //{ keyboardWidth*-1+5,  ??? },    //Corsair K70 MK2 "lock"
                { keyboardWidth*-1+17, "Keyboard_MediaMute" },    //Corsair K70 MK2 "mute"
                { keyboardWidth*0+17,  "Keyboard_MediaStop" },    //Corsair K70 MK2 "stop"
                { keyboardWidth*0+18,  "Keyboard_MediaPreviousTrack" },    //Corsair K70 MK2 "previous track"
                { keyboardWidth*0+19,  "Keyboard_MediaPlay" },    //Corsair K70 MK2 "play/pause"
                { keyboardWidth*0+20,  "Keyboard_MediaNextTrack" },    //Corsair K70 MK2 "next track"
                { keyboardWidth*3+1,   "Keyboard_A" },    //a
                { keyboardWidth*4+6,   "Keyboard_B" },    //b
                { keyboardWidth*4+4,   "Keyboard_C" },    //c
                { keyboardWidth*3+3,   "Keyboard_D" },    //d
                { keyboardWidth*2+3,   "Keyboard_E" },    //e
                { keyboardWidth*3+4,   "Keyboard_F" },    //f
                { keyboardWidth*3+5,   "Keyboard_G" },    //g
                { keyboardWidth*3+6,   "Keyboard_H" },    //h
                { keyboardWidth*2+8,   "Keyboard_I" },    //i
                { keyboardWidth*3+7,   "Keyboard_J" },    //j
                { keyboardWidth*3+8,   "Keyboard_K" },    //k
                { keyboardWidth*3+9,   "Keyboard_L" },    //l
                { keyboardWidth*4+8,   "Keyboard_M" },    //m
                { keyboardWidth*4+7,   "Keyboard_N" },    //n
                { keyboardWidth*2+9,   "Keyboard_O" },    //o
                { keyboardWidth*2+10,  "Keyboard_P" },    //p
                { keyboardWidth*2+1,   "Keyboard_Q" },    //q
                { keyboardWidth*2+4,   "Keyboard_R" },    //r
                { keyboardWidth*3+2,   "Keyboard_S" },    //s
                { keyboardWidth*2+5,   "Keyboard_T" },    //t
                { keyboardWidth*2+7,   "Keyboard_U" },    //u
                { keyboardWidth*4+5,   "Keyboard_V" },    //v
                { keyboardWidth*2+2,   "Keyboard_W" },    //w
                { keyboardWidth*4+3,   "Keyboard_X" },    //x
                { keyboardWidth*2+6,   "Keyboard_Y" },    //y
                { keyboardWidth*4+2,   "Keyboard_Z" },    //z
                { keyboardWidth*1+1,   "Keyboard_1" },    //1
                { keyboardWidth*1+2,   "Keyboard_2" },    //2
                { keyboardWidth*1+3,   "Keyboard_3" },    //3
                { keyboardWidth*1+4,   "Keyboard_4" },    //4
                { keyboardWidth*1+5,   "Keyboard_5" },    //5
                { keyboardWidth*1+6,   "Keyboard_6" },    //6
                { keyboardWidth*1+7,   "Keyboard_7" },    //7
                { keyboardWidth*1+8,   "Keyboard_8" },    //8
                { keyboardWidth*1+9,   "Keyboard_9" },    //9
                { keyboardWidth*1+10,  "Keyboard_0" },    //0
                { keyboardWidth*3+13,  "Keyboard_Enter" },    //Enter
                { keyboardWidth*0+0,   "Keyboard_Escape" },    //Esc
                { keyboardWidth*1+13,  "Keyboard_Backspace" },    //Backspace
                { keyboardWidth*2+0,   "Keyboard_Tab" },    //Tab
                { keyboardWidth*5+5,   "Keyboard_Space" },    //Space
                { keyboardWidth*1+11,  "Keyboard_MinusAndUnderscore" },    //-/_
                { keyboardWidth*1+12,  "Keyboard_EqualsAndPlus" },    //+/=
                { keyboardWidth*2+11,  "Keyboard_BracketLeft" },    //[/{
                { keyboardWidth*2+12,  "Keyboard_BracketRight" },    //]/}
                {keyboardWidth*3+12,   "Keyboard_NonUsTilde" },    //US Backslash, UK ~/#
                {keyboardWidth*4+1,    "Keyboard_NonUsBackslash" },    //UK Backslash/|
                { keyboardWidth*3+10,  "Keyboard_SemicolonAndColon" },    //;/:
                { keyboardWidth*3+11,  "Keyboard_ApostropheAndDoubleQuote" },    //'/"
                { keyboardWidth*1+0,   "Keyboard_GraveAccentAndTilde" },    //`/~
                { keyboardWidth*4+9,   "Keyboard_CommaAndLessThan" },    //,/<
                { keyboardWidth*4+10,  "Keyboard_PeriodAndBiggerThan" },    //./>
                { keyboardWidth*4+11,  "Keyboard_SlashAndQuestionMark" },    //?//
                { keyboardWidth*3+0,   "Keyboard_CapsLock" },    //Caps Lock
                { keyboardWidth*0+1,   "Keyboard_F1" },    //F1
                { keyboardWidth*0+2,   "Keyboard_F2" },    //F2
                { keyboardWidth*0+3,   "Keyboard_F3" },    //F3
                { keyboardWidth*0+4,   "Keyboard_F4" },    //F4
                { keyboardWidth*0+5,   "Keyboard_F5" },    //F5
                { keyboardWidth*0+6,   "Keyboard_F6" },    //F6
                { keyboardWidth*0+7,   "Keyboard_F7" },    //F7
                { keyboardWidth*0+8,   "Keyboard_F8" },    //F8
                { keyboardWidth*0+9,   "Keyboard_F9" },    //F9
                { keyboardWidth*0+10,  "Keyboard_F10" },    //F10
                { keyboardWidth*0+11,  "Keyboard_F11" },    //F11
                { keyboardWidth*0+12,  "Keyboard_F12" },    //F12
                { keyboardWidth*0+13,  "Keyboard_PrintScreen" },    //PrtScr
                { keyboardWidth*0+14,  "Keyboard_ScrollLock" },    //Scroll Lock
                { keyboardWidth*0+15,  "Keyboard_PauseBreak" },    //Pause
                { keyboardWidth*1+14,  "Keyboard_Insert" },    //Insert
                { keyboardWidth*1+15,  "Keyboard_Home" },    //Home
                { keyboardWidth*1+16,  "Keyboard_PageUp" },    //PgUp
                { keyboardWidth*2+14,  "Keyboard_Delete" },    //Delete
                { keyboardWidth*2+15,  "Keyboard_End" },    //End
                { keyboardWidth*2+16,  "Keyboard_PageDown" },    //PgDn
                { keyboardWidth*5+17,  "Keyboard_ArrowRight" },    //Right Arrow
                { keyboardWidth*5+15,  "Keyboard_ArrowLeft" },    //Left Arrow
                { keyboardWidth*5+16,  "Keyboard_ArrowDown" },    //Down Arrow
                { keyboardWidth*4+15,  "Keyboard_ArrowUp" },    //Up Arrow
                { keyboardWidth*1+17,  "Keyboard_NumLock" },    //Num Lock
                { keyboardWidth*1+18,  "Keyboard_NumSlash" },    //Numpad /
                { keyboardWidth*1+19,  "Keyboard_NumAsterisk" },    //Numpad *
                { keyboardWidth*1+20,  "Keyboard_NumMinus" },    //Numpad -
                { keyboardWidth*2+20,  "Keyboard_NumPlus" },    //Numpad +
                { keyboardWidth*4+20,  "Keyboard_NumEnter" },    //Numpad Enter
                { keyboardWidth*4+17,  "Keyboard_Num1" },    //Numpad 1/End
                { keyboardWidth*4+18,  "Keyboard_Num2" },    //Numpad 2/Down
                { keyboardWidth*4+19,  "Keyboard_Num3" },    //Numpad 3/PgDn
                { keyboardWidth*3+17,  "Keyboard_Num4" },    //Numpad 4/Left
                { keyboardWidth*3+18,  "Keyboard_Num5" },    //Numpad 5
                { keyboardWidth*3+19,  "Keyboard_Num6" },    //Numpad 6/Right
                { keyboardWidth*2+17,  "Keyboard_Num7" },    //Numpad 7/Home
                { keyboardWidth*2+18,  "Keyboard_Num8" },    //Numpad 8/Up
                { keyboardWidth*2+19,  "Keyboard_Num9" },    //Numpad 9/PgUp
                { keyboardWidth*5+18,  "Keyboard_Num0" },    //Numpad 0/Ins
                { keyboardWidth*5+19,  "Keyboard_NumPeriodAndDelete" },    //Numpad ./Del
                { keyboardWidth*5+0,   "Keyboard_LeftCtrl" },    //Left Ctrl
                { keyboardWidth*4+0,   "Keyboard_LeftShift" },    //Left Shift
                { keyboardWidth*5+2,   "Keyboard_LeftAlt" },    //Left Alt
                { keyboardWidth*5+1,   "Keyboard_LeftGui" },    //Left GUI
                { keyboardWidth*5+14,  "Keyboard_RightCtrl" },    //Right Ctrl
                { keyboardWidth*4+13,  "Keyboard_RightShift" },    //Right Shift
                { keyboardWidth*5+11,  "Keyboard_RightAlt" },    //Right Alt
                { keyboardWidth*5+12,  "Keyboard_RightGui" },    //Right GUI
                { keyboardWidth*5+13,  "Keyboard_Application" },    //Menu
        };
        private static readonly Dictionary<String, int> ledId2Bitmap = bitmap2LedId.ToDictionary(x => x.Value, x => x.Key);

        public override void Start()
        {
            http = new HttpClient();
            url = System.IO.File.ReadAllText(Environment.GetEnvironmentVariable("PROGRAMDATA") + @"\Artemis\webserver.txt");
            Console.WriteLine("url=" + url);
            var endpoints = JArray.Parse(http.GetAsync(url + "/api/plugins/endpoints").Result.Content.ReadAsStringAsync().Result);
            foreach (var ept in endpoints)
            {
                String url = (string)ept["Url"];
                String name = (string)ept["Name"];
                if (name == "GetLeds") getLedsUrl = url;
                if (name == "SetLeds") setLedsUrl = url;
            }
            leds = JArray.Parse(http.PostAsync(getLedsUrl, new StringContent("")).Result.Content.ReadAsStringAsync().Result);
            ledIndex = new Dictionary<int, int>();
            for (var i = 0; i < leds.Count; ++i)
            {
                ledIndex[ledId2Bitmap[(string)leds[i]["LedId"]]] = i;
            }
        }

        public override void Stop()
        {
            http = null;
        }

        public override void SetGameName(string game)
        {
            //NOP
        }

        public override void SetColor(int key, byte r, byte g, byte b)
        {
            if (ledIndex.ContainsKey(key))
            {
                leds[ledIndex[key]]["Color"] = "#" + r.ToString("X2") + g.ToString("X2") + b.ToString("X2");
            }
        }
        public override void ApplyChanges()
        {
            http.PostAsync(setLedsUrl, new StringContent(leds.ToString(Newtonsoft.Json.Formatting.None)));
        }

    }
}
