using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace maicy_bot_core.MaicyModule
{
    public class Ping : ModuleBase<SocketCommandContext>
    {
        [Command("About")]
        public async Task About()
        {
            await ReplyAsync("Hi! Im Maicy!");
        }
    }
}
