using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using LunaLyrics.Assets.Scripts;


public static class LyricsLoader
{
    public static async UniTask<SyncedLyricsData> SearchLyrics(string artist, string title)
    {
        var result = new SyncedLyricsData { syncedLyrics = new() };
        string uri = $"https://lrclib.net/api/get?artist_name={artist.Replace(' ', '+')}&track_name={title.Replace(' ', '+')}";

        UnityWebRequest webRequest = UnityWebRequest.Get(uri);
        await webRequest.SendWebRequest();

        if (webRequest.result != UnityWebRequest.Result.Success) return result;

        string jsonResponse = webRequest.downloadHandler.text;
        if (jsonResponse.Contains("404")) return result;

        var parseResult = JsonUtility.FromJson<MediaData>(jsonResponse);
        var lyricsArr = parseResult.syncedLyrics.Split("\n");

        foreach (var lyrics in lyricsArr)
        {
            var timeStr = lyrics.Substring(1, 8).Split(":");
            var timeNum = (float.Parse(timeStr[0]) * 60f) + float.Parse(timeStr[1]);
            var lyricsStr = lyrics[11..];

            if (lyricsStr.Length > 0 || lyricsStr == "(End)") result.syncedLyrics.Add(timeNum, lyricsStr);
        }

        return result;
    }
}