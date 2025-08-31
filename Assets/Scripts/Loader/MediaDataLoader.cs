using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using LunaLyrics.Data;
using LunaLyrics.Util;
using LunaLyrics.Visual;
using UnityEngine;


namespace LunaLyrics.Loader
{
    public class MediaDataLoader : MonoBehaviour
    {
        private const string DllName = "LunaLyrics.CppNative";
        [DllImport(DllName)] private static extern bool InitializeMediaManager();
        [DllImport(DllName)] private static extern void UpdateMediaInfo();
        [DllImport(DllName, CharSet = CharSet.Unicode)] private static extern void GetMediaTitle(StringBuilder buffer, int bufferSize);
        [DllImport(DllName, CharSet = CharSet.Unicode)] private static extern void GetMediaArtist(StringBuilder buffer, int bufferSize);
        [DllImport(DllName)] private static extern double GetPositionInSeconds();
        [DllImport(DllName)] private static extern double GetDurationInSeconds();
        [DllImport(DllName)] private static extern bool IsPlaying();

        public VisualLyrics lyricsText;
        public Transform textParent;
        public Material colorMaterial;
        public Color[] textColors;



        private bool isInitialized = false;

        private Thread mediaInfoThread;
        private volatile bool isThreadRunning = false;
        private readonly object dataLock = new object();
        private string _title = "";
        private string _artist = "";
        private double _position = 0.0;
        private double _duration = 0.0;
        private bool _isPlaying = false;
        private bool _hasData = false;

        private double lastKnownPosition = -1.0;
        private double interpolatedPosition = 0.0;

        private string wasMediaKey = "";
        private bool wasPlayingLastFrame = false;
        private bool wasHasDataLastFrame = false;
        private int wasLyricIndex = -1;


        private Dictionary<string, SyncedLyricsData> cachedLyricsData = new();
        private SyncedLyricsData lyricsData;
        private SyncedLyricsData emptyData;

        private int currentLyricIndex = -1;

        private ObjectPool<VisualLyrics> objectPool;

        private void Start()
        {
            objectPool = new ObjectPool<VisualLyrics>(lyricsText, new PoolOptions(textParent, "Lyrics", 5), null, null, null);
            VisualLyrics.lastTextPos = Vector2.zero;

            if (InitializeMediaManager())
            {
                isInitialized = true;
                isThreadRunning = true;
                mediaInfoThread = new Thread(MediaInfoWorker);
                mediaInfoThread.Start();
            }
            else
            {
                isInitialized = false;
            }
        }

        private void MediaInfoWorker()
        {
            StringBuilder titleBuffer = new StringBuilder(256);
            StringBuilder artistBuffer = new StringBuilder(256);
            while (isThreadRunning)
            {
                UpdateMediaInfo();
                titleBuffer.Clear();
                GetMediaTitle(titleBuffer, titleBuffer.Capacity);
                artistBuffer.Clear();
                GetMediaArtist(artistBuffer, artistBuffer.Capacity);
                double position = GetPositionInSeconds();
                double duration = GetDurationInSeconds();
                bool isPlaying = IsPlaying();
                bool hasData = titleBuffer.Length > 0;

                lock (dataLock)
                {
                    _title = titleBuffer.ToString();
                    _artist = artistBuffer.ToString();
                    _position = position;
                    _duration = duration;
                    _isPlaying = isPlaying;
                    _hasData = hasData;
                }
                Thread.Sleep(500);
            }
        }

        private void Update()
        {
            if (!isInitialized) return;

            string currentTitle, currentArtist;
            double currentPosition, currentDuration;
            bool currentIsPlaying, currentHasData;
            lock (dataLock)
            {
                currentTitle = _title;
                currentArtist = _artist;
                currentPosition = _position;
                currentDuration = _duration;
                currentIsPlaying = _isPlaying;
                currentHasData = _hasData;
            }

            if (Math.Abs(currentPosition - lastKnownPosition) > 0.001)
            {
                lastKnownPosition = currentPosition;
                interpolatedPosition = currentPosition;
            }
            else if (currentIsPlaying)
            {
                interpolatedPosition += Time.deltaTime;
            }

            double displayPosition = Math.Min(interpolatedPosition, currentDuration);
            UpdateUI(displayPosition, currentIsPlaying, currentHasData);

            wasPlayingLastFrame = _isPlaying;
            wasHasDataLastFrame = _hasData;
        }

        private void UpdateUI(double position, bool playing, bool hasData)
        {
            if (hasData)
            {
                var mediaKey = $"{_title}-{_artist}";
                if (wasHasDataLastFrame != _hasData || mediaKey != wasMediaKey)
                {
                    VisualLyrics.lastTextPos = Vector2.zero;
                    objectPool.Clear();
                    colorMaterial.SetColor("_GlowColor", textColors[UnityEngine.Random.Range(0, textColors.Length)]);
                    lyricsData = emptyData;
                    currentLyricIndex = -1;

                    Debug.Log($"{_title}, {_artist}, {_duration}");
                    LoadLyrics(_title, _artist, mediaKey, _duration);
                }
                if (playing) UpdateLyricUI(position);
            }
        }

        void UpdateLyricUI(double currentPositionInSeconds)
        {
            if (lyricsData.syncedLyrics == null || lyricsData.syncedLyrics.Count == 0) return;
            int nextLyricIndex = currentLyricIndex + 1;
            while (nextLyricIndex < lyricsData.syncedLyrics.Count && currentPositionInSeconds >= lyricsData.syncedLyrics[nextLyricIndex].TimeInSeconds)
            {
                nextLyricIndex++;
                currentLyricIndex++;
            }

            if (currentLyricIndex >= 0 && currentPositionInSeconds < lyricsData.syncedLyrics[currentLyricIndex].TimeInSeconds)
            {
                currentLyricIndex = -1;
            }

            if (wasLyricIndex != currentLyricIndex)
            {
                if (currentLyricIndex >= 0)
                {
                    var text = objectPool.Pop();
                    text.SetText(lyricsData.syncedLyrics[currentLyricIndex]);
                }

                wasLyricIndex = currentLyricIndex;
            }
        }

        public void ReturnLyrics(VisualLyrics obj)
        {
            objectPool.Push(obj);
        }

        private async void LoadLyrics(string title, string artist, string mediaKey, double duration)
        {
            lyricsData = emptyData;

            if (artist == "" || title == "") return;
            wasMediaKey = mediaKey;

            if (!cachedLyricsData.ContainsKey(mediaKey))
            {
                var result = await LyricsLoader.SearchLyrics(artist.Split("(feat.")[0].Split(",")[0], title.Split("(feat.")[0]);
                if (result.syncedLyrics.Count > 0 && Math.Abs(result.duration - duration) < 5) cachedLyricsData.TryAdd(mediaKey, result);
            }

            if (cachedLyricsData.ContainsKey(mediaKey))
                lyricsData = cachedLyricsData[mediaKey];

            wasMediaKey = mediaKey;
        }

        private void OnDestroy()
        {
            isThreadRunning = false;
            if (mediaInfoThread != null && mediaInfoThread.IsAlive)
            {
                mediaInfoThread.Join();
            }
        }

    }

}