using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LunaLyrics.Assets.Scripts
{
    public struct MediaData
    {
        public int id;
        public string name;
        public string trackName;
        public string artistName;
        public string albumName;
        public float duration;
        public bool instrumental;

        public string plainLyrics;
        public string syncedLyrics;
    }

    public struct SyncedLyricsData
    {
        public float duration;
        public Dictionary<float, string> syncedLyrics;
    }
}