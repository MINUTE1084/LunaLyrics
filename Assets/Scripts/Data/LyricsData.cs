using System.Collections.Generic;

namespace LunaLyrics.Data
{
    public struct MediaSearchData
    {
        public MediaData[] results;
    }

    public struct MediaData
    {
        public int id;
        public string name;
        public string trackName;
        public string artistName;
        public string albumName;
        public double duration;
        public bool instrumental;

        public string plainLyrics;
        public string syncedLyrics;
    }

    [System.Serializable]
    public struct SyncedLyricsData
    {
        public double duration;
        public List<LyricLine> syncedLyrics;
    }

    [System.Serializable]
    public class LyricLine
    {
        public double TimeInSeconds;
        public double LyricDuration;
        public string Text;
    }
}