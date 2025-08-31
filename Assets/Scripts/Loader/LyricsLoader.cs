using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using System.Text.RegularExpressions;
using System;
using LunaLyrics.Data;
using System.Collections.Generic;
using System.IO;


namespace LunaLyrics.Loader
{
    public static class LyricsLoader
    {
        public static Dictionary<string, SyncedLyricsData> LoadLyricFromCache()
        {
            Dictionary<string, SyncedLyricsData> lyricsDict = new();
            string directoryPath = Path.Combine(Application.persistentDataPath, "LyricsData");

            if (!Directory.Exists(directoryPath)) return lyricsDict;
            string[] files = Directory.GetFiles(directoryPath, "*.json");

            foreach (var filePath in files)
            {
                string json = File.ReadAllText(filePath);
                var parseResult = JsonUtility.FromJson<SyncedLyricsData>(json);

                string key = Path.GetFileNameWithoutExtension(filePath);

                if (parseResult.syncedLyrics != null && parseResult.syncedLyrics.Count > 0)
                {
                    lyricsDict.Add(key, parseResult);
                }
            }

            return lyricsDict;
        }

        private static void SaveToJson(string mediaKey, string jsonData)
        {
            string directoryPath = Path.Combine(Application.persistentDataPath, "LyricsData");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string filePath = Path.Combine(directoryPath, $"{mediaKey}.json");
            File.WriteAllText(filePath, jsonData);
        }



        public static async UniTask<SyncedLyricsData> SearchLyrics(string artist, string title)
        {
            // LRCLIB에서 로드
            // TODO: 현재는 데이터 기반으로 바로 값을 가져오나, 검색 API로 변경 예정
            var result = new SyncedLyricsData { syncedLyrics = new() };
            string uri = $"https://lrclib.net/api/get?artist_name={RemoveBracketsAndSpaces(artist).Replace(' ', '+')}&track_name={RemoveBracketsAndSpaces(title).Replace(' ', '+')}";

            try
            {
                UnityWebRequest webRequest = UnityWebRequest.Get(uri);
                await webRequest.SendWebRequest();
                if (webRequest.result != UnityWebRequest.Result.Success) return result;

                string jsonResponse = webRequest.downloadHandler.text;
                var parseResult = JsonUtility.FromJson<MediaData>(jsonResponse); // 응답 json 변환
                if (parseResult.syncedLyrics.Length < 1) return result; // 타임스탬프 가사가 없으면 빈 데이터 리턴

                var lyricsArr = parseResult.syncedLyrics.Split("\n");
                result.duration = parseResult.duration;

                foreach (var lyrics in lyricsArr)
                {
                    // 타임스탬프 분리
                    var timeStr = lyrics[1..].Split("]")[0].Split(":");
                    var timeNum = (float.Parse(timeStr[0]) * 60f) + float.Parse(timeStr[1]);
                    var lyricsStr = lyrics.Split("]")[1];

                    if (lyricsStr.Length > 0 && lyricsStr != "(End)")
                    {
                        if (result.syncedLyrics.Count > 0) // 두번째 가사의 경우, 이전 가사에 대한 로직 진행
                        {
                            var lastData = result.syncedLyrics[^1];
                            double textLength = lastData.Text.Length;
                            result.syncedLyrics[^1].LyricDuration = timeNum - lastData.TimeInSeconds; // 가사 출력 시간 계산

                            // 50글자 넘어가면 분리 로직 진행
                            // TODO: 분리 길이 조정 기능 만들어야함 
                            if (textLength >= 50) TrimLyrics(lastData, textLength);
                        }

                        result.syncedLyrics.Add(new LyricLine
                        {
                            TimeInSeconds = timeNum - 0.25f,
                            Text = RemoveSpaces(lyricsStr) // 앞뒤 공백 제거
                        });
                    }

                }
            }
            catch (Exception) // 오류 발생 시 (곡 데이터 없음, 기타 변환 오류 등)
            {
                Debug.Log($"Couldn't Load Media: {artist} - {title}");
            }

            // 정렬 + 마지막 가사에 출력 길이 추가
            if (result.syncedLyrics.Count >= 1)
            {
                result.syncedLyrics.Sort((a, b) => a.TimeInSeconds.CompareTo(b.TimeInSeconds));
                result.syncedLyrics[^1].LyricDuration = result.syncedLyrics[^1].Text.Length * 0.15f;
            }

            var mediaKey = $"{artist}-{title}";
            SaveToJson(mediaKey, JsonUtility.ToJson(result, true));

            return result;


            void TrimLyrics(LyricLine lastData, double textLength)
            {
                var lastIndex = result.syncedLyrics.Count - 1;

                // 글자 기준 생성
                var sepCount = Math.Ceiling(textLength / 40d);
                var targetLength = textLength / sepCount;
                var lyricsWords = lastData.Text.Split(' ');

                var newLyrics = "";
                var timeOffset = 0d;
                var duration = 0d;
                foreach (var word in lyricsWords)
                {
                    newLyrics += word + ' ';

                    // 현재 분할구간이 목표 길이에 도달하거나, 마지막 단어인 경우 분할해서 저장
                    // 해당 가사의 출력 길이는 기존 전체 길이에 비율만큼 나누어짐
                    if (newLyrics.Length > targetLength || lyricsWords[^1] == word)
                    {
                        duration = lastData.LyricDuration * (newLyrics.Length / textLength);

                        result.syncedLyrics.Add(new LyricLine
                        {
                            TimeInSeconds = lastData.TimeInSeconds + timeOffset,
                            Text = RemoveSpaces(newLyrics),
                            LyricDuration = duration
                        });

                        timeOffset += duration - 0.4f;
                        newLyrics = "";
                    }
                }

                result.syncedLyrics.RemoveAt(lastIndex);
            }
        }

        private static string RemoveSpaces(string input)
        {
            string result = Regex.Replace(input, "\\s+", " ");
            result = result.Trim();

            return result;
        }

        private static string RemoveBracketsAndSpaces(string input)
        {
            string result = Regex.Replace(input, "\\s\\(.*?\\)", "");
            return RemoveSpaces(result);
        }
    }
}