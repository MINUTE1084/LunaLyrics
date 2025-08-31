using LunaLyrics.Data;
using LunaLyrics.Util;
using UnityEngine;
using Zenject;

namespace LunaLyrics.Visual
{
    public class VisualLyricsManager : MonoBehaviour
    {
        [Inject] private readonly SignalBus signalBus;

        [SerializeField] private VisualLyrics lyricsText;
        [SerializeField] private RectTransform textParent;
        [SerializeField] private Material colorMaterial;
        [SerializeField] private Color[] textColors;

        [Header("Position Setting")]
        public float minDistance = 150f;
        public float randomOffset = 300f;

        [Header("Jitter Setting")]
        public float update = 0.15f;
        public float duration = 2.5f;
        public Vector2 positionJitter = Vector2.one * 0.005f;
        public Vector2 scaleJitter = Vector2.one * 0.03f;

        [Header("Stair Setting")]
        public float minAmount = 4;
        public float stepAmount = 0.2f;
        public int stairCount = 30;

        private ObjectPool<VisualLyrics> _objectPool;
        private PoolOption _poolOption;

        private Vector3 _lastTextPos;
        private float _minX, _maxX, _minY, _maxY;

        private void Start()
        {
            _poolOption = new(textParent, "Lyrics", 5);
            _objectPool = new ObjectPool<VisualLyrics>(lyricsText, _poolOption, InitText, null);
            ResetLyrics();

            // 현재 캔버스 크기 기반으로 범위 지정
            float screenWidth = textParent.rect.width;
            float screenHeight = textParent.rect.height;

            float paddingX = screenWidth * 0.05f;
            float paddingY = screenHeight * 0.1f;

            _minX = textParent.rect.xMin + paddingX;
            _maxX = textParent.rect.xMax - paddingX;
            _minY = textParent.rect.yMin + paddingY;
            _maxY = textParent.rect.yMax - paddingY;

            _lastTextPos = Vector3.zero;

            // 가사 이벤트 구독
            signalBus.Subscribe<NewLyricsSignal>(AddText);
        }

        private void InitText(VisualLyrics obj)
        {
            obj.Init(_objectPool, UpdateTextPosition);
        }

        private void AddText(NewLyricsSignal signal)
        {
            var text = _objectPool.Pop();

            text.SetJitterData(update, duration, positionJitter, scaleJitter);
            text.SetStairData(minAmount, stepAmount, stairCount);
            text.SetText(signal.lyricLine);
        }

        private void ResetLyrics()
        {
            _lastTextPos = Vector2.zero;
            colorMaterial.SetColor("_GlowColor", textColors[Random.Range(0, textColors.Length)]);
            _objectPool.Clear();
        }

        private Vector2 UpdateTextPosition(float textWidth, float textHeight)
        {
            Vector3 direction = new Vector2((Random.value * (Random.value < 0.5f ? -1 : 1)) - 0.5f, (0.5f + Random.value * 2) * (Random.value < 0.5f ? -1 : 1)).normalized;
            Vector3 newPosition;

            // 기존 위치에서 무작위 방향 & 거리로 이동
            newPosition = _lastTextPos + direction * (minDistance + Random.Range(0, randomOffset));

            var finalMinX = _minX;
            var finalMaxX = Mathf.Max(_minX, _maxX - textWidth);
            var finalMinY = _minY + textHeight / 2;
            var finalMaxY = _maxY - textHeight / 2;

            // 화면 밖으로 나가면 반대쪽에서 나옴
            if (newPosition.x < finalMinX) newPosition.x = finalMaxX - newPosition.x;
            if (newPosition.x > finalMaxX) newPosition.x -= finalMaxX;
            if (newPosition.y < finalMinY) newPosition.y = finalMaxY - newPosition.x;
            if (newPosition.y > finalMaxY) newPosition.y -= finalMaxY;

            // 텍스트 길이가 화면을 나갈것 같으면 그만큼 이동
            newPosition.x = Mathf.Clamp(newPosition.x, finalMinX, finalMaxX);
            newPosition.y = Mathf.Clamp(newPosition.y, finalMinY, finalMaxY);

            _lastTextPos = newPosition;
            return newPosition;
        }
    }
}