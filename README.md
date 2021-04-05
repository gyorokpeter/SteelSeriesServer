This project implements a HTTP server that allows [GameSense SDK](https://github.com/SteelSeries/gamesense-sdk/) enabled games to connect to and forward keyboard lighting events to [Aurora](https://github.com/antonpup/Aurora/), [OpenRGB](https://gitlab.com/CalcProgrammer1/OpenRGB) and [Artemis](https://github.com/Artemis-RGB/Artemis).

**Usage:**
- Make sure SteelSeries Game Engine is not running. It may interfere with this server.
- After doing the necessary configuration, start SteelSeriesServer before launching your game.

***For Aurora***
- In Aurora, make sure the SteelSeries device client is not running (in General Settings -> Device Manager, it should say "SteelSeries: Not initialized")
- Add a profile for the game and add a "Wrapper Lights" layer.
- Run SteelSeriesServer before starting the game.

***For OpenRGB***
- Start the SDK server before starting SteelSeriesServer.

***For Artemis***
- You need to manually add the Remote Control module (unless it is already part of Artemis since this writing). Find "RemoteControlModule.zip" on the Artemis discord (in the support channel) and extract it into %PROGRAMDATA%\Artemis\plugins\Artemis.Plugins.Modules.RemoteControlModule .

Tested with the following games:
- [Cardaclysm](https://store.steampowered.com/app/1252710/Cardaclysm/)
- [Factorio](https://store.steampowered.com/app/427520/Factorio/) - also supports Razer but apparently the Razer wrapper for Aurora is glitchy so not all the effects show up. Remove Aurora's Razer patch first.
