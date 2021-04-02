using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using Newtonsoft.Json.Linq;

namespace SteelSeriesServer
{
    internal class AuroraSender : Sender
    {
        private NamedPipeClientStream pipeClient;
        private readonly JObject pipeMsg;

        public AuroraSender()
        {
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
        }

        override public void Start()
        {
            pipeClient =
                    new NamedPipeClientStream(".", "Aurora\\server",
                        PipeDirection.Out, PipeOptions.None,
                        TokenImpersonationLevel.Anonymous);
            pipeClient.Connect(1000);
        }

        override public void Stop()
        {
            pipeClient.Close();
        }

        override public void SetGameName(string game)
        {
            pipeMsg["provider"]["name"] = game.ToLower() + ".exe";
        }

        override public void SetColor(int key, byte r, byte g, byte b)
        {
            var colorInt = (r << 16) | (g << 8) | b;
            pipeMsg["bitmap"][key] = colorInt;
        }

        override public void ApplyChanges()
        {
            var str = pipeMsg.ToString(Newtonsoft.Json.Formatting.None) + "\n";
            var data = Encoding.ASCII.GetBytes(str);
            pipeClient.Write(data, 0, data.Length);
            pipeClient.Flush();
        }

    }
}