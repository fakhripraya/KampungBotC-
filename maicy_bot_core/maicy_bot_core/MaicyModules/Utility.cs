using Discord;
using Discord.Commands;
using Discord.WebSocket;
using maicy_bot_core.MaicyServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace maicy_bot_core.MaicyModule
{
    public class Utility : ModuleBase<SocketCommandContext>
    {
        private UtilityService maicy_utility_service;

        public Utility(UtilityService utility_service)
        {
            maicy_utility_service = utility_service;
        }

        [Command("Help"), Alias("h", "helep", "tolong")]
        public async Task Help()
        {
            var embed = new EmbedBuilder
            {
                // Embed property can be set within object initializer
                Title = "Help",
            };
            // Or with methods
            var ready = embed
                .WithColor(Color.Green)
                .WithDescription(
                "MUSIC COMMAND\n\n\n" +
                "`Join` [Join current user channel]\n" + 
                "`Leave` [Leave channel]\n" + 
                "\n" +
                "`Play` [Play song/playlist from Youtube]\n" + 
                "`Soundcloud` [Play song from Soundcloud]\n" + 
                "`spotify` [Play playlist from spotify]\n" +
                "[ex: (prefix)spotify {url} ]\n" + 
                "\n" +
                "`Resume` [Resume current playback]\n" + 
                "`Pause` [Pause current playback]\n" + 
                "\n" +
                "`Clear` [Stop and Clear all tracks]\n" + 
                "`Skip` [Skip current playback]\n" + 
                "\n" +
                "`Volume` [Set playback Volumes]\n" + 
                "`Loop` [Loop tracks]\n" + 
                "\n" + 
                "`Now` [Get current track info]\n" + 
                "`Lyrics` [Fetch current track lyrics]\n" + 
                "\n" +
                "`Queue` [Get tracks queue info]\n" + 
                "`Page` [Get tracks queue info]\n" +
                "[based by user page input]\n" +
                "[ex : (prefix)page 1]\n" + 
                "`Shuffle` [Shuffle the queue randomly]\n" + 
                "\n\n" +
                "UTILITY\n\n" +
                "`Help` [summon this command]")
                .WithFooter("Contact : PakPres#8360 for any feedback")
                .WithCurrentTimestamp()
                .Build();

            await ReplyAsync(default, default, ready);
        }

        [Command("Send")]
        public async Task Send(string guild_name,string channel_name,string message)
        {
            var user = Context.User as SocketGuildUser;

            await maicy_utility_service.send_async(user,guild_name,channel_name,message);
        }

        [Command("Kick")]
        public async Task Kick(IGuildUser userAccount, string reason)
        {
            var user = Context.User as SocketGuildUser;
            var role = (user as IGuildUser).Guild.Roles.FirstOrDefault(x => x.Name == "Marshall");
            if (user.GuildPermissions.KickMembers)
            {
                await userAccount.KickAsync(reason);
                await Context.Channel.SendMessageAsync($"The user `{userAccount}` has been kicked, for {reason}");
            }
            else
            {
                await Context.Channel.SendMessageAsync("No permissions for kicking a user.");
            }
        }
    }
}
