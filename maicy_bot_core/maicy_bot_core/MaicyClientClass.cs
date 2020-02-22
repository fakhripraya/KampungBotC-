using Discord.Commands;
using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Victoria;
using maicy_bot_core.MaicyServices;
using System.Threading;
using System.Linq;

namespace maicy_bot_core
{
    public class MaicyClientClass
    {
        private DiscordSocketClient maicy_client;
        private CommandService maicy_cmd_serv;
        private IServiceProvider maicy_services;
        private Timer _timer;

        //create your list of statuses and an indexer to keep track
        private readonly List<string> _statusList = new List<string>() {
            "Strvrains",
            "Genji",
            "Gazer",
            "Birra",
            "Dreqqy",
            "Ugau",
            "Edgar",
            "Sauce",
            "MekMek",
            "King",
            "Plasti",
            "Jarvis",
            "Maicy",
            "Pakpres" };
        private int _statusIndex = 0;

        public MaicyClientClass(DiscordSocketClient client = null, CommandService cmd = null)
        {
            maicy_client = client ?? new DiscordSocketClient(new DiscordSocketConfig {
                AlwaysDownloadUsers = true,
                MessageCacheSize = 50,
                LogLevel = LogSeverity.Debug
            });

            maicy_cmd_serv = cmd ?? new CommandService(new CommandServiceConfig{
                LogLevel = LogSeverity.Verbose,
                CaseSensitiveCommands = false
            });
        }

        public async Task InitializeAsync()
        {
            //Login
            //await maicy_client.LoginAsync(TokenType.Bot, "NjczNDcyMTU2MDMzNjEzODU2.Xja3Qg.9PwtgPvClJozYpJdAQMTN9PJnxk"); //maicy
            /*await maicy_client.LoginAsync(TokenType.Bot, "NjczNzU3MDU1NDIwNTk2MjY1.XjerXw.Tz9NWPWo9bY5UjqRaXgOi-942Jo");*/ //euy
            await maicy_client.LoginAsync(TokenType.Bot, "Njc0NjUyMTE4NDcyNDU4MjQw.XjrsxQ.0ByxKE0yvJd17oWz_CBe373wTz8"); //eh
            //await maicy_client.LoginAsync(TokenType.Bot, "Njc3NTQyNDIwNjY5NTMwMTEy.XkVxLg.tat8vRmwYxh4oSaNnQBPgdy7Uso");   //cave cafe

            //Startin the bot
            await maicy_client.StartAsync();

            maicy_client.Ready += Maicy_client_Ready;
            maicy_client.Log += Maicy_client_Log;
            maicy_services = SetupServices();

            //MaicyCommandClass
            var cmd_handler = new MaicyCommandClass(maicy_client, maicy_cmd_serv, maicy_services);
            await cmd_handler.InitializeAsync();

            //MusicService
            await maicy_services.GetRequiredService<MusicService>().InitializeAsync();

            //bot live forever
            await Task.Delay(-1);
        }

        private Task Maicy_client_Ready()
        {
            _timer = new Timer(async _ =>
            {//any code in here will run periodically       
                await maicy_client.SetGameAsync(_statusList.ElementAtOrDefault(_statusIndex), type: ActivityType.Watching); //set activity to current index position
                _statusIndex = _statusIndex + 1 == _statusList.Count ? 0 : _statusIndex + 1; //increment index position, restart if end of list
            },
            null,
            TimeSpan.FromSeconds(1), //time to wait before executing the timer for the first time (set first status)
            TimeSpan.FromSeconds(10)); //time to wait before executing the timer again (set new status - repeats indifinitely every 10 seconds)
            return Task.CompletedTask;
        }

        private Task Maicy_client_Log(LogMessage log_message)
        {
            Console.WriteLine(log_message.Message);
            return Task.CompletedTask;
        }

        //Dependency Injection
        private IServiceProvider SetupServices()
            => new ServiceCollection()
            .AddSingleton(maicy_client)
            .AddSingleton(maicy_cmd_serv)
            .AddSingleton<LavaRestClient>()
            .AddSingleton<LavaSocketClient>()
            .AddSingleton<MusicService>()
            .AddSingleton<UtilityService>()
            .BuildServiceProvider();
    }
}