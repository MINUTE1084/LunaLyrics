using System.Collections;
using System.Collections.Generic;
using LunaLyrics.Assets.Scripts;
using TMPro;
using UnityEngine;


namespace LunaLyrics.Assets.Scripts
{
    public class TextAnimator : MonoBehaviour
    {
        public float updateInterval = 0.2f;
        public float positionJitterAmount = 0.005f;
        public float scaleJitterAmount = 0.002f;
        public float disappearDelay = 2.5f;

        public float startYPos = 3f;
        public float randomYPosOffset = 0.25f;
        public int randomYPosLevel = 3;


        public float minDistance = 2.0f;
        public float randomOffset = 1.0f;

        public TMP_Text textMeshPro;
        public Animator animator;
        public CanvasGroup canvasGroup;


        private float timer = 0f;
        private float randomYPos;

        private Coroutine typingCoroutine;
        private bool isTyping;
        private float delayPerChar;
        private float targetInterval;

        public static Vector3 lastTextPos;
        private static float minX, maxX, minY, maxY;
        private static bool posInit;

        private RectTransform rectTransform;

        void Awake()
        {
            if (posInit) return;
            rectTransform = GetComponent<RectTransform>();

            RectTransform parentRect = rectTransform.parent.GetComponent<RectTransform>();
            float screenWidth = parentRect.rect.width;
            float screenHeight = parentRect.rect.height;

            float paddingX = screenWidth * 0.05f;
            float paddingY = screenHeight * 0.1f;

            minX = parentRect.rect.xMin + paddingX;
            maxX = parentRect.rect.xMax - paddingX;
            minY = parentRect.rect.yMin + paddingY;
            maxY = parentRect.rect.yMax - paddingY;

            lastTextPos = Vector3.zero;
        }

        void OnEnable()
        {
            canvasGroup.alpha = 1;
            animator.enabled = false;
            textMeshPro.maxVisibleCharacters = 0;
        }
        public void SetText(LyricLine line)
        {
            animator.enabled = false;
            canvasGroup.alpha = 1;
            textMeshPro.rectTransform.anchoredPosition = Vector2.zero;

            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            typingCoroutine = StartCoroutine(TypewriterRoutine(line.Text, Mathf.Clamp((float)line.LyricDuration * 0.8f, 0.2f, 3f)));
        }

        public void ResetText()
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            textMeshPro.text = "";
            textMeshPro.maxVisibleCharacters = 0;
        }

        private IEnumerator TypewriterRoutine(string text, float duration)
        {
            isTyping = true;

            textMeshPro.text = text;
            textMeshPro.maxVisibleCharacters = text.Length;
            textMeshPro.ForceMeshUpdate();
            var textWidth = textMeshPro.preferredWidth;
            var textHeight = textMeshPro.preferredHeight;
            textMeshPro.maxVisibleCharacters = 0;
            textMeshPro.ForceMeshUpdate();

            Vector3 direction = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), 0).normalized;
            Vector3 newPosition = lastTextPos + direction * (minDistance + UnityEngine.Random.Range(0, randomOffset));

            if (newPosition.x < minX) newPosition.x = maxX - newPosition.x;
            if (newPosition.x > maxX) newPosition.x -= maxX;
            if (newPosition.y < minY) newPosition.y = maxY - newPosition.x;
            if (newPosition.y > maxY) newPosition.y -= maxY;

            newPosition.x = Mathf.Clamp(newPosition.x, minX, Mathf.Max(minX, maxX - textWidth));
            newPosition.y = Mathf.Clamp(newPosition.y, minY + textHeight / 2, maxY - textHeight / 2);

            lastTextPos = newPosition;
            rectTransform.anchoredPosition = lastTextPos;

            randomYPos = (startYPos + (randomYPosOffset * Random.Range(1, randomYPosLevel))) * (Random.value < 0.5f ? -1 : 1);

            var viewpoint = Camera.main.WorldToViewportPoint(transform.position);
            if (viewpoint.y < 0.25f) randomYPos = Mathf.Abs(randomYPos);
            if (viewpoint.y > 0.75f) randomYPos = -Mathf.Abs(randomYPos);

            yield return null;

            delayPerChar = duration / text.Length;
            targetInterval = Mathf.Max(delayPerChar, updateInterval);

            for (int i = 1; i <= text.Length; i++)
            {
                textMeshPro.maxVisibleCharacters = i;
                ApplyJitterToVisibleCharacters();
                yield return new WaitForSeconds(delayPerChar);
            }

            isTyping = false;

            yield return new WaitForSeconds(disappearDelay);

            animator.enabled = true;
        }

        void Update()
        {
            timer += Time.deltaTime;
            if (timer >= targetInterval)
            {
                timer = 0f;
                if (!isTyping) ApplyJitterToVisibleCharacters();
            }
        }

        void ApplyJitterToVisibleCharacters()
        {
            textMeshPro.ForceMeshUpdate();
            var textInfo = textMeshPro.textInfo;

            int visibleCharacterCount = textMeshPro.maxVisibleCharacters;
            if (visibleCharacterCount == 0) return;

            for (int i = 0; i < visibleCharacterCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                var meshInfo = textInfo.meshInfo[charInfo.materialReferenceIndex];
                var vertices = meshInfo.vertices;

                Vector3 positionOffset = new Vector3(
                    Random.Range(-positionJitterAmount, positionJitterAmount),
                    Random.Range(-positionJitterAmount, positionJitterAmount) + randomYPos * i,
                    0);
                float scaleMultiplier = 1.0f + Random.Range(-scaleJitterAmount, scaleJitterAmount);

                Vector3 center = (vertices[charInfo.vertexIndex + 0] + vertices[charInfo.vertexIndex + 2]) / 2f;

                for (int j = 0; j < 4; j++)
                {
                    int vertexIndex = charInfo.vertexIndex + j;
                    Vector3 originalVertex = vertices[vertexIndex] - center;
                    Vector3 modifiedVertex = (originalVertex * scaleMultiplier) + center + positionOffset;
                    vertices[vertexIndex] = modifiedVertex;
                }
            }

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
    }
}
