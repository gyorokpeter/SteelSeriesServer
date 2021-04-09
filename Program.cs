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
using EmbedIO;
using Newtonsoft.Json.Linq;
using OpenRGB.NET;

namespace SteelSeriesServer
{

    class Program
    {
        static int Main(string[] args)
        {

            int senderType = -1;
            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i] == "-sender")
                {
                    ++i;
                    if (i >= args.Length)
                    {
                        Console.Error.WriteLine("no value for -sender parameter");
                        return 1;
                    }
                    if (args[i] == "aurora")
                    {
                        senderType = 1;
                    }
                    else if (args[i] == "openrgb")
                    {
                        senderType = 2;
                    }
                    else if (args[i] == "artemis")
                    {
                        senderType = 3;
                    }
                    else
                    {
                        Console.Error.WriteLine("unknown value for -sender parameter: " + args[i]);
                        return 1;
                    }
                }
            }
            if (senderType == -1)
            {
                Console.Write("Choose sender type (1=Aurora, 2=OpenRGB, 3=Artemis): ");
                senderType = int.Parse(Console.ReadLine());
                if (senderType < 0 || senderType > 3)
                {
                    Console.Error.WriteLine("invalid sender type");
                    return 1;
                }
            }
            Sender sender;
            switch (senderType)
            {
                case 1:
                    sender = new AuroraSender();
                    break;
                case 2:
                    sender = new OpenRGBSender();
                    break;
                case 3:
                    sender = new ArtemisSender();
                    break;
                default:
                    Console.Error.WriteLine("this should never happen, senderType= "+senderType);
                    return 1;
            }

            var server = new SteelSeriesServer(sender);
            server.Run();
            return 0;
        }
    }
}
