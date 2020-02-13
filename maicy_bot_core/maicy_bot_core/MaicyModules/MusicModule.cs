using Discord;
using Discord.Commands;
using Discord.WebSocket;
using maicy_bot_core.MaicyServices;
using maicy_bot_core.MiscData;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Victoria;

namespace maicy_bot_core.MaicyModule
{
    public class MusicModule : ModuleBase<SocketCommandContext>
    {
        private MusicService maicy_music_service;

        public MusicModule(MusicService music_service)
        {
            maicy_music_service = music_service;
        }

        [Command("Join")]
        public async Task Join()
        {
            var user = Context.User as SocketGuildUser;

            if (user.VoiceChannel is null)
            {
                await ReplyAsync("You need to connect to a voice channel.");
                return;
            }
            else
            {
                await maicy_music_service.connect_async(user.VoiceChannel , Context.Channel as ITextChannel);
                await ReplyAsync($"Successfully connected to {user.VoiceChannel.Name}");
            }
        }

        [Command("Leave")]
        public async Task Leave()
        {
            var user = Context.User as SocketGuildUser;

            if (user.VoiceChannel is null)
            {
                await ReplyAsync("Please join the voice channel the bot is in to make it leave.");
            }
            else
            {
                await maicy_music_service.leave_async(user.VoiceChannel);
                await ReplyAsync($"Successfully disconnected from {user.VoiceChannel.Name}");
            }
        }

        [Command("Play")]
        public async Task Play([Remainder]string search)
        {
            var user = Context.User as SocketGuildUser;

            if (user.VoiceChannel is null)
            {
                await ReplyAsync("You need to connect to a voice channel.");
                return;
            }

            //await maicy_music_service.connect_async(user.VoiceChannel, Context.Channel as ITextChannel);
            //await ReplyAsync($"Successfully connected to {user.VoiceChannel.Name}");

            await maicy_music_service.play_async(
                search,
                Context.Guild.Id,
                user.VoiceChannel,
                Context.Channel as ITextChannel,
                user.VoiceChannel.Name,"YT");

            //await ReplyAsync(result);
            //await maicy_music_service.now_async();
        }

        [Command("Sc")]
        public async Task Soundcloud([Remainder]string search)
        {
            var user = Context.User as SocketGuildUser;

            if (user.VoiceChannel is null)
            {
                await ReplyAsync("You need to connect to a voice channel.");
                return;
            }

            //await maicy_music_service.connect_async(user.VoiceChannel, Context.Channel as ITextChannel);
            //await ReplyAsync($"Successfully connected to {user.VoiceChannel.Name}");

            await maicy_music_service.play_async(
                search,
                Context.Guild.Id,
                user.VoiceChannel,
                Context.Channel as ITextChannel,
                user.VoiceChannel.Name,"SC");

            //await ReplyAsync(result);
            //await maicy_music_service.now_async();
        }

        [Command("Stop")]
        public async Task Stop()
        {
            await maicy_music_service.stop_async();
            await ReplyAsync("Player Stopped.");
        }

        [Command("Pause")]
        public async Task Pause()
        {
            string reply_msg = await maicy_music_service.pause_async();
            await ReplyAsync(reply_msg);
        }

        [Command("Resume")]
        public async Task Resume()
        {
            string reply_msg = await maicy_music_service.resume_async();
            await ReplyAsync(reply_msg);
        }

        [Command("Skip")]
        public async Task Skip()
        {
            var result = await maicy_music_service.skip_async();
            await ReplyAsync(result);
        }

        [Command("Volume")]
        public async Task Volume(int vol)
        {
            string reply_msg = await maicy_music_service.set_volume_async(vol);
            await ReplyAsync(reply_msg);
        }

        [Command("Loop")]
        public async Task Loop()
        {
            var user = Context.User as SocketGuildUser;

            if (user.VoiceChannel is null)
            {
                await ReplyAsync("You need to connect to a voice channel.");
                return;
            }

            string reply_msg = maicy_music_service.player_check();
            await ReplyAsync(reply_msg);
        }

        [Command("Now")]
        public async Task Now()
        {

            await maicy_music_service.now_async();
        }

        [Command("Lyric")]
        public async Task Lyric()
        {

            await maicy_music_service.lyric_async();
        }

        [Command("Queue")]
        public async Task Queue()
        {
            await maicy_music_service.queue_async();
        }
    }
}
