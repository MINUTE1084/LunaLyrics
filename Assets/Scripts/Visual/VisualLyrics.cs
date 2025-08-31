using System;
using System.Collections;
using LunaLyrics.Data;
using LunaLyrics.Util;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;

namespace LunaLyrics.Visual
{
    [RequireComponent(typeof(RectTransform))]
    public class VisualLyrics : MonoBehaviour
    {
        [SerializeField] private TMP_Text textMeshPro;
        [SerializeField] private Animator animator;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rectTransform;

        // Jitter Variable
        private float _updateInterval;
        private float _disappearDelay;
        private Vector2 _positionJitterAmount;
        private Vector2 _scaleJitterAmount;

        // Stair Variable
        private float _minStairAmount;
        private float _stepStairAmount;
        private int _stairCount;
        private float _stairPos;

        // General Variable
        private Coroutine _typingCoroutine;
        private bool _isTyping;

        private float _timer;
        private float _delayPerChar;
        private float _targetInterval;

        private ObjectPool<VisualLyrics> _objectPool;
        private Func<float, float, Vector2> _onPositionUpdated;

        #region Init
        public void Init(ObjectPool<VisualLyrics> objectPool, Func<float, float, Vector2> onPositionUpdated)
        {
            _objectPool = objectPool;
            this._onPositionUpdated = onPositionUpdated;
        }

        public void SetJitterData(float update, float duration, Vector2 positionJitter, Vector2 scaleJitter)
        {
            _updateInterval = update;
            _disappearDelay = duration;
            _positionJitterAmount = positionJitter;
            _scaleJitterAmount = scaleJitter;
        }

        public void SetStairData(float minAmount, float stepAmount, int stairCount)
        {
            _minStairAmount = minAmount;
            _stepStairAmount = stepAmount;
            _stairCount = stairCount;
        }

        public void SetText(LyricLine line)
        {
            // 초기화 
            _timer = 0;
            _isTyping = false;

            canvasGroup.alpha = 1;
            animator.enabled = false;
            textMeshPro.maxVisibleCharacters = 0;
            textMeshPro.rectTransform.anchoredPosition = Vector2.zero;

            // 이전 타이핑 루틴 종료 후 실행
            if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
            _typingCoroutine = StartCoroutine(TypewriterRoutine(line.Text, Mathf.Clamp((float)line.LyricDuration * 0.8f, 0.2f, 3f)));
        }
        #endregion

        // 애니메이션 종료 시 실행됨
        public void ReturnObject()
        {
            _objectPool.Push(this);
        }

        #region Update
        private IEnumerator TypewriterRoutine(string text, float duration)
        {
            _isTyping = true;

            // 최대 출력 시 텍스트 길이를 가져 온 후, 화면 밖으로 나가지 않도록 보간
            textMeshPro.text = text;
            textMeshPro.maxVisibleCharacters = text.Length;
            textMeshPro.ForceMeshUpdate();
            var textWidth = textMeshPro.preferredWidth;
            var textHeight = textMeshPro.preferredHeight;
            textMeshPro.maxVisibleCharacters = 0;
            textMeshPro.ForceMeshUpdate();
            rectTransform.anchoredPosition = _onPositionUpdated.Invoke(textWidth, textHeight);

            // 기울기 각도 랜덤 지정, Y좌표에 따라 화면 밖으로 나가지 않도록 보간
            _stairPos = (_minStairAmount + (_stepStairAmount * Random.Range(1, _stairCount))) * (Random.value < 0.5f ? -1 : 1);

            var viewpoint = Camera.main.WorldToViewportPoint(transform.position);
            if (viewpoint.y < 0.25f) _stairPos = Mathf.Abs(_stairPos);
            if (viewpoint.y > 0.75f) _stairPos = -Mathf.Abs(_stairPos);

            // 프레임 대기
            yield return null;

            // 출력 딜레이 계산, 지터링 딜레이보다 길면 출력 딜레이가 지터링 딜레이가 됨
            _delayPerChar = duration / text.Length;
            _targetInterval = Mathf.Max(_delayPerChar, _updateInterval);

            for (int i = 1; i <= text.Length; i++)
            {
                // 한글자 씩 출력 + 출력 할 때마다 지터링 실행
                textMeshPro.maxVisibleCharacters = i;
                ApplyJitterToVisibleCharacters();
                yield return new WaitForSeconds(_delayPerChar);
            }

            _isTyping = false;

            yield return new WaitForSeconds(_disappearDelay);

            // 페이드아웃 애니메이션 시작
            animator.enabled = true;
        }

        private void Update()
        {
            if (_isTyping) return;

            // 타이핑 안 하고 있을 때만 지터링 자동 진행

            _timer += Time.deltaTime;
            if (_timer >= _targetInterval)
            {
                _timer = 0f;
                ApplyJitterToVisibleCharacters();
            }
        }

        private void ApplyJitterToVisibleCharacters()
        {
            textMeshPro.ForceMeshUpdate();
            var textInfo = textMeshPro.textInfo;

            // 현재 보이는 글자만 버텍스 수정
            int visibleCharacterCount = textMeshPro.maxVisibleCharacters;
            if (visibleCharacterCount == 0) return;

            for (int i = 0; i < visibleCharacterCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                var meshInfo = textInfo.meshInfo[charInfo.materialReferenceIndex];
                var vertices = meshInfo.vertices;

                Vector3 positionOffset = new(
                    Random.Range(-_positionJitterAmount.x, _positionJitterAmount.x),
                    Random.Range(-_positionJitterAmount.y, _positionJitterAmount.y) + _stairPos * i,
                    0);

                Vector3 scaleMultiplier = new(
                    1.0f + Random.Range(-_scaleJitterAmount.x, _scaleJitterAmount.x),
                    1.0f + Random.Range(-_scaleJitterAmount.y, _scaleJitterAmount.y),
                    1.0f);

                Vector3 center = (vertices[charInfo.vertexIndex + 0] + vertices[charInfo.vertexIndex + 2]) / 2f;

                // 글자의 각 버텍스에 랜덤 위치/크기 적용 (지터링 효과)
                for (int j = 0; j < 4; j++)
                {
                    int vertexIndex = charInfo.vertexIndex + j;
                    Vector3 originalVertex = vertices[vertexIndex] - center;
                    Vector3 modifiedVertex = new Vector3(originalVertex.x * scaleMultiplier.x, originalVertex.y * scaleMultiplier.y, originalVertex.z) + center + positionOffset;
                    vertices[vertexIndex] = modifiedVertex;
                }
            }

            // 버텍스 업데이트
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                var meshInfo = textInfo.meshInfo[i];
                if (meshInfo.vertexCount > 0)
                {
                    meshInfo.mesh.vertices = meshInfo.vertices;
                    textMeshPro.UpdateGeometry(meshInfo.mesh, i);
                }
            }
        }
        #endregion
    }
}
