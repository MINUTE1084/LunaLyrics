using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using LunaLyrics.Assets.Scripts;
using System.Text.RegularExpressions;
using System;
using UnityEngine.UI;


namespace LunaLyrics.Assets.Scripts
{
    public static class LyricsLoader
    {
        public static async UniTask<SyncedLyricsData> SearchLyrics(string artist, string title)
        {
            var result = new SyncedLyricsData { syncedLyrics = new() };
            string uri = $"https://lrclib.net/api/get?artist_name={RemoveBracketsAndSpaces(artist).Replace(' ', '+')}&track_name={RemoveBracketsAndSpaces(title).Replace(' ', '+')}";

            try
            {
                UnityWebRequest webRequest = UnityWebRequest.Get(uri);
                await webRequest.SendWebRequest();

                if (webRequest.result != UnityWebRequest.Result.Success) return result;

                string jsonResponse = webRequest.downloadHandler.text;

                if (jsonResponse.Contains("404")) return result;

                var parseResult = JsonUtility.FromJson<MediaData>(jsonResponse);
                if (parseResult.syncedLyrics.Length < 1) return result;

                var lyricsArr = parseResult.syncedLyrics.Split("\n");
                result.duration = parseResult.duration;

                foreach (var lyrics in lyricsArr)
                {
                    var timeStr = lyrics[1..].Split("]")[0].Split(":");
                    var timeNum = (float.Parse(timeStr[0]) * 60f) + float.Parse(timeStr[1]);
                    var lyricsStr = lyrics.Split("]")[1];

                    if (lyricsStr.Length > 0 && lyricsStr != "(End)")
                    {
                        if (lyricsStr[0] == ' ') lyricsStr = lyricsStr.Substring(1);

                        if (result.syncedLyrics.Count > 0)
                        {
                            var lastData = result.syncedLyrics[^1];
                            double textLength = lastData.Text.Length;
                            result.syncedLyrics[^1].LyricDuration = timeNum - lastData.TimeInSeconds;

                            if (textLength >= 50)
                            {
                                var lastIndex = result.syncedLyrics.Count - 1;

                                var sepCount = Math.Ceiling(textLength / 40d);
                                var targetLength = textLength / sepCount;
                                var lyricsWords = lastData.Text.Split(' ');

                                var newLyrics = "";
                                var timeOffset = 0d;
                                var duration = 0d;
                                foreach (var word in lyricsWords)
                                {
                                    newLyrics += word + ' ';
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

                        result.syncedLyrics.Add(new LyricLine
                        {
                            TimeInSeconds = timeNum - 0.25f,
                            Text = RemoveSpaces(lyricsStr)
                        });
                    }

                }
            }
            catch (Exception)
            {
                Debug.Log($"Couldn't Load Media: {artist} - {title}");
            }

            if (result.syncedLyrics.Count > 1)
            {
                result.syncedLyrics.Sort((a, b) => a.TimeInSeconds.CompareTo(b.TimeInSeconds));

                result.syncedLyrics[^1].LyricDuration = result.syncedLyrics[^1].Text.Length * 0.15f;
            }

            return result;
        }

        public static string RemoveSpaces(string input)
        {
            string result = Regex.Replace(input, "\\s\\(.*?\\)", "");
            result = Regex.Replace(result, "\\s+", " ");
            result = result.Trim();

            return result;
        }

        public static string RemoveBracketsAndSpaces(string input)
        {
            string result = Regex.Replace(input, "\\s\\(.*?\\)", "");
            result = Regex.Replace(result, "\\s+", " ");
            result = result.Trim();

            return result;
        }
    }
}