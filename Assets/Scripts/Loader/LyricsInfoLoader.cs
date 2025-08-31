using System;
using System.Collections.Generic;
using LunaLyrics.Data;
using UnityEngine;
using Zenject;

namespace LunaLyrics.Loader
{
    public class LyricsInfoLoader : MonoBehaviour
    {
        [Inject] private readonly SignalBus signalBus;

        private Dictionary<string, SyncedLyricsData> cachedLyricsData = new();
        private SyncedLyricsData lyricsData;
        private SyncedLyricsData emptyData;

        private int wasLyricIndex = -1;
        private int currentLyricIndex = -1;
        private int _pauseFrame;

        private void Awake()
        {
            LyricsLoader.LoadLyricFromCache();
        }

        private void Start()
        {
            signalBus.Subscribe<UpdateMediaSignal>(LoadLyrics); // 미디어 업데이트 이벤트 구독
            signalBus.Subscribe<CheckNextLyricsSignal>(UpdateLyricUI); // 가사 체크 이벤트 구독
        }

        private async void LoadLyrics(UpdateMediaSignal signal)
        {
            lyricsData = emptyData;
            currentLyricIndex = -1;

            if (signal.artist == "" || signal.title == "") return;

            if (!cachedLyricsData.ContainsKey(signal.mediaKey))
            {
                var result = await LyricsLoader.SearchLyrics(signal.artist.Split("(feat.")[0].Split(",")[0], signal.title.Split("(feat.")[0]);
                if (result.syncedLyrics.Count > 0 && Math.Abs(result.duration - signal.duration) < 5) cachedLyricsData.TryAdd(signal.mediaKey, result);
            }

            if (cachedLyricsData.ContainsKey(signal.mediaKey))
                lyricsData = cachedLyricsData[signal.mediaKey];
        }

        private void UpdateLyricUI(CheckNextLyricsSignal signal)
        {
            if (lyricsData.syncedLyrics == null || lyricsData.syncedLyrics.Count == 0) return;

            // 새 곡 시작 or 뒤로 감기 한 경우 가사 인덱스 초기화
            if (currentLyricIndex >= 0 && signal.position < lyricsData.syncedLyrics[currentLyricIndex].TimeInSeconds)
            {
                _pauseFrame = 1;
                currentLyricIndex = -1;
            }

            int nextLyricIndex = currentLyricIndex + 1;

            // 가장 최근 위치의 가사 검색
            while (nextLyricIndex < lyricsData.syncedLyrics.Count && signal.position >= lyricsData.syncedLyrics[nextLyricIndex].TimeInSeconds)
            {
                currentLyricIndex = nextLyricIndex++;
            }

            // 가사가 바뀌었으면
            if (wasLyricIndex != currentLyricIndex && currentLyricIndex >= 0 && _pauseFrame-- <= 0)
            {
                signalBus.Fire(new NewLyricsSignal(lyricsData.syncedLyrics[currentLyricIndex])); // 새 가사 출력 이벤트 발송
                wasLyricIndex = currentLyricIndex;
            }
        }
    }
}