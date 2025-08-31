namespace LunaLyrics.Data
{

    public struct UpdateMediaSignal
    {
        public string title;
        public string artist;
        public string mediaKey;
        public double duration;

        public UpdateMediaSignal(string title, string artist, string mediaKey, double duration)
        {
            this.title = title;
            this.artist = artist;
            this.mediaKey = mediaKey;
            this.duration = duration;
        }
    }

    public struct CheckNextLyricsSignal
    {
        public double position;

        public CheckNextLyricsSignal(double position)
        {
            this.position = position;
        }
    }

    public struct NewLyricsSignal
    {
        public LyricLine lyricLine;

        public NewLyricsSignal(LyricLine lyricLine)
        {
            this.lyricLine = lyricLine;
        }
    }
}