using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria;
using Victoria.Entities;
using maicy_bot_core.MiscData;

namespace maicy_bot_core.MaicyServices
{
    public class MusicService
    {
        private DiscordSocketClient maicy_client;
        private LavaRestClient lava_rest_client;
        private LavaSocketClient lava_socket_client;
        private LavaPlayer lava_player;

        public MusicService(LavaRestClient lavaRestClient,
            LavaSocketClient lavaSocketClient,
            DiscordSocketClient client)
        {
            maicy_client = client;
            lava_rest_client = lavaRestClient;
            lava_socket_client = lavaSocketClient;
        }

        public Task InitializeAsync()
        {
            maicy_client.Ready += Maicy_client_Ready_async;
            lava_socket_client.Log += Lava_socket_client_Log;
            lava_socket_client.OnTrackFinished += Lava_socket_client_OnTrackFinished;
            return Task.CompletedTask;
        }

        //on song finish
        private async Task Lava_socket_client_OnTrackFinished(
            LavaPlayer player,
            LavaTrack track,
            TrackEndReason reason)
        {
            if (!reason.ShouldPlayNext())
            {
                return;
            }

            if(Gvar.loop_flag is true)
            {
                if (!player.Queue.TryDequeue(out var item)
                || !(item is LavaTrack next_track))
                {
                    if (!lava_player.IsPlaying)
                    {
                        await lava_player.PlayAsync(Gvar.loop_track);
                        await now_async();
                    }

                    foreach (var loop_item in Gvar.list_loop_track)
                    {
                        lava_player.Queue.Enqueue(loop_item);
                    }
                }
                else
                {
                    await player.PlayAsync(next_track);
                    await now_async();
                }
                //await player.PlayAsync(Gvar.loop_track);
                //await now_async();
            }
            else
            {
                if (!player.Queue.TryDequeue(out var item) 
                    || !(item is LavaTrack next_track))
                {
                    await player.TextChannel.SendMessageAsync
                        ("There are no more tracks in the queue.");
                    return;
                }

                await player.PlayAsync(next_track);
                await now_async();
            }
        }

        //player loop check
        public string player_check()
        {
            if (lava_player == null)
            {
                return "There is no track to loop";
            }

            if (Gvar.loop_flag is true)
            {
                Gvar.loop_flag = false;
                return "Loop Off";
            }
            else
            {
                Gvar.loop_track = lava_player.CurrentTrack;
                Gvar.loop_flag = true;
                return "Loop On";
            }
        }

        //join
        public async Task connect_async(SocketVoiceChannel voice_channel, ITextChannel text_channel)
            => await lava_socket_client.ConnectAsync(voice_channel, text_channel);

        //leave
        public async Task leave_async(SocketVoiceChannel voice_channel)
            => await lava_socket_client.DisconnectAsync(voice_channel);

        //play music from youtube
        public async Task play_async(
            string search,
            ulong guild_id,
            SocketVoiceChannel voice_channel,
            ITextChannel channel,
            string voice_channel_name,
            string type)
        {
            if (lava_player == null)
            {
                await connect_async(voice_channel,channel);
                lava_player = lava_socket_client.GetPlayer(guild_id);
                await lava_player.TextChannel.SendMessageAsync($"Successfully connected to {voice_channel_name}");
            }
            else
            {
                lava_player = lava_socket_client.GetPlayer(guild_id);
            }

            SearchResult results = null; //initialize biar seneng
            if (type == "YT")
            {
                results = await lava_rest_client.SearchYouTubeAsync(search);
            }
            else if (type == "SC")
            {
                results = await lava_rest_client.SearchSoundcloudAsync(search);
            }

            if (results.LoadType == LoadType.NoMatches 
                || results.LoadType == LoadType.LoadFailed)
            {
                await lava_player.TextChannel.SendMessageAsync("No matches found.");
            }

            var track = results.Tracks.FirstOrDefault();

            if (lava_player.IsPlaying)
            {
                lava_player.Queue.Enqueue(track);
                Gvar.list_loop_track = lava_player.Queue.Items.ToList();
                await lava_player.TextChannel.SendMessageAsync($"{track.Title} has been added to the queue");
            }
            else
            {
                await lava_player.PlayAsync(track);
                Gvar.loop_track = track;
                await now_async();
            }
        }

        //lyric
        public async Task lyric_async()
        {
            var current_track_title = lava_player.CurrentTrack.Title;
            var lyric = await lava_player.CurrentTrack.FetchLyricsAsync();

            var embed = new EmbedBuilder
            {
                // Embed property can be set within object initializer
                Title = "Lyric",
            };
            // Or with methods
            var ready = embed
                .WithColor(Color.Green)
                .WithDescription(lyric)
                .WithCurrentTimestamp()
                .Build();

            await lava_player.TextChannel.SendMessageAsync(default, default, ready);
        }

        //now
        public async Task now_async()
        {
            if (lava_player == null)
            {
                return;
            }
            var thumbnail = await lava_player.CurrentTrack.FetchThumbnailAsync();

            var current_track_author = lava_player.CurrentTrack.Author;
            var current_track_title = lava_player.CurrentTrack.Title;
            var current_track_length = lava_player.CurrentTrack.Length;
            var current_track_url = lava_player.CurrentTrack.Uri;
            string desc = null;

            if (current_track_url.ToString().ToUpper().Contains("YOUTUBE"))
            {
                desc = "Youtube";
            }
            else if (current_track_url.ToString().ToUpper().Contains("SOUNDCLOUD"))
            {
                desc = "Soundcloud";
            }

            var hour = lava_player.CurrentTrack.Length.Hours;
            var minute = lava_player.CurrentTrack.Length.Minutes;
            var second = lava_player.CurrentTrack.Length.Seconds;
            string s_hour = lava_player.CurrentTrack.Length.Hours.ToString(),
                s_minute = lava_player.CurrentTrack.Length.Minutes.ToString(),
                s_second = lava_player.CurrentTrack.Length.Seconds.ToString();
            if (hour < 10)
            {
                s_hour = "0" + hour.ToString();
            }

            if (minute < 10)
            {
                s_minute = "0" + minute.ToString();
            }

            if (second < 10)
            {
                s_second = "0" + second.ToString();
            }

            var embed = new EmbedBuilder
            {
                Description = $"By : {current_track_author}" +
                              $"\nSource : {desc}"
            };
            var ready = embed.AddField("Duration",
                s_hour + ":" +
                s_minute + ":" +
                s_second)
                .WithAuthor("Now Playing")
                .WithColor(Color.Green)
                .WithTitle(current_track_title)
                .WithUrl(current_track_url.ToString())
                .WithThumbnailUrl(thumbnail)
                .WithCurrentTimestamp()
                .Build();

            await lava_player.TextChannel.SendMessageAsync(default,default,ready);
            return;
        }

        //queue
        public async Task queue_async()
        {
            if (lava_player == null)
            {
                return;
            }

            string queue_string = "";
            int queue_count = 1;
            var queue_list = lava_player.Queue.Items.ToList();

            foreach(var item in queue_list)
            {
                if (item is null)
                {
                    continue;
                }
                var next_track = item as LavaTrack;
                queue_string += $"{queue_count}. " + $"{next_track.Title}\n";
                queue_count++;
            }

            // Or with methods
            var ready = new EmbedBuilder()
                .WithAuthor("Queue")
                .WithDescription(queue_string)
                .WithColor(Color.Green)
                .WithFooter($"There is total {queue_list.Count.ToString()} tracks in the queue")
                .WithCurrentTimestamp()
                .Build();

            await lava_player.TextChannel.SendMessageAsync(default, default, ready);
            return;
        }

        //stop
        public async Task stop_async()
        {
            if (lava_player == null)
            {
                return;
            }
            await lava_player.StopAsync();
        }

        //skip
        public async Task<string> skip_async()
        {
            if (lava_player == null || lava_player.Queue.Count == 0)
            {
                return "Nothing in queue";
            }

            var old_track = lava_player.CurrentTrack;
            await lava_player.SkipAsync();
            await lava_player.TextChannel.SendMessageAsync($"Successfully skipped {old_track.Title}");
            await now_async();
            return " ";
        }

        //volume adjustment
        public async Task<string> set_volume_async(int vol)
        {
            if (lava_player == null)
            {
                return "Player need to be connected to the channel first";
            }

            if (vol < 0 || vol > 150)
            {
                return "Volume must between 0 - 150";
            }

            await lava_player.SetVolumeAsync(vol);
            return $"Volume set to {vol}";
        }

        //pause
        public async Task<string> pause_async()
        {
            if (lava_player == null)
            {
                return "Nothing played at this time.";
            }

            if (lava_player.IsPaused == true)
            {
                return "Track already paused.";
            }

            await lava_player.PauseAsync();
            return "Player Paused.";
        }

        //resume
        public async Task<string> resume_async()
        {
            if (lava_player == null)
            {
                return "Nothing played at this time.";
            }

            if (lava_player.IsPaused != true)
            {
                return "Track still playing.";
            }

            await lava_player.ResumeAsync();
            return "Track resumed.";
        }

        private Task Lava_socket_client_Log(LogMessage Logmessage)
        {
            Console.WriteLine(Logmessage.Message);
            return Task.CompletedTask;
        }

        private async Task Maicy_client_Ready_async()
        {
            await lava_socket_client.StartAsync(maicy_client);
        }
    }
}
