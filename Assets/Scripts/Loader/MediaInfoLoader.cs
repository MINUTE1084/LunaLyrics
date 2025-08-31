using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using LunaLyrics.Data;
using UnityEngine;
using Zenject;

namespace LunaLyrics.Loader
{
    public class MediaInfoLoader : MonoBehaviour
    {
        // 별도 제작한 전용 Cpp 라이브러리
        private const string DllName = "LunaLyrics.CppNative";
        [DllImport(DllName)] private static extern bool InitializeMediaManager();
        [DllImport(DllName)] private static extern void UpdateMediaInfo();
        [DllImport(DllName, CharSet = CharSet.Unicode)] private static extern void GetMediaTitle(StringBuilder buffer, int bufferSize);
        [DllImport(DllName, CharSet = CharSet.Unicode)] private static extern void GetMediaArtist(StringBuilder buffer, int bufferSize);
        [DllImport(DllName)] private static extern double GetPositionInSeconds();
        [DllImport(DllName)] private static extern double GetDurationInSeconds();
        [DllImport(DllName)] private static extern bool IsPlaying();

        [Inject] private readonly SignalBus signalBus;

        private bool _isInitialized = false;

        private Thread _mediaInfoThread;
        private volatile bool _isThreadRunning = false;
        private readonly object _dataLock = new();

        private string _title = "";
        private string _artist = "";
        private double _position = 0.0;
        private double _duration = 0.0;
        private bool _isPlaying = false;
        private bool _hasData = false;

        private double _lastKnownPosition = -1.0;
        private double _interpolatedPosition = 0.0;

        private bool _wasPlayingLastFrame = false;
        private string _wasMediaKey;

        private void Start()
        {
            if (InitializeMediaManager())
            {
                // 초기화 성공 시 미디어 정보 불러오는 스레드 생성
                _isInitialized = true;
                _isThreadRunning = true;
                _mediaInfoThread = new Thread(MediaInfoWorker);
                _mediaInfoThread.Start();
            }
            else
            {
                _isInitialized = false;
            }
        }

        private void MediaInfoWorker()
        {
            StringBuilder titleBuffer = new(256);
            StringBuilder artistBuffer = new(256);

            // 프로그램 꺼질 때 까지
            while (_isThreadRunning)
            {
                // 윈도우 데이터 업데이트
                UpdateMediaInfo();

                // 각종 데이터를 라이브러리에서 로드
                titleBuffer.Clear();
                GetMediaTitle(titleBuffer, titleBuffer.Capacity);

                artistBuffer.Clear();
                GetMediaArtist(artistBuffer, artistBuffer.Capacity);

                double position = GetPositionInSeconds();
                double duration = GetDurationInSeconds();
                bool isPlaying = IsPlaying();
                bool hasData = titleBuffer.Length > 0;

                // 유니티 스레드 충돌 방지를 위해 잠금
                lock (_dataLock)
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
            if (!_isInitialized) return;

            string currentTitle, currentArtist;
            double currentPosition, currentDuration;
            bool currentIsPlaying, currentHasData;

            // 라이브러리 스레드 충돌 방지를 위해 잠금
            lock (_dataLock)
            {
                currentTitle = _title;
                currentArtist = _artist;
                currentPosition = _position;
                currentDuration = _duration;
                currentIsPlaying = _isPlaying;
                currentHasData = _hasData;
            }

            // 재생 시간 보간
            if (Math.Abs(currentPosition - _lastKnownPosition) > 0.001)
            {
                _lastKnownPosition = currentPosition;
                _interpolatedPosition = currentPosition;
            }
            else if (currentIsPlaying)
            {
                _interpolatedPosition += Time.deltaTime;
            }

            var mediaKey = $"{currentTitle}-{currentArtist}";
            bool isNewMedia = mediaKey != _wasMediaKey;
            if (currentHasData) // 음악 데이터가 있는 경우
            {
                if (isNewMedia) // 첫 음악 or 음악이 바뀐 경우
                {
                    signalBus.Fire(new UpdateMediaSignal(currentTitle, currentArtist, mediaKey, currentDuration)); // 음악 업데이트 이벤트 발송
                    _wasMediaKey = mediaKey;
                }
                else if (currentIsPlaying) // 재생 중인 경우 
                {
                    signalBus.Fire(new CheckNextLyricsSignal(_interpolatedPosition));
                }
            }

            _wasPlayingLastFrame = _isPlaying;
        }

        private void OnDestroy()
        {
            // 프로그램 종료 시 스레드도 같이 종료
            _isThreadRunning = false;
            if (_mediaInfoThread != null && _mediaInfoThread.IsAlive)
            {
                _mediaInfoThread.Join(); // 종료 될 때까지 대기, 종료 전까지 OnDestroy가 끝나지 않음
            }
        }
    }
}