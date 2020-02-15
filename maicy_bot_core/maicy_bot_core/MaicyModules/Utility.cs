using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace maicy_bot_core.MaicyModule
{
    public class Utility : ModuleBase<SocketCommandContext>
    {
        [Command("Help")]
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
                "`Join` [Join current user channel] , `Leave` [Leave channel]\n\n" +
                "`Play` [Play song from Youtube] , `Sc` [Play song from Soundcloud]\n\n" +
                "`Clear` [Stop and Clear all tracks] , `Pause` [Pause current playback]\n\n" +
                "`Resume` [Resume current playback], `Skip` [Skip current playback]\n\n" +
                "`Volume` [Set playback Volumes] , `Loop` [Loop tracks]\n\n" +
                "`Now` [Get current track info] , `Shuffle` [Shuffle the queue randomly]\n\n" +
                "`Lyrics` [Fetch current track lyrics], `Queue` [Get tracks queue info]\n\n\n" +
                "UTILITY\n\n" +
                "`Help` [summon this command]")
                .WithFooter("Contact : <@292649640619278348> for any feedback")
                .WithCurrentTimestamp()
                .Build();

            await ReplyAsync(default, default, ready);
        }
    }
}
