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
using YoutubeExplode;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Models;

namespace maicy_bot_core.MaicyServices
{
    public class MusicService
    {
        private DiscordSocketClient maicy_client;
        private LavaRestClient lava_rest_client;
        private LavaSocketClient lava_socket_client;
        private LavaPlayer lava_player;
        private static SpotifyWebAPI _spotify;

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
            lava_socket_client.OnTrackException += Lava_socket_client_OnTrackException;
            maicy_client.UserVoiceStateUpdated += Maicy_client_UserVoiceStateUpdated;
            maicy_client.Disconnected += Maicy_client_Disconnected;
            return Task.CompletedTask;
        }

        private Task Maicy_client_UserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
        {
            if (Gvar.current_client_channel == null)
            {
                return Task.CompletedTask;
            }
            if (Gvar.current_client_channel.Users.Count() == 1)
            {
                if (!Gvar.current_client_channel.Users.FirstOrDefault().IsBot)
                {
                    return Task.CompletedTask;
                }

                clear_all_loop();
                lava_socket_client.DisconnectAsync(Gvar.current_client_channel as IVoiceChannel);
                Gvar.current_client_channel = null;
                lava_player.TextChannel.SendMessageAsync
                            ("All player left, Trying to disconnect.");
                lava_player = null;
            }
            return Task.CompletedTask;
        }

        private Task Lava_socket_client_OnTrackException(LavaPlayer player, LavaTrack track, string ex_msg)
        {
            clear_all_loop();
            lava_socket_client.DisconnectAsync(player.VoiceChannel);
            lava_player.TextChannel.SendMessageAsync
                            ($"Track Error, {ex_msg} Disconnecting.");
            lava_player = null;
            return Task.CompletedTask;
        }

        private Task Maicy_client_Disconnected(Exception ex)
        {
            Console.WriteLine(ex.Message);
            clear_all_loop();
            lava_player = null;
            return Task.CompletedTask;
        }

        //clear all
        public void clear_all_loop()
        {
            Gvar.loop_track = null;
            Gvar.list_loop_track = null;
            Gvar.loop_flag = false;
            return;
        }

        //on song finish
        private async Task Lava_socket_client_OnTrackFinished(
            LavaPlayer player,
            LavaTrack track,
            TrackEndReason reason)
        {
            try
            {
                if (lava_player.IsPlaying)
                {
                    return;
                }

                if (!lava_player.IsPaused && lava_player.IsPlaying)
                {
                    return;
                }

                if (Gvar.loop_flag is true)
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
                }
                else
                {
                    if (!player.Queue.TryDequeue(out var item)
                        || !(item is LavaTrack next_track))
                    {
                        await player.TextChannel.SendMessageAsync
                            ("There are no more tracks in the queue.");
                        clear_all_loop();
                        lava_player = null;
                        await lava_socket_client.DisconnectAsync(player.VoiceChannel);
                        return;
                    }

                    await player.PlayAsync(next_track);
                    await now_async();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                clear_all_loop();
                lava_player = null;
                await lava_socket_client.DisconnectAsync(player.VoiceChannel);
            }
        }

        //player loop check
        public string player_check()
        {
            if (lava_player == null)
            {
                return "There are no track to loop";
            }

            if (Gvar.loop_flag is true)
            {
                clear_all_loop();
                return "Loop Off";
            }
            else
            {
                Gvar.loop_track = lava_player.CurrentTrack;
                Gvar.list_loop_track = lava_player.Queue.Items.ToList();
                Gvar.loop_flag = true;
                return "Loop On";
            }
        }

        //join
        public async Task connect_async(SocketVoiceChannel voice_channel, ITextChannel text_channel)
            => await lava_socket_client.ConnectAsync(voice_channel, text_channel);

        //leave
        public async Task leave_async(SocketVoiceChannel voice_channel)
        {
            clear_all_loop();
            lava_player = null;
            await lava_socket_client.DisconnectAsync(voice_channel);
        }

        //play music from youtube
        public async Task play_async(
            string search,
            ulong guild_id,
            SocketVoiceChannel voice_channel,
            ITextChannel channel,
            string voice_channel_name,
            string type,
            ulong user_guild_id)
        {
            try
            {
                //674652118472458240 jukbok id
                //673472156033613856 maicy id
                //673757055420596265 euy

                Gvar.current_client_channel = maicy_client.GetChannel(voice_channel.Id);
                var lava_client_id = Gvar.current_client_channel
                    .Users
                    .Select(x => x)
                    .Where(x => x.IsBot == true && x.Id == 674652118472458240)
                    .FirstOrDefault(); //input your bot id here

                if (lava_client_id == null)
                {
                    await connect_async(voice_channel, channel);
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
                    if (search.Contains("playlist?list="))
                    {
                        int _chars = search.Count() - 34;

                        var playlist_id = search.Substring(_chars, 34);
                        var client = new YoutubeClient();
                        var playlist = await client.GetPlaylistAsync(playlist_id);

                        if (playlist == null)
                        {
                            await lava_player.TextChannel.SendMessageAsync("Can't find playlist");
                            return;
                        }

                        await lava_player.TextChannel.SendMessageAsync($"Adding {playlist.Author} playlist to the queue. Please wait.");

                        results = await lava_rest_client.SearchTracksAsync(search);

                        if (results.LoadType == LoadType.NoMatches
                        || results.LoadType == LoadType.LoadFailed)
                        {
                            await lava_player.TextChannel.SendMessageAsync("Load type error.");
                            return;
                        }

                        Gvar.playlist_load_flag = true;

                        foreach (var item in results.Tracks)
                        {
                            if (lava_player.IsPlaying)
                            {
                                lava_player.Queue.Enqueue(item);

                                if (Gvar.list_loop_track != null)
                                {
                                    Gvar.list_loop_track.Add(item);
                                }
                                else
                                {
                                    Gvar.list_loop_track = lava_player.Queue.Items.ToList();
                                }
                            }
                            else
                            {
                                await lava_player.PlayAsync(item);
                                Gvar.loop_track = item;
                            }
                        }

                        await now_async();
                        await lava_player.TextChannel.SendMessageAsync($"{playlist.Author} playlist has been added to the queue");
                        Gvar.playlist_load_flag = false;
                        return;
                    }
                    else
                    {
                        results = await lava_rest_client.SearchYouTubeAsync(search);
                    }
                }
                else if (type == "SC")
                {
                    results = await lava_rest_client.SearchSoundcloudAsync(search);
                }
                else if (type == "SP")
                {
                    string[] collection = search.Split('/');

                    string[] spotify_id = collection[collection.Count() - 1].Split("?si=");

                    FullPlaylist sp_playlist = _spotify.GetPlaylist(spotify_id[0], fields: "", market: "");

                    if (sp_playlist.Tracks.Total > 200)
                    {
                        await lava_player.TextChannel.SendMessageAsync("Cannot add a playlist with more than 200 songs in it");
                        return;
                    }

                    var temp_msg = await lava_player.TextChannel.SendMessageAsync($"Adding {sp_playlist.Owner.DisplayName} playlist to the queue. Please wait.");
                    Gvar.playlist_load_flag = true;

                    int load_count = 0;
                    var lastMessageID = temp_msg.Id;

                    foreach (var sp_item in sp_playlist.Tracks.Items)
                    {
                        if (load_count % 5 == 0)
                        {
                            if (load_count > 0)
                            {
                                var del = await lava_player.TextChannel.GetMessageAsync(lastMessageID);
                                await temp_msg.DeleteAsync();
                                temp_msg = await lava_player.TextChannel.SendMessageAsync($"{load_count} / {sp_playlist.Tracks.Items.Count()} tracks loaded.");
                                lastMessageID = temp_msg.Id;
                            }
                        }

                        results = await lava_rest_client.SearchYouTubeAsync(sp_item.Track.Artists.FirstOrDefault().Name + " " + sp_item.Track.Name);

                        if (results.LoadType == LoadType.NoMatches
                            || results.LoadType == LoadType.LoadFailed)
                        {
                            load_count++;
                            continue;
                        }

                        if (lava_player.IsPlaying)
                        {
                            lava_player.Queue.Enqueue(results.Tracks.FirstOrDefault());

                            if (Gvar.list_loop_track != null)
                            {
                                Gvar.list_loop_track.Add(results.Tracks.FirstOrDefault());
                            }
                            else
                            {
                                Gvar.list_loop_track = lava_player.Queue.Items.ToList();
                            }
                        }
                        else
                        {
                            await lava_player.PlayAsync(results.Tracks.FirstOrDefault());
                            Gvar.loop_track = results.Tracks.FirstOrDefault();
                        }
                        load_count++;
                    }
                    
                    await now_async();
                    await lava_player.TextChannel.SendMessageAsync($"{sp_playlist.Owner.DisplayName} playlist has been added to the queue");
                    Gvar.playlist_load_flag = false;
                    return;
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

                    if (Gvar.list_loop_track != null)
                    {
                        Gvar.list_loop_track.Add(track);
                    }
                    else
                    {
                        Gvar.list_loop_track = lava_player.Queue.Items.ToList();
                    }
                    await lava_player.TextChannel.SendMessageAsync($"{track.Title} has been added to the queue");
                }
                else
                {
                    await lava_player.PlayAsync(track);
                    Gvar.loop_track = track;
                    await now_async();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        //lyric
        public async Task<string> lyric_async()
        {
            try
            {
                if (lava_player == null)
                {
                    return "There are no track playing at this time.";
                }

                if (!lava_player.IsPlaying)
                {
                    return "There are no track playing at this time.";
                }

                var lyric = await lava_player.CurrentTrack.FetchLyricsAsync();

                if (lyric == "" || lyric == null)
                {
                    return "Can't find lyric.";
                }

                var embed = new EmbedBuilder
                {
                    Title = $"By : {lava_player.CurrentTrack.Author}\n" +
                            $"Title : {lava_player.CurrentTrack.Title}"
                };
                var ready = embed
                    .WithColor(Color.Green)
                    .WithDescription(lyric)
                    .WithCurrentTimestamp()
                    .Build();

                await lava_player.TextChannel.SendMessageAsync(default, default, ready);
                return "";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return "";
        }

        //now
        public async Task now_async()
        {
            try
            {
                var return_embed = new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithTitle("There are no track playing at this time.")
                    .WithCurrentTimestamp()
                    .Build();

                if (lava_player == null)
                {
                    await lava_player.TextChannel.SendMessageAsync(default, default, return_embed);
                }

                if (!lava_player.IsPlaying)
                {
                    await lava_player.TextChannel.SendMessageAsync(default, default, return_embed);
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
                var current_hour = lava_player.CurrentTrack.Position.Hours;
                var current_minute = lava_player.CurrentTrack.Position.Minutes;
                var current_second = lava_player.CurrentTrack.Position.Seconds;

                var hour = lava_player.CurrentTrack.Length.Hours;
                var minute = lava_player.CurrentTrack.Length.Minutes;
                var second = lava_player.CurrentTrack.Length.Seconds;

                string s_hour = lava_player.CurrentTrack.Length.Hours.ToString(),
                    s_minute = lava_player.CurrentTrack.Length.Minutes.ToString(),
                    s_second = lava_player.CurrentTrack.Length.Seconds.ToString();

                string s_hour_current = lava_player.CurrentTrack.Position.Hours.ToString(),
                    s_minute_current = lava_player.CurrentTrack.Position.Minutes.ToString(),
                    s_second_current = lava_player.CurrentTrack.Position.Seconds.ToString();

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

                if (current_hour < 10)
                {
                    s_hour_current = "0" + current_hour.ToString();
                }

                if (current_minute < 10)
                {
                    s_minute_current = "0" + current_minute.ToString();
                }

                if (current_second < 10)
                {
                    s_second_current = "0" + current_second.ToString();
                }

                var embed = new EmbedBuilder
                {
                    Description = $"By : {current_track_author}" +
                                  $"\nSource : {desc}" +
                                  $"\nVolume : {lava_player.CurrentVolume}"
                };
                var ready = embed.AddField("Duration",
                    s_hour_current + ":" +
                    s_minute_current + ":" +
                    s_second_current +
                    " / " +
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

                await lava_player.TextChannel.SendMessageAsync(default, default, ready);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        //queue
        public Embed queue_async(int? input_page)
        {
            try
            {
                string now_playing_title = "";

                if (lava_player == null)
                {
                    var ready = new EmbedBuilder()
                        .WithAuthor("Queue")
                        .WithDescription("```bash\n\"Now Playing\"\n```\n" + $"```There are no currently playing track right now```" + "\n\n```bash\n\"Queue List\"\n```\n" + "```" + "There are no more tracks in the queue" + "```")
                        .WithColor(Color.Green)
                        .WithFooter($"Loop Status : {Gvar.loop_flag.ToString()}\n" + $"There are total {0} tracks in the queue")
                        .WithCurrentTimestamp()
                        .Build();

                    return ready;
                }

                if (!lava_player.IsPlaying)
                {
                    now_playing_title = "There are no currently playing track right now";
                }
                else
                {
                    now_playing_title = lava_player.CurrentTrack.Title;
                }

                string queue_string = "";
                int queue_count = 0;
                var queue_list = lava_player.Queue.Items.ToList();

                if (lava_player.Queue.Count == 0)
                {
                    if (Gvar.loop_flag == true)
                    {
                        queue_string = "";
                    }
                    else
                    {
                        queue_string = "There are no more tracks in the queue";
                    }
                }

                if (Gvar.loop_flag == false)
                {
                    int page = 0;
                    int queue_index = 0;
                    int queue_track_index = 0;
                    LavaTrack[,] queue_list_array = new LavaTrack[1000,10];

                    foreach (var queue_item in queue_list)
                    {
                        queue_list_array[queue_index, queue_track_index] = queue_item as LavaTrack;
                        if (queue_track_index == 9)
                        {
                            queue_index++;
                            queue_track_index = 0;
                        }
                        else
                        {
                            queue_track_index++;
                        }
                    }

                    if (!(input_page is null))
                    {
                        if (input_page <= 0)
                        {
                            return new EmbedBuilder()
                                .WithColor(Color.Green)
                                .WithDescription("Please input the correct page number")
                                .Build();
                        }

                        if (input_page > queue_index + 1)
                        {
                            return new EmbedBuilder()
                                .WithColor(Color.Green)
                                .WithDescription("Please input the correct page number").Build();
                        }

                        page = (int)input_page - 1;
                    }

                    for (int i = 0; i <= 9; i++)
                    {
                        if (queue_list_array[page, i] is null)
                        {
                            continue;
                        }
                        var next_track = queue_list_array[page, i];
                        queue_string += $"{(page * 10) + queue_count + 1}. " + $"{next_track.Title}\n\n";
                        queue_count++;
                    }

                    if (lava_player.Queue.Items.ToList().Count() == 0)
                    {
                        queue_string = "There are no more tracks in the queue";
                    }
                    
                    var ready = new EmbedBuilder()
                        .WithAuthor("Queue")
                        .WithDescription(
                        "```bash\n\"Now Playing\"\n```\n" +
                        $"```{now_playing_title}```" +
                        "\n\n```bash\n\"Queue List\"\n```\n" +
                        "```" +
                        queue_string +
                        "```")
                        .WithColor(Color.Green)
                        .WithFooter(
                        $"Loop Status : {Gvar.loop_flag.ToString()}\n" +
                        $"Current Page : {(page + 1).ToString()} / {(queue_index + 1).ToString()}\n" +
                        $"There are total {queue_list.Count() + 1} tracks in the queue"
                        )
                        .WithCurrentTimestamp()
                        .Build();

                    return ready;
                }
                else
                {
                    queue_count = 1;
                    queue_list = Gvar.list_loop_track;

                    if (lava_player.Queue.Items.ToList().Count() == 0 && queue_list.Count() == 0)
                    {
                        queue_string = "There are no more tracks in the queue";
                    }

                    int page = 0;
                    int queue_index = 0;
                    int queue_track_index = 0;
                    LavaTrack[,] queue_list_array = new LavaTrack[1000, 10];

                    foreach (var queue_item in queue_list)
                    {
                        queue_list_array[queue_index, queue_track_index] = queue_item as LavaTrack;
                        if (queue_track_index == 9)
                        {
                            queue_index++;
                            queue_track_index = 0;
                        }
                        else
                        {
                            queue_track_index++;
                        }
                    }

                    if (!(input_page is null))
                    {
                        if (input_page <= 0)
                        {
                            return new EmbedBuilder()
                                .WithColor(Color.Green)
                                .WithDescription("Please input the correct page number")
                                .Build();
                        }

                        if (input_page > queue_index + 1)
                        {
                            return new EmbedBuilder()
                                .WithColor(Color.Green)
                                .WithDescription("Please input the correct page number")
                                .Build();
                        }

                        page = (int)input_page - 1;
                    }

                    if (page == 0)
                    {
                        //halaman pertama
                        queue_string += $"{queue_count}. " + $"{Gvar.loop_track.Title}\n\n";
                        for (int i = 0; i <= 8; i++)
                        {
                            if (queue_list_array[page, i] is null)
                            {
                                continue;
                            }
                            var next_track = queue_list_array[page, i];
                            queue_string += $"{(page * 10) + queue_count + 1}. " + $"{next_track.Title}\n\n";
                            queue_count++;
                        }
                    }
                    else
                    {
                        var next_track = queue_list_array[page - 1, 9];
                        queue_string += $"{(page * 10) + queue_count}. " + $"{next_track.Title}\n\n";
                        queue_count++;
                        //halaman selanjutnya
                        for (int i = 0; i <= 8; i++)
                        {
                            if (queue_list_array[page, i] is null)
                            {
                                continue;
                            }
                            next_track = queue_list_array[page, i];
                            queue_string += $"{(page * 10) + queue_count}. " + $"{next_track.Title}\n\n";
                            queue_count++;
                        }
                    }
                    
                    var ready = new EmbedBuilder()
                        .WithAuthor("Queue")
                        .WithDescription(
                        "```bash\n\"Now Playing\"\n```\n" +
                        $"```{now_playing_title}```" +
                        "\n\n```bash\n\"Looping Queue List\"\n```\n" +
                        "```" +
                        queue_string +
                        "```")
                        .WithColor(Color.Green)
                        .WithFooter(
                        $"Loop Status : {Gvar.loop_flag.ToString()}\n" +
                        $"Current Page : {(page + 1).ToString()} / {(queue_index + 1).ToString()}\n" +
                        $"There are total {queue_list.Count() + 1} tracks in the queue"
                        )
                        .WithCurrentTimestamp()
                        .Build();

                    return ready;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return new EmbedBuilder()
                .WithColor(Color.Green)
                .WithDescription("Error , Contact pres asap")
                .Build();
        }

        //clear
        public async Task<string> clear_not_async()
        {
            if (lava_player == null)
            {
                return "There are no track playing at this time.";
            }
            await lava_player.StopAsync();
            lava_player.Queue.Clear();
            lava_player = null;
            clear_all_loop();
            return "Tracks cleared.";
        }

        //skip
        public async Task<string> skip_async()
        {
            var old_track = lava_player.CurrentTrack;
            if (lava_player.IsPlaying && lava_player.Queue.Count == 0)
            {
                await lava_player.StopAsync();
                await lava_player.TextChannel.SendMessageAsync($"Successfully skipped {old_track.Title}");
                return " ";
            }

            if (lava_player == null || lava_player.Queue.Count == 0)
            {
                return "Nothing in queue";
            }
            
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

            if (vol < 0 || vol > 100)
            {
                return "Volume must between 0 - 100";
            }

            await lava_player.SetVolumeAsync(vol);
            return $"Volume set to {vol}";
        }

        //volume earrape
        public async Task<string> set_Earrape()
        {
            if (lava_player == null)
            {
                return "Player need to be connected to the channel first";
            }

            await lava_player.SetVolumeAsync(1000);
            return $"Mampos lo 1000 volume earrape!!";
        }

        //pause
        public async Task<string> pause_async()
        {
            if (lava_player == null)
            {
                return "There are no track playing at this time.";
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
                return "There are no track playing at this time.";
            }

            if (lava_player.IsPaused != true)
            {
                return "Track still playing.";
            }

            await lava_player.ResumeAsync();
            return "Track resumed.";
        }

        //shuffle
        public string shuffle_async()
        {
            if (lava_player == null)
            {
                return "There are no track playing at this time.";
            }

            lava_player.Queue.Shuffle();
            Gvar.list_loop_track = lava_player.Queue.Items.ToList();
            return "Track shuffled.";
        }

        private Task Lava_socket_client_Log(LogMessage Logmessage)
        {
            Console.WriteLine(Logmessage.Message);
            return Task.CompletedTask;
        }

        private async Task Maicy_client_Ready_async()
        {
            await lava_socket_client.StartAsync(maicy_client, new Configuration()
            {
                AutoDisconnect = false
            });

            CredentialsAuth auth = new CredentialsAuth("56894be43189492a881161efd8963cb0", "06a0a3c3331247c4bf4f2a5f979a3d11");
            Token token = await auth.GetToken();
            _spotify = new SpotifyWebAPI()
            {
                AccessToken = token.AccessToken,
                TokenType = token.TokenType
            };
        }
    }
}
