using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SteelSeriesServer
{
    struct GameMetadata
    {
        public String game;
        public String game_display_name;
        public String developer;
        public int icon_color_id;
        public int deinitialize_timer_length_ms;
    }

    struct Color
    {
        public bool isGradient;
        public int red, green, blue;
        public int red2, green2, blue2;

        internal static Color FromJObject(JObject p)
        {
            if (p["gradient"] != null)
            {
                var g = p["gradient"];
                var h = g["hundred"];
                var z = g["zero"];
                return new Color
                {
                    isGradient = true,
                    red = (int)z["red"],
                    green = (int)z["green"],
                    blue = (int)z["blue"],
                    red2 = (int)h["red"],
                    green2 = (int)h["green"],
                    blue2 = (int)h["blue"]
                };
            }
            else
            {
                return new Color { red = (int)p["red"], green = (int)p["green"], blue = (int)p["blue"] };
            }
        }
        internal JObject ToJObject()
        {
            if (isGradient)
            {
                return new JObject
                {
                    ["gradient"] = new JObject
                    {
                        ["zero"] = new JObject { ["red"] = red, ["green"] = green, ["blue"] = blue },
                        ["hundred"] = new JObject { ["red"] = red2, ["green"] = green2, ["blue"] = blue2 },
                    }
                };
            }
            else
            {
                return new JObject { ["red"] = red, ["green"] = green, ["blue"] = blue };
            }
        }
    }
    struct Rate
    {
        public int frequency, repeat_limit;

        internal static Rate FromJObject(JObject p)
        {
            return new Rate { frequency = (int)p["frequency"], repeat_limit = (int)p["repeat_limit"] };
        }
    }

    struct GameEventHandler
    {
        public String game;
        public enum Mode { Color, ContextColor, Count, Percent, Bitmap, PartialBitmap };
        public Mode mode;
        public enum DeviceType { RgbPerKeyZones, RgbZonedDevice };
        public DeviceType deviceType;
        public enum Zone { None, All, NumberKeys, FunctionKeys, One, Two, Three, Four, Five, Six, Seven, Eight };
        public Zone zone;
        public List<int> customZoneKeys;
        private int[] customZoneKeyLocs;
        public Color color;
        public Rate rate;
        public String contextFrameKey;
        public static readonly Dictionary<String, DeviceType> deviceTypes = new Dictionary<string, DeviceType> {
                { "rgb-per-key-zones", DeviceType.RgbPerKeyZones },
                { "rgb-zoned-device", DeviceType.RgbZonedDevice },
            };
        public static readonly Dictionary<DeviceType, String> deviceTypesR = deviceTypes.ToDictionary(x => x.Value, x => x.Key);
        public static readonly Dictionary<String, Mode> modes = new Dictionary<string, Mode> {
                { "color", Mode.Color },
                { "context-color", Mode.ContextColor },
                { "count", Mode.Count },
                { "percent", Mode.Percent },
                { "bitmap", Mode.Bitmap },
                { "partial-bitmap", Mode.PartialBitmap }
            };
        public static readonly Dictionary<Mode, String> modesR = modes.ToDictionary(x => x.Value, x => x.Key);
        public static readonly Dictionary<String, Zone> zones = new Dictionary<string, Zone> {
                { "", Zone.None },
                { "all", Zone.All },
                { "number-keys",Zone.NumberKeys },
                { "function-keys",Zone.FunctionKeys },
                { "one",Zone.One },
                { "two",Zone.Two },
                { "three",Zone.Three },
                { "four",Zone.Four },
                { "five",Zone.Five },
                { "six",Zone.Six },
                { "seven",Zone.Seven },
                { "eight",Zone.Eight },
            };
        const int keyboardWidth = Sender.keyboardWidth;
        public static readonly Dictionary<Zone, String> zonesR = zones.ToDictionary(x => x.Value, x => x.Key);
        public static readonly Dictionary<int, int> hid2Bitmap = new Dictionary<int, int> {
                { 4,   keyboardWidth*3+1  },    //a
                { 5,   keyboardWidth*4+6  },    //b
                { 6,   keyboardWidth*4+4  },    //c
                { 7,   keyboardWidth*3+3  },    //d
                { 8,   keyboardWidth*2+3  },    //e
                { 9,   keyboardWidth*3+4  },    //f
                { 10,  keyboardWidth*3+5  },    //g
                { 11,  keyboardWidth*3+6  },    //h
                { 12,  keyboardWidth*2+8  },    //i
                { 13,  keyboardWidth*3+7  },    //j
                { 14,  keyboardWidth*3+8  },    //k
                { 15,  keyboardWidth*3+9  },    //l
                { 16,  keyboardWidth*4+8  },    //m
                { 17,  keyboardWidth*4+7  },    //n
                { 18,  keyboardWidth*2+9  },    //o
                { 19,  keyboardWidth*2+10 },    //p
                { 20,  keyboardWidth*2+1  },    //q
                { 21,  keyboardWidth*2+4  },    //r
                { 22,  keyboardWidth*3+2  },    //s
                { 23,  keyboardWidth*2+5  },    //t
                { 24,  keyboardWidth*2+7  },    //u
                { 25,  keyboardWidth*4+5  },    //v
                { 26,  keyboardWidth*2+2  },    //w
                { 27,  keyboardWidth*4+3  },    //x
                { 28,  keyboardWidth*2+6  },    //y
                { 29,  keyboardWidth*4+2  },    //z
                { 30,  keyboardWidth*1+1  },    //1
                { 31,  keyboardWidth*1+2  },    //2
                { 32,  keyboardWidth*1+3  },    //3
                { 33,  keyboardWidth*1+4  },    //4
                { 34,  keyboardWidth*1+5  },    //5
                { 35,  keyboardWidth*1+6  },    //6
                { 36,  keyboardWidth*1+7  },    //7
                { 37,  keyboardWidth*1+8  },    //8
                { 38,  keyboardWidth*1+9  },    //9
                { 39,  keyboardWidth*1+10 },    //0
                { 40,  keyboardWidth*3+13 },    //Enter
                { 41,  keyboardWidth*0+0  },    //Esc
                { 42,  keyboardWidth*1+13 },    //Backspace
                { 43,  keyboardWidth*2+0  },    //Tab
                { 44,  keyboardWidth*5+5  },    //Space
                { 45,  keyboardWidth*1+11 },    //-/_
                { 46,  keyboardWidth*1+12 },    //+/=
                { 47,  keyboardWidth*2+11 },    //[/{
                { 48,  keyboardWidth*2+12 },    //]/}
                { 49,  keyboardWidth*3+12 },    //US Backslash, UK ~/#
                { 50,  keyboardWidth*4+1  },    //UK Backslash/|
                { 51,  keyboardWidth*3+10 },    //;/:
                { 52,  keyboardWidth*3+11 },    //'/"
                { 53,  keyboardWidth*1+0  },    //`/~
                { 54,  keyboardWidth*4+9  },    //,/<
                { 55,  keyboardWidth*4+10 },    //./>
                { 56,  keyboardWidth*4+11 },    //?//
                { 57,  keyboardWidth*3+0  },    //Caps Lock
                { 58,  keyboardWidth*0+1  },    //F1
                { 59,  keyboardWidth*0+2  },    //F2
                { 60,  keyboardWidth*0+3  },    //F3
                { 61,  keyboardWidth*0+4  },    //F4
                { 62,  keyboardWidth*0+5  },    //F5
                { 63,  keyboardWidth*0+6  },    //F6
                { 64,  keyboardWidth*0+7  },    //F7
                { 65,  keyboardWidth*0+8  },    //F8
                { 66,  keyboardWidth*0+9  },    //F9
                { 67,  keyboardWidth*0+10 },    //F10
                { 68,  keyboardWidth*0+11 },    //F11
                { 69,  keyboardWidth*0+12 },    //F12
                { 70,  keyboardWidth*0+13 },    //PrtScr
                { 71,  keyboardWidth*0+14 },    //Scroll Lock
                { 72,  keyboardWidth*0+15 },    //Pause
                { 73,  keyboardWidth*1+14 },    //Insert
                { 74,  keyboardWidth*1+15 },    //Home
                { 75,  keyboardWidth*1+16 },    //PgUp
                { 76,  keyboardWidth*2+14 },    //Delete
                { 77,  keyboardWidth*2+15 },    //End
                { 78,  keyboardWidth*2+16 },    //PgDn
                { 79,  keyboardWidth*5+17 },    //Right Arrow
                { 80,  keyboardWidth*5+15 },    //Left Arrow
                { 81,  keyboardWidth*5+16 },    //Down Arrow
                { 82,  keyboardWidth*4+15 },    //Up Arrow
                { 83,  keyboardWidth*1+17 },    //Num Lock
                { 84,  keyboardWidth*1+18 },    //Numpad /
                { 85,  keyboardWidth*1+19 },    //Numpad *
                { 86,  keyboardWidth*1+20 },    //Numpad -
                { 87,  keyboardWidth*2+20 },    //Numpad +
                { 88,  keyboardWidth*4+20 },    //Numpad Enter
                { 89,  keyboardWidth*4+17 },    //Numpad 1/End
                { 90,  keyboardWidth*4+18 },    //Numpad 2/Down
                { 91,  keyboardWidth*4+19 },    //Numpad 3/PgDn
                { 92,  keyboardWidth*3+17 },    //Numpad 4/Left
                { 93,  keyboardWidth*3+18 },    //Numpad 5
                { 94,  keyboardWidth*3+19 },    //Numpad 6/Right
                { 95,  keyboardWidth*2+17 },    //Numpad 7/Home
                { 96,  keyboardWidth*2+18 },    //Numpad 8/Up
                { 97,  keyboardWidth*2+19 },    //Numpad 9/PgUp
                { 98,  keyboardWidth*5+18 },    //Numpad 0/Ins
                { 99,  keyboardWidth*5+19 },    //Numpad ./Del
                { 224, keyboardWidth*5+0  },    //Left Ctrl
                { 225, keyboardWidth*4+0  },    //Left Shift
                { 226, keyboardWidth*5+2  },    //Left Alt
                { 227, keyboardWidth*5+1  },    //Left GUI
                { 228, keyboardWidth*5+14 },    //Right Ctrl
                { 229, keyboardWidth*4+13 },    //Right Shift
                { 230, keyboardWidth*5+11 },    //Right Alt
                { 231, keyboardWidth*5+12 },    //Right GUI
                { 240, keyboardWidth*5+13 },    //Menu???
                //unmapped:
                //keyboardWidth*2+13
                //keyboardWidth*3+14
                //keyboardWidth*3+15
                //keyboardWidth*3+16
                //keyboardWidth*3+20
                //keyboardWidth*4+12
                //keyboardWidth*4+14
                //keyboardWidth*4+16
                //keyboardWidth*5+3
                //keyboardWidth*5+4
                //keyboardWidth*5+6
                //keyboardWidth*5+7
                //keyboardWidth*5+8
                //keyboardWidth*5+9
                //keyboardWidth*5+10
                //keyboardWidth*5+20
            };
        public static readonly Dictionary<Zone, int[]> zoneKeys = new Dictionary<Zone, int[]>
            {
                {Zone.None, new int[] { } },
                {Zone.All, Enumerable.Range(0, 126).ToArray() },
                {Zone.NumberKeys, Enumerable.Range(22, 10).ToArray() },
                {Zone.FunctionKeys, Enumerable.Range(1, 12).ToArray() },
            };
        public JObject ToJObject()
        {
            var result = new JObject
            {
                ["mode"] = modesR[mode],
                ["device-type"] = deviceTypesR[deviceType],
                ["zone"] = zonesR[zone],
                ["color"] = color.ToJObject(),
                ["rate"] = JObject.FromObject(rate)
            };
            if (customZoneKeys != null)
            {
                result["custom-zone-keys"] = JArray.FromObject(customZoneKeys);
            }
            return result;
        }

        private int EvalPct(int i, int max, int val, ref int red, ref int green, ref int blue)
        {
            double lowerBound = 100 * ((double)i) / max;
            double upperBound = 100 * ((double)(i + 1)) / max;
            if (val <= lowerBound)
            {
                red = green = blue = 0;
            }
            else if (val < upperBound)
            {
                double intensity = (val - lowerBound) / (upperBound - lowerBound);
                red = (int)(red * intensity);
                green = (int)(green * intensity);
                blue = (int)(blue * intensity);
            }
            return (red << 16) | (green << 8) | blue;
        }
        internal void Execute(int val, Sender sender)
        {
            if (deviceType == DeviceType.RgbZonedDevice) return;    //only keyboard supported for now
            int red, green, blue;
            if (color.isGradient)
            {
                red = (color.red * (100 - val) + color.red2 * val) / 100;
                green = (color.green * (100 - val) + color.green2 * val) / 100;
                blue = (color.blue * (100 - val) + color.blue2 * val) / 100;
            }
            else
            {
                red = val > 0 ? color.red : 0;
                green = val > 0 ? color.green : 0;
                blue = val > 0 ? color.blue : 0;
            }
            int[] keys = GetAffectedKeys();
            for (var i = 0; i < keys.Length; ++i)
            {
                if (mode == Mode.Color)
                {
                    //color not changed
                }
                else if (mode == Mode.ContextColor)
                {
                    //contextColor should be called via ExecuteWithFrame
                }
                else if (mode == Mode.Count)
                {
                    if (i >= val) red = green = blue = 0;
                }
                else if (mode == Mode.Percent)
                {
                    EvalPct(i, keys.Length, val, ref red, ref green, ref blue);
                }
                else
                {
                    throw new Exception("unknown mode");
                }
                sender.SetColor(keys[i], (byte)red, (byte)green, (byte)blue);
            }
            sender.ApplyChanges();
        }

        private int[] GetAffectedKeys()
        {
            if (customZoneKeyLocs != null) return customZoneKeyLocs;
            return zoneKeys[zone];
        }

        internal void ExecuteWithFrame(JObject frame, IEventMaskProvider maskp, Sender sender)
        {
            if (deviceType == DeviceType.RgbZonedDevice) return;    //only keyboard supported for now
            if (mode == Mode.ContextColor)
            {
                color = Color.FromJObject((JObject)frame[contextFrameKey]);
                Execute(1, sender);
            }
            else if (mode == Mode.Bitmap)
            {
                var bmp = (JArray)frame["bitmap"];
                for (int i = 0; i < 126; ++i)
                {
                    var color = (JArray)bmp[i + i / 21];
                    sender.SetColor(i, (byte)color[0], (byte)color[1], (byte)color[2]);
                }
                sender.ApplyChanges();
            }
            else if (mode == Mode.PartialBitmap)
            {
                int[] mask = maskp.GetEventMask(game, frame["excluded-events"].ToObject<String[]>());
                var bmp = (JArray)frame["bitmap"];
                for (int i = 0; i < 126; ++i)
                {
                    if (mask[i] == 0)
                    {
                        var color = (JArray)bmp[i + i / 21];
                        sender.SetColor(i, (byte)color[0], (byte)color[1], (byte)color[2]);
                    }
                }
                sender.ApplyChanges();
            }
            else
            {
                throw new Exception("unknown mode");
            }
        }

        internal void SetCustomZoneKeys(List<int> keys)
        {
            customZoneKeys = keys;
            customZoneKeyLocs = new int[keys.Count];
            for (int i = 0; i < keys.Count; ++i)
                customZoneKeyLocs[i] = hid2Bitmap[keys[i]];
        }

        internal void UpdateMask(int[] result)
        {
            foreach (int i in GetAffectedKeys())
                result[i] = 1;
        }
    }

    internal interface IEventMaskProvider
    {
        int[] GetEventMask(string game, string[] vs);
    }

    struct Event
    {
        public String game;
        public String evtName;
        public int min_value;
        public int max_value;
        public int icon_id;
        public List<GameEventHandler> handlers;
        public bool value_optional;
        public Object data_fields;

        internal void Execute(int val, Sender sender)
        {
            if (val < min_value) val = min_value;
            if (val > max_value) val = max_value;
            foreach (var h in handlers)
            {
                h.Execute(val, sender);
            }
        }

        internal void ExecuteWithFrame(JObject frame, IEventMaskProvider maskp, Sender sender)
        {
            foreach (var h in handlers)
            {
                h.ExecuteWithFrame(frame, maskp, sender);
            }
        }

        internal void UpdateMask(int[] mask)
        {
            foreach (var handler in handlers)
            {
                handler.UpdateMask(mask);
            }
        }
    }

    class Game
    {
        public GameMetadata meta;
        public Dictionary<String, Event> events;
        public Game(GameMetadata meta_)
        {
            meta = meta_;
            events = new Dictionary<String, Event>();
        }
        public int[] GetEventMask(String[] maskEvents)
        {
            var result = new int[126];
            foreach (var evt in maskEvents)
            {
                events[evt].UpdateMask(result);
            }
            return result;
        }
    }

    class NetworkReader
    {
        bool closed = false;
        readonly TcpClient client;
        readonly NetworkStream stream;
        byte[] buffer;
        int bufStart = 0;
        int bufEnd = 0;
        public void SetTimeout(int timeout)
        {
            stream.ReadTimeout = timeout;
        }
        public NetworkReader(TcpClient client_)
        {
            client = client_;
            stream = client.GetStream();
            stream.ReadTimeout = 15000;
            buffer = new byte[65536];
        }
        private void CheckForMoreData()
        {
            try
            {
                var bytes = stream.Read(buffer, bufEnd, buffer.Length - bufEnd);
                Console.WriteLine("bytes=" + bytes);
                if (bytes == 0) closed = true;
                bufEnd += bytes;
                if (bufEnd == buffer.Length)
                {
                    var newBuffer = new byte[65536];
                    Array.Copy(buffer, bufStart, newBuffer, 0, bufEnd - bufStart);
                    buffer = newBuffer;
                    bufEnd -= bufStart;
                    bufStart = 0;
                }
            }
            catch (System.IO.IOException)
            {
                Console.WriteLine("timeout");
                closed = true;
                client.Close();
            }
        }
        public String ReadLine()
        {
            while (true)
            {
                for (int i = bufStart; i < bufEnd - 1; ++i)
                {
                    if (buffer[i] == '\r' && buffer[i + 1] == '\n')
                    {
                        var result = Encoding.UTF8.GetString(buffer, bufStart, i - bufStart);
                        bufStart = i + 2;
                        if (bufStart >= bufEnd)
                        {
                            bufStart = bufEnd = 0;
                        }
                        return result;
                    }
                }
                CheckForMoreData();
                if (closed) return null;
            }
        }
        public String Read(int contentLength)
        {
            while (true)
            {
                if (bufEnd - bufStart >= contentLength)
                {
                    var result = Encoding.UTF8.GetString(buffer, bufStart, contentLength);
                    bufStart += contentLength;
                    if (bufStart >= bufEnd)
                    {
                        bufStart = bufEnd = 0;
                    }
                    return result;
                }
                CheckForMoreData();
                if (closed) return null;
            }
        }
    }


    class SteelSeriesServer : IEventMaskProvider
    {
        private readonly Sender sender;
        private readonly TcpListener listener;
        private readonly Dictionary<string, Game> games;

        public int[] GetEventMask(string game, string[] maskEvents)
        {
            return games[game].GetEventMask(maskEvents);
        }

        public SteelSeriesServer(Sender sender)
        {
            this.sender = sender;
            listener = new TcpListener(IPAddress.Any, 0);
            listener.Start();
            var ssConfig = new JObject
            {
                ["address"] = "127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port,
            };
            File.WriteAllText(Environment.GetEnvironmentVariable("PROGRAMDATA") + @"\SteelSeries\SteelSeries Engine 3\coreProps.json", ssConfig.ToString(Newtonsoft.Json.Formatting.None));
            games = new Dictionary<String, Game>();
        }

        public void Run()
        {
            while (true)
            {
                Console.WriteLine("Waiting for client\n");
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Client connected\n");
                sender.Start();
                StreamWriter sWriter = new StreamWriter(client.GetStream(), Encoding.ASCII);
                StreamReader sReader = new StreamReader(client.GetStream(), Encoding.ASCII);
                var action = "";
                var expectContinue = false;
                var readingReq = false;
                var length = 0;
                var nr = new NetworkReader(client);
                while (client.Connected)
                {
                    String sData;
                    if (readingReq)
                    {
                        sData = nr.Read(length);
                    }
                    else
                    {
                        sData = nr.ReadLine();
                    }
                    if (sData == null) break;
                    if (readingReq)
                    {
                        JObject response = null;
                        readingReq = false;
                        if (action == "/game_metadata")
                        {
                            JObject req = JObject.Parse(sData);
                            GameMetadata meta = new GameMetadata
                            {
                                game = (string)req["game"],
                                game_display_name = (string)req["game_display_name"],
                                developer = req.ContainsKey("developer") ? (string)req["developer"] : "",
                                icon_color_id = req.ContainsKey("icon_color_id") ? (int)req["icon_color_id"] : 0,
                                deinitialize_timer_length_ms = req.ContainsKey("deinitialize_timer_length_ms") ? (int)req["deinitialize_timer_length_ms"] : 15000
                            };
                            nr.SetTimeout(meta.deinitialize_timer_length_ms);
                            //Console.WriteLine(req);
                            Console.WriteLine("Game metadata: " + meta.game);
                            if (games.ContainsKey(meta.game))
                            {
                                games[meta.game].meta = meta;
                            }
                            else
                            {
                                games[meta.game] = new Game(meta);
                            }

                            response = new JObject
                            {
                                ["game_metadata"] = JObject.FromObject(meta)
                            };
                        }
                        else if (action == "/bind_game_event")
                        {
                            JObject req = JObject.Parse(sData);
                            JObject evtResp = new JObject();
                            Event ev = new Event
                            {
                                game = (string)req["game"],
                                evtName = (string)req["event"],
                                handlers = new List<GameEventHandler>(),
                            };
                            if (req.ContainsKey("icon_id"))
                            {
                                ev.icon_id = (int)req["icon_id"];
                            }
                            if (req.ContainsKey("min_value"))
                            {
                                ev.min_value = (int)req["min_value"];
                            }
                            else
                            {
                                ev.min_value = 0;
                            }
                            if (req.ContainsKey("max_value"))
                            {
                                ev.max_value = (int)req["max_value"];
                            }
                            else
                            {
                                ev.max_value = 100;
                            }
                            foreach (var h in req["handlers"])
                            {
                                var handler = new GameEventHandler
                                {
                                    game = ev.game,
                                    deviceType = GameEventHandler.deviceTypes[(string)h["device-type"]],
                                    mode = GameEventHandler.modes[(string)h["mode"]],
                                };
                                if (h["color"] != null)
                                {
                                    handler.color = Color.FromJObject((JObject)h["color"]);
                                }
                                if (h["custom-zone-keys"] != null)
                                {
                                    handler.SetCustomZoneKeys(h["custom-zone-keys"].ToObject<List<int>>());
                                }
                                if (h["zone"] != null)
                                {
                                    handler.zone = GameEventHandler.zones[(string)h["zone"]];
                                }
                                if (h["rate"] != null)
                                {
                                    handler.rate = Rate.FromJObject((JObject)h["rate"]);
                                }
                                if (h["context-frame-key"] != null)
                                {
                                    handler.contextFrameKey = (string)h["context-frame-key"];
                                }
                                ev.handlers.Add(handler);
                            }
                            if (games.ContainsKey(ev.game))
                            {
                                games[ev.game].events[ev.evtName] = ev;
                                evtResp["game"] = ev.game;
                                evtResp["event"] = ev.evtName;
                                evtResp["icon_id"] = ev.icon_id;
                                evtResp["min_value"] = ev.min_value;
                                evtResp["max_value"] = ev.max_value;
                                evtResp["handlers"] = JArray.FromObject(ev.handlers.Select(x => x.ToJObject()));
                                if (ev.data_fields != null)
                                {
                                    evtResp["data_fields"] = JObject.FromObject(ev.data_fields);
                                }
                                response = new JObject { ["game_event_binding"] = evtResp };
                            }
                            else
                            {
                                response = new JObject { ["error"] = "unregistered game: " + ev.game };
                            }
                        }
                        else if (action == "/game_event")
                        {
                            JObject req = JObject.Parse(sData);
                            JObject evtResp = new JObject();
                            String game = (string)req["game"];
                            String evtName = (string)req["event"];
                            int val = 0;
                            bool hasValue = req["data"]["value"] != null;
                            JObject frame = (JObject)req["data"]["frame"];
                            if (hasValue) val = (int)req["data"]["value"];
                            if (!games.ContainsKey(game))
                            {
                                response = new JObject { ["error"] = "unregistered game: " + game };
                            }
                            else
                            {
                                sender.SetGameName(game);
                                if (hasValue)
                                    games[game].events[evtName].Execute(val, sender);
                                else
                                    games[game].events[evtName].ExecuteWithFrame(frame, this, sender);

                                evtResp["game"] = game;
                                evtResp["event"] = evtName;
                                if (hasValue)
                                {
                                    evtResp["data"] = new JObject { ["value"] = val };
                                    evtResp["Data"] = evtResp["data"].ToString(Newtonsoft.Json.Formatting.None);
                                }
                                response = new JObject { ["game_event"] = evtResp };
                            }
                        }
                        else if (action == "/game_heartbeat")
                        {
                            JObject req = JObject.Parse(sData);
                            String game = (string)req["game"];
                            response = new JObject { ["game_heartbeat"] = new JObject { ["game"] = game } };
                        }
                        else
                        {
                            Console.WriteLine("Unknown action: " + action);
                            Console.WriteLine(sData);
                        }
                        if (response != null)
                        {
                            var responseStr = response.ToString(Newtonsoft.Json.Formatting.None);
                            Console.WriteLine(responseStr);
                            //Console.WriteLine("Sending response");
                            sWriter.WriteLine("HTTP/1.1 200 OK");
                            sWriter.WriteLine("Access-Control-Allow-Origin: *");
                            sWriter.WriteLine("Content-Length: " + responseStr.Length);
                            sWriter.WriteLine("Content-Type: text/html; charset=utf-8");
                            sWriter.WriteLine("Server: web.go");
                            sWriter.WriteLine("");
                            sWriter.Write(responseStr);
                            sWriter.Flush();
                            //if (!keepAlive) client.Close();
                        }
                    }
                    else if (sData.Contains("POST "))
                    {
                        var parts = sData.Split(' ');
                        action = parts[1];
                        //Console.WriteLine("Action: " + action);
                    }
                    else if (sData.Contains("Content-Length:"))
                    {
                        length = Int32.Parse(sData.Split(' ')[1]);
                    }
                    else if (sData == "Expect: 100-continue")
                    {
                        expectContinue = true;
                    } /*else if (sData == "Connection: keep-alive")
                    {
                        keepAlive = true;
                    }*/
                    else if (sData == "")
                    {
                        if (expectContinue)
                        {
                            //Console.WriteLine("Sending Continue");
                            sWriter.WriteLine("HTTP/1.1 100 Continue");
                            sWriter.WriteLine("");
                            sWriter.Flush();
                            expectContinue = false;
                            readingReq = true;
                        }
                        else
                        {
                            //Console.WriteLine("Need to read request body");
                            readingReq = true;
                        }
                    }
                }
                sender.Stop();
            }
        }
    }
}
