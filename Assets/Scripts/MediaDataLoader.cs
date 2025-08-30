using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using LunaLyrics.Assets.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MediaDataLoader : MonoBehaviour
{
    // --- C++ DLL 함수 임포트 (수정 없음) ---
    private const string DllName = "LunaLyrics.CppNative";
    [DllImport(DllName)] private static extern bool InitializeMediaManager();
    [DllImport(DllName)] private static extern void UpdateMediaInfo();
    [DllImport(DllName, CharSet = CharSet.Unicode)] private static extern void GetMediaTitle(StringBuilder buffer, int bufferSize);
    [DllImport(DllName, CharSet = CharSet.Unicode)] private static extern void GetMediaArtist(StringBuilder buffer, int bufferSize);
    [DllImport(DllName)] private static extern double GetPositionInSeconds();
    [DllImport(DllName)] private static extern double GetDurationInSeconds();
    [DllImport(DllName)] private static extern bool IsPlaying();

    public TMP_Text displayText;
    public TMP_Text lyricsText;

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


    private Dictionary<string, SyncedLyricsData> cachedLyricsData = new();
    private SyncedLyricsData lyricsData;
    private SyncedLyricsData emptyData;


    private void Start()
    {
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
            if (displayText != null) displayText.text = "Error";
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
        UpdateUI(currentTitle, currentArtist, displayPosition, currentDuration, currentIsPlaying, currentHasData);


        var mediaKey = $"{currentTitle}-{currentArtist}";
        if (_hasData && (wasHasDataLastFrame != _hasData || mediaKey != wasMediaKey)) LoadLyrics(currentTitle, currentArtist, mediaKey);

        wasPlayingLastFrame = _isPlaying;
        wasHasDataLastFrame = _hasData;
    }

    private void UpdateUI(string title, string artist, double position, double duration, bool playing, bool hasData)
    {
        if (displayText == null) return;
        if (hasData)
        {
            TimeSpan posSpan = TimeSpan.FromSeconds(position);
            TimeSpan durSpan = TimeSpan.FromSeconds(duration);
            string status = playing ? "Play: " : "Stop: ";

            displayText.text = $"Artist: {artist}\n" +
                               $"Title: {title} {status}\n" +
                               $"Time: {posSpan:mm\\:ss} / {durSpan:mm\\:ss}";
        }
        else
        {
            displayText.text = "Stopped";
        }
    }

    private async void LoadLyrics(string title, string artist, string mediaKey)
    {
        lyricsData = emptyData;

        if (artist == "" || title == "") return;
        wasMediaKey = mediaKey;

        if (!cachedLyricsData.ContainsKey(mediaKey))
        {
            var result = await LyricsLoader.SearchLyrics(artist, title);
            if (result.syncedLyrics.Count > 0) cachedLyricsData.Add(mediaKey, result);
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

