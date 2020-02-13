using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Victoria.Entities;

namespace maicy_bot_core.MiscData
{
    static class Gvar
    {
        //Lava Player Loop Flag
        public static bool loop_flag { get; set; }
        public static LavaTrack loop_track { get; set; }
        public static List<Victoria.Queue.IQueueObject> list_loop_track { get; set; }
    }
}
