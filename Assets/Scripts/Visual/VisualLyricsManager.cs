using System.Collections.Generic;
using System.Runtime.InteropServices;
using LunaLyrics.Data;
using LunaLyrics.Util;
using UnityEngine;
using Zenject;

namespace LunaLyrics.Visual
{
    public class VisualLyricsManager : MonoBehaviour
    {
        [Inject] private readonly SignalBus signalBus;

        [SerializeField] private VisualLyrics lyrics2DText;
        [SerializeField] private VisualLyrics lyrics3DText;

        [SerializeField] private RectTransform text2DParent;
        [SerializeField] private RectTransform text3DParent;

        [Header("Position Setting")]
        public float minDistance = 150f;
        public float randomOffset = 300f;
        public int maxUsedPosLength = 3;

        [Header("3D Setting")]
        public float zRange = 50;

        [Header("Jitter Setting")]
        public float update = 0.15f;
        public float duration = 2.5f;
        public Vector2 positionJitter = Vector2.one * 0.005f;
        public Vector2 scaleJitter = Vector2.one * 0.03f;

        [Header("Stair Setting")]
        public float minAmount = 4;
        public float stepAmount = 0.2f;
        public int stairCount = 30;

        [Header("Color")]
        [SerializeField] private Material lyricsMaterial;

        public bool IsRainbow { get; set; }
        public float H { get; set; }
        public float S { get; set; }
        public float V { get; set; }
        public float RainbowSpeed { get; set; }
        public bool Is3D { get; set; }

        private ObjectPool<VisualLyrics> _objectPool2D;
        private ObjectPool<VisualLyrics> _objectPool3D;

        private PoolOption _poolOption2D;
        private PoolOption _poolOption3D;

        private Vector3 _lastTextPos;
        private List<Vector3> _usedTextPos = new();

        private void Start()
        {
            Is3D = false;

            _poolOption2D = new(text2DParent, "Lyrics2D", 5);
            _objectPool2D = new ObjectPool<VisualLyrics>(lyrics2DText, _poolOption2D, InitText, null);

            _poolOption3D = new(text3DParent, "Lyrics3D", 5);
            _objectPool3D = new ObjectPool<VisualLyrics>(lyrics3DText, _poolOption3D, InitText, null);
            ResetLyrics();

            IsRainbow = false;
            H = Random.value;
            S = 0.93f;
            V = 0.93f;
            RainbowSpeed = 0.2f;

            _lastTextPos = Vector3.zero;
            signalBus.Subscribe<NewLyricsSignal>(AddText);
        }

        private void Update()
        {
            if (IsRainbow) H = (H + (RainbowSpeed * Time.deltaTime)) % 1f;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                var axis = Input.GetAxis("Vertical") * Time.deltaTime;

                if (Input.GetKeyDown(KeyCode.BackQuote)) Is3D = !Is3D;
                if (Input.GetKeyDown(KeyCode.Alpha1)) IsRainbow = !IsRainbow;

                if (Input.GetKey(KeyCode.Alpha2)) H = Mathf.Abs((H + axis) % 1f);
                if (Input.GetKey(KeyCode.Alpha3)) S = Mathf.Clamp01(S + axis);
                if (Input.GetKey(KeyCode.Alpha4)) V = Mathf.Clamp01(V + axis);
                if (Input.GetKey(KeyCode.Alpha5)) RainbowSpeed = Mathf.Clamp01(RainbowSpeed + axis);
            }

            var color = Color.HSVToRGB(H, S, V);
            lyricsMaterial.SetColor("_GlowColor", color);
        }

        private void InitText(VisualLyrics obj)
        {
            obj.Init(Is3D ? _objectPool3D : _objectPool2D, UpdateTextPosition);
        }

        private void AddText(NewLyricsSignal signal)
        {
            var text = Is3D ? _objectPool3D.Pop() : _objectPool2D.Pop();
            var finalMinAmount = minAmount * (Is3D ? 0.1f : 1f);
            var finalStepAmount = stepAmount * (Is3D ? 0.1f : 1f);

            text.SetJitterData(update, duration, positionJitter, scaleJitter);
            text.SetStairData(finalMinAmount, finalStepAmount, stairCount);
            text.SetText(signal.lyricLine);
        }

        private void ResetLyrics()
        {
            _lastTextPos = Vector2.zero;
            _usedTextPos.Clear();

            _objectPool3D.Clear();
            _objectPool2D.Clear();
        }

        public void Toggle3D()
        {
            Is3D = !Is3D;
        }

        private Vector3 UpdateTextPosition(float textWidth, float textHeight)
        {
            return Is3D ? Update3DTextPosition(textWidth, textHeight) : Update2DTextPosition(textWidth, textHeight);
        }

        private Vector3 Update3DTextPosition(float textWidth, float textHeight)
        {
            float screenHeight = 2.0f * 10 * Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float screenWidth = screenHeight * Camera.main.aspect;

            var offset = Camera.main.transform.position;
            Debug.Log($"{screenHeight} {screenWidth}");

            float paddingX = screenWidth * 0.05f;
            float paddingY = screenHeight * 0.1f;

            var finalMinX = text3DParent.rect.xMin + paddingX;
            var finalMaxX = Mathf.Max(finalMinX, text3DParent.rect.xMax - paddingX - textWidth);
            var finalMinY = text3DParent.rect.yMin + paddingY + textHeight / 2;
            var finalMaxY = text3DParent.rect.yMax - paddingY - textHeight / 2;

            Vector3 newPosition;
            bool passed;
            int count = 0;

            do
            {
                newPosition = new(Random.Range(finalMinX, finalMaxX), Random.Range(finalMinY, finalMaxY));
                passed = true;

                foreach (var lastPos in _usedTextPos)
                {
                    if ((lastPos - newPosition).magnitude < minDistance) passed = false;
                }
            } while (!passed && count++ < 50);

            _usedTextPos.Add(newPosition);
            if (_usedTextPos.Count > maxUsedPosLength) _usedTextPos.RemoveAt(_usedTextPos.Count - 1);

            newPosition.z = Random.Range(0, zRange);

            return offset + newPosition;
        }

        private Vector3 Update2DTextPosition(float textWidth, float textHeight)
        {
            Vector3 direction = new Vector2((Random.value * (Random.value < 0.5f ? -1 : 1)) - 0.5f, (0.5f + Random.value * 2) * (Random.value < 0.5f ? -1 : 1)).normalized;
            Vector3 newPosition;

            // 기존 위치에서 무작위 방향 & 거리로 이동
            newPosition = _lastTextPos + direction * (minDistance + Random.Range(0, randomOffset));

            float screenWidth = text2DParent.rect.width;
            float screenHeight = text2DParent.rect.height;

            float paddingX = screenWidth * 0.05f;
            float paddingY = screenHeight * 0.1f;

            var minX = text2DParent.rect.xMin + paddingX;
            var maxX = text2DParent.rect.xMax - paddingX;
            var minY = text2DParent.rect.yMin + paddingY;
            var maxY = text2DParent.rect.yMax - paddingY;

            var finalMinX = minX;
            var finalMaxX = Mathf.Max(minX, maxX - textWidth);
            var finalMinY = minY + textHeight / 2;
            var finalMaxY = maxY - textHeight / 2;

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