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

        [Command("Join") , Alias("Connect","cn","masok","sokin","masuk")]
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
                await maicy_music_service.connect_async(user.VoiceChannel, Context.Channel as ITextChannel);
                await ReplyAsync($"Successfully connected to {user.VoiceChannel.Name}");
            }
        }

        [Command("Leave"), Alias("dc", "disconnect", "kluar", "keluar", "caw")]
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

        [Command("Play"), Alias("p", "main", "mainken")]
        public async Task Play([Remainder]string search)
        {
            var user = Context.User as SocketGuildUser;

            if (user.VoiceChannel is null)
            {
                await ReplyAsync("You need to connect to a voice channel.");
                return;
            }

            await maicy_music_service.play_async(
                search,
                Context.Guild.Id,
                user.VoiceChannel,
                Context.Channel as ITextChannel,
                user.VoiceChannel.Name, "YT");
        }

        [Command("Sc"), Alias("soundcloud", "sonclod", "sonklod")]
        public async Task Soundcloud([Remainder]string search)
        {
            var user = Context.User as SocketGuildUser;

            if (user.VoiceChannel is null)
            {
                await ReplyAsync("You need to connect to a voice channel.");
                return;
            }

            await maicy_music_service.play_async(
                search,
                Context.Guild.Id,
                user.VoiceChannel,
                Context.Channel as ITextChannel,
                user.VoiceChannel.Name, "SC");
        }

        [Command("Clear"), Alias("cl", "stop", "bersihken")]
        public async Task Stop()
        {
            string reply_msg = await maicy_music_service.clear_not_async();
            await ReplyAsync(reply_msg);
        }

        [Command("Pause") , Alias("ps", "henti", "hentiken", "hentikeun", "sebat", "sebatdl", "sebatdulu")]
        public async Task Pause()
        {
            string reply_msg = await maicy_music_service.pause_async();
            await ReplyAsync(reply_msg);
        }

        [Command("Resume"), Alias("r", "lanjut", "lanjutken", "lanjutkeun", "gasken", "gaskeun", "skuy")]
        public async Task Resume()
        {
            string reply_msg = await maicy_music_service.resume_async();
            await ReplyAsync(reply_msg);
        }

        [Command("Skip"), Alias("s", "next")]
        public async Task Skip()
        {
            var result = await maicy_music_service.skip_async();
            await ReplyAsync(result);
        }

        [Command("Volume"), Alias("v", "vol", "suara")]
        public async Task Volume(int vol)
        {
            string reply_msg = await maicy_music_service.set_volume_async(vol);
            await ReplyAsync(reply_msg);
        }

        [Command("Loop"), Alias("lp", "repeat", "rp")]
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

        [Command("Now"), Alias("n","np", "nowplaying", "sekarang")]
        public async Task Now()
        {
            string reply_msg = await maicy_music_service.now_async();
            await ReplyAsync(reply_msg);
        }

        [Command("Lyric"), Alias("ly")]
        public async Task Lyric()
        {

            string reply_msg = await maicy_music_service.lyric_async();
            await ReplyAsync(reply_msg);
        }

        [Command("Queue"), Alias("q", "antrean")]
        public async Task Queue()
        {
            string reply_msg = await maicy_music_service.queue_async();
            await ReplyAsync(reply_msg);
        }

        [Command("Shuffle"), Alias("sh", "acak", "everydayimshuffling")]
        public async Task Shuffle()
        {
            string reply_msg = maicy_music_service.shuffle_async();
            await ReplyAsync(reply_msg);
        }
    }
}
