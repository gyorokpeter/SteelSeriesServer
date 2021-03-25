using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace SteelSeriesServer
{

    class Program
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
                    return new Color { isGradient = true, red = (int)z["red"], green = (int)z["green"], blue = (int)z["blue"],
                        red2 = (int)h["red"], green2 = (int)h["green"], blue2 = (int)h["blue"]
                    };
                } else {
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
        struct EventHandler
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
            public static readonly Dictionary<Zone, String> zonesR = zones.ToDictionary(x => x.Value, x => x.Key);
            public static readonly Dictionary<int, int> hid2Bitmap = new Dictionary<int, int> {
                { 4, 64 },    //a
                { 5, 90 },    //b
                { 6, 88 },    //c
                { 7, 66 },    //d
                { 8, 45 },    //e
                { 9, 67 },    //f
                { 10, 68 },   //g
                { 11, 69 },   //h
                { 12, 50 },   //i
                { 13, 70 },   //j
                { 14, 71 },   //k
                { 15, 72 },   //l
                { 16, 92 },   //m
                { 17, 91 },   //n
                { 18, 51 },   //o
                { 19, 52 },   //p
                { 20, 43 },   //q
                { 21, 46 },   //r
                { 22, 65 },   //s
                { 23, 47 },   //t
                { 24, 49 },   //u
                { 25, 89 },   //v
                { 26, 44 },   //w
                { 27, 87 },   //x
                { 28, 48 },   //y
                { 29, 86 },   //z
                { 30, 22 },   //1
                { 31, 23 },   //2
                { 32, 24 },   //3
                { 33, 25 },   //4
                { 34, 26 },   //5
                { 35, 27 },   //6
                { 36, 28 },   //7
                { 37, 29 },   //8
                { 38, 30 },   //9
                { 39, 31 },   //0
                { 40, 76 },   //Enter
                { 41, 0 },    //Esc
                { 42, 34 },   //Backspace
                { 43, 42 },   //Tab
                { 44, 110 },  //Space
                { 45, 32 },   //-/_
                { 46, 33 },   //+/=
                { 47, 53 },   //[/{
                { 48, 54 },   //]/}
                { 49, 75 },   //Backslash
                { 50, 85 },   //|/\
                { 51, 73 },   //;/:
                { 52, 74 },   //'/"
                { 53, 21 },   //`/~
                { 55, 94 },   //./>
                { 56, 95 },   //?//
                { 57, 63 },   //Caps Lock
                { 58, 1 },    //F1
                { 59, 2 },    //F2
                { 60, 3 },    //F3
                { 61, 4 },    //F4
                { 62, 5 },    //F5
                { 63, 6 },    //F6
                { 64, 7 },    //F7
                { 65, 8 },    //F8
                { 66, 9 },    //F9
                { 67, 10 },   //F10
                { 68, 11 },   //F11
                { 69, 12 },   //F12
                { 70, 13 },   //PrtScr
                { 71, 14 },   //Scroll Lock
                { 72, 15 },   //Pause
                { 73, 35 },   //Insert
                { 74, 36 },   //Home
                { 75, 37 },   //PgUp
                { 76, 56 },   //Delete
                { 77, 57 },   //End
                { 78, 58 },   //PgDn
                { 79, 122 },  //Right Arrow
                { 80, 120 },  //Left Arrow
                { 81, 121 },  //Down Arrow
                { 82, 99 },   //Up Arrow
                { 83, 38 },   //Num Lock
                { 84, 39 },   //Numpad /
                { 85, 40 },   //Numpad *
                { 86, 41 },   //Numpad -
                { 87, 62 },   //Numpad +
                { 88, 104 },  //Numpad Enter
                { 89, 101 },  //Numpad 1/End
                { 90, 102 },  //Numpad 2/Down
                { 91, 103 },  //Numpad 3/PgDn
                { 92, 80 },   //Numpad 4/Left
                { 93, 81 },   //Numpad 5
                { 94, 82 },   //Numpad 6/Right
                { 95, 59 },   //Numpad 7/Home
                { 96, 60 },   //Numpad 8/Up
                { 97, 61 },   //Numpad 9/PgUp
                { 98, 123 },  //Numpad 0/Ins
                { 99, 124 },  //Numpad ./Del
                { 224, 105 }, //Left Ctrl
                { 225, 84 },  //Left Shift
                { 226, 107 }, //Left Alt
                { 227, 106 }, //Left GUI
                { 228, 119 }, //Right Ctrl
                { 229, 97 },  //Right Shift
                { 230, 116 }, //Right Alt
                { 231, 117 }, //Right GUI
                { 240, 118 }, //Right Menu???
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
                if (customZoneKeys != null) {
                    result["custom-zone-keys"] = JArray.FromObject(customZoneKeys);
                }
                return result;
            }

            private int EvalPct(int i, int max, int val, int red, int green, int blue)
            {
                double lowerBound = 100 * ((double)i) / max;
                double upperBound = 100 * ((double)(i+1)) / max;
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
            internal void Execute(int val)
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
                int colorInt = (red << 16) | (green << 8) | blue;
                int[] keys = GetAffectedKeys();
                for (var i = 0; i < keys.Length; ++i)
                {
                    if (mode == Mode.Color)
                    {
                        //color not changed
                    } else if (mode == Mode.ContextColor)
                    {
                        //contextColor should be called via ExecuteWithFrame
                    }
                    else if (mode == Mode.Count)
                    {
                        if (i >= val) colorInt = 0;
                    } else if (mode == Mode.Percent)
                    {
                        colorInt = EvalPct(i, keys.Length, val, red, green, blue);
                    } else
                    {
                        throw new Exception("unknown mode");
                    }
                    pipeMsg["bitmap"][keys[i]] = colorInt;
                }
                SendMessage();
            }

            private int[] GetAffectedKeys()
            {
                if (customZoneKeyLocs != null) return customZoneKeyLocs;
                return zoneKeys[zone];
            }

            internal void ExecuteWithFrame(JObject frame)
            {
                if (deviceType == DeviceType.RgbZonedDevice) return;    //only keyboard supported for now
                if (mode == Mode.ContextColor) {
                    color = Color.FromJObject((JObject)frame[contextFrameKey]);
                    Execute(1);
                } else if (mode == Mode.Bitmap)
                {
                    var bmp = (JArray)frame["bitmap"];
                    for (int i = 0; i < 126; ++i)
                    {
                        var color = (JArray)bmp[i + i / 21];
                        int colorInt = ((int)color[0] << 16) | ((int)color[1] << 8) | ((int)color[2]);
                        pipeMsg["bitmap"][i] = colorInt;
                    }
                    SendMessage();
                }
                else if (mode == Mode.PartialBitmap)
                {
                    int[] mask = games[game].GetEventMask(frame["excluded-events"].ToObject<String[]>());
                    var bmp = (JArray)frame["bitmap"];
                    for (int i=0; i<126; ++i)
                    {
                        if (mask[i] == 0)
                        {
                            var color = (JArray)bmp[i + i / 21];
                            int colorInt = ((int)color[0] << 16) | ((int)color[1] << 8) | ((int)color[2]);
                            pipeMsg["bitmap"][i] = colorInt;
                        }
                    }
                    SendMessage();
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
        struct Event
        {
            public String game;
            public String evtName;
            public int min_value;
            public int max_value;
            public int icon_id;
            public List<EventHandler> handlers;
            public bool value_optional;
            public Object data_fields;

            internal void Execute(int val)
            {
                if (val < min_value) val = min_value;
                if (val > max_value) val = max_value;
                foreach (var h in handlers)
                {
                    h.Execute(val);
                }
            }

            internal void ExecuteWithFrame(JObject frame)
            {
                foreach (var h in handlers)
                {
                    h.ExecuteWithFrame(frame);
                }
            }

            internal void UpdateMask(int[] mask)
            {
                foreach (var handler in handlers) {
                    handler.UpdateMask(mask);
                }
            }
        }

        class Game
        {
            public GameMetadata meta;
            public Dictionary<String, Event> events;
            public Game(GameMetadata meta_) {
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

        static Dictionary<String, Game> games;
        static JObject pipeMsg;
        static NamedPipeClientStream pipeClient;

        static void SendMessage()
        {
            var str = pipeMsg.ToString(Newtonsoft.Json.Formatting.None) + "\n";
            var data = Encoding.ASCII.GetBytes(str);
            Console.WriteLine(str);
            pipeClient.Write(data, 0, data.Length);
            pipeClient.Flush();
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
                } catch(System.IO.IOException)
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

        static void Main(string[] _)
        {
            games = new Dictionary<String, Game>();
            var provider = new JObject
            {
                ["name"] = "SteelSeriesServer.exe",
                ["appid"] = 0
            };
            var cmdData = new JObject
            {
                ["custom_mode"] = 0,
                ["effect_type"] = "CHROMA_CUSTOM"
            };
            var extraKeys = new JObject
            {
                ["logo"] = 0,
                ["G1"] = 0,
                ["G2"] = 0,
                ["G3"] = 0,
                ["G4"] = 0,
                ["G5"] = 0,
                ["peripheral"] = 0,
                ["mousepad0"] = 0,
                ["mousepad1"] = 0,
                ["mousepad2"] = 0,
                ["mousepad3"] = 0,
                ["mousepad4"] = 0,
                ["mousepad5"] = 0,
                ["mousepad6"] = 0,
                ["mousepad7"] = 0,
                ["mousepad8"] = 0,
                ["mousepad9"] = 0,
                ["mousepad10"] = 0,
                ["mousepad11"] = 0,
                ["mousepad12"] = 0,
                ["mousepad13"] = 0,
                ["mousepad14"] = 0
            };
            pipeMsg = new JObject
            {
                ["provider"] = provider,
                ["command"] = "CreateKeyboardEffect",
                ["command_data"] = cmdData,
                ["bitmap"] = JArray.FromObject(new int[126]),
                ["extra_keys"] = extraKeys,
            };

            var listener = new TcpListener(IPAddress.Any, 0);
            listener.Start();
            var ssConfig = new JObject
            {
                ["address"] = "127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port,
            };
            File.WriteAllText(Environment.GetEnvironmentVariable("PROGRAMDATA")+@"\SteelSeries\SteelSeries Engine 3\coreProps.json",ssConfig.ToString(Newtonsoft.Json.Formatting.None));
            while (true)
            {
                Console.WriteLine("Waiting for client\n");
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Client connected\n");
                pipeClient =
                        new NamedPipeClientStream(".", "Aurora\\server",
                            PipeDirection.Out, PipeOptions.None,
                            TokenImpersonationLevel.Anonymous);
                pipeClient.Connect();
                StreamWriter sWriter = new StreamWriter(client.GetStream(), Encoding.ASCII);
                StreamReader sReader = new StreamReader(client.GetStream(), Encoding.ASCII);
                var action = "";
                var expectContinue = false;
                var readingReq = false;
                var keepAlive = false;
                var length = 0;
                var nr = new NetworkReader(client);
                while (client.Connected)
                {
                    String sData;
                    if (readingReq)
                    {
                        sData = nr.Read(length);
                    } else
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
                                handlers = new List<EventHandler>(),
                            };
                            if (req.ContainsKey("icon_id"))
                            {
                                ev.icon_id = (int)req["icon_id"];
                            }
                            if (req.ContainsKey("min_value"))
                            {
                                ev.min_value = (int)req["min_value"];
                            } else
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
                                var handler = new EventHandler
                                {
                                    game = ev.game,
                                    deviceType = EventHandler.deviceTypes[(string)h["device-type"]],
                                    mode = EventHandler.modes[(string)h["mode"]],
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
                                    handler.zone = EventHandler.zones[(string)h["zone"]];
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
                                pipeMsg["provider"]["name"] = game.ToLower()+".exe";
                                if (hasValue)
                                    games[game].events[evtName].Execute(val);
                                else
                                    games[game].events[evtName].ExecuteWithFrame(frame);

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
                    } else if (sData.Contains("Content-Length:")) {
                        length = Int32.Parse(sData.Split(' ')[1]);
                    }
                    else if (sData == "Expect: 100-continue")
                    {
                        expectContinue = true;
                    } else if (sData == "Connection: keep-alive")
                    {
                        keepAlive = true;
                    }
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
                        } else
                        {
                            //Console.WriteLine("Need to read request body");
                            readingReq = true;
                        }
                    }
                }
                pipeClient.Close();
            }
        }
    }
}
