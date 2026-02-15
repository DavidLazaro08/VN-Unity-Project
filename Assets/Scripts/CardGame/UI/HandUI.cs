using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGame.Core;

namespace CardGame.UI
{
    /// <summary>
    /// Gestiona la visualización de la mano del jugador
    /// </summary>
    public class HandUI : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private GameObject cardUIPrefab;
        [SerializeField] private Transform handContainer;
        [SerializeField] private float cardSpacing = 150f;
        [SerializeField] private float maxSpread = 30f; // Ángulo máximo de abanico
        [SerializeField] private bool useFanLayout = true;

        // Lista de cartas visuales en la mano
        private List<GameObject> cardVisuals = new List<GameObject>();
        private Dictionary<Card, GameObject> cardVisualsMap = new Dictionary<Card, GameObject>();

        private void Awake()
        {
            if (handContainer == null)
            {
                handContainer = transform;
            }
        }

        /// <summary>
        /// Añade una carta a la mano
        /// </summary>
        public void AddCard(Card card)
        {
            if (card == null) return;

            // Crear la visualización de la carta
            GameObject cardVisual = Instantiate(cardUIPrefab, handContainer);
            
            // Configurar la UI de la carta
            var cardUI = cardVisual.GetComponent<CardUI>();
            if (cardUI != null)
            {
                cardUI.SetCard(card, false);
            }

            // Guardar referencia
            cardVisuals.Add(cardVisual);
            cardVisualsMap[card] = cardVisual;

            // Reorganizar la mano
            UpdateHandLayout();

            // Animación de entrada (opcional)
            StartCoroutine(CardDrawAnimation(cardVisual));
        }

        /// <summary>
        /// Remueve una carta de la mano
        /// </summary>
        public void RemoveCard(Card card)
        {
            if (card == null) return;

            if (cardVisualsMap.ContainsKey(card))
            {
                GameObject visual = cardVisualsMap[card];
                cardVisuals.Remove(visual);
                cardVisualsMap.Remove(card);

                Destroy(visual);

                // Reorganizar la mano
                UpdateHandLayout();
            }
        }

        /// <summary>
        /// Limpia todas las cartas de la mano
        /// </summary>
        public void ClearHand()
        {
            foreach (GameObject visual in cardVisuals)
            {
                if (visual != null)
                    Destroy(visual);
            }

            cardVisuals.Clear();
            cardVisualsMap.Clear();
        }

        /// <summary>
        /// Actualiza el layout de la mano
        /// </summary>
        private void UpdateHandLayout()
        {
            int cardCount = cardVisuals.Count;
            if (cardCount == 0) return;

            if (useFanLayout)
            {
                UpdateFanLayout();
            }
            else
            {
                UpdateLinearLayout();
            }
        }

        /// <summary>
        /// Layout lineal (cartas en fila)
        /// </summary>
        private void UpdateLinearLayout()
        {
            int cardCount = cardVisuals.Count;
            float totalWidth = (cardCount - 1) * cardSpacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < cardCount; i++)
            {
                if (cardVisuals[i] != null)
                {
                    RectTransform rect = cardVisuals[i].GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        Vector2 targetPos = new Vector2(startX + i * cardSpacing, 0);
                        rect.anchoredPosition = targetPos;
                        rect.rotation = Quaternion.identity;
                    }
                }
            }
        }

        /// <summary>
        /// Layout en abanico
        /// </summary>
        private void UpdateFanLayout()
        {
            int cardCount = cardVisuals.Count;
            float angleStep = cardCount > 1 ? maxSpread / (cardCount - 1) : 0;
            float startAngle = -maxSpread / 2f;

            float totalWidth = (cardCount - 1) * cardSpacing;
            float startX = -totalWidth / 2f;

            // Radio del arco
            float radius = 500f;

            for (int i = 0; i < cardCount; i++)
            {
                if (cardVisuals[i] != null)
                {
                    RectTransform rect = cardVisuals[i].GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        float angle = startAngle + angleStep * i;
                        float angleRad = angle * Mathf.Deg2Rad;

                        // Posición en el arco
                        float x = startX + i * cardSpacing;
                        float y = -Mathf.Abs(x) * 0.1f; // Curvatura sutil

                        Vector2 targetPos = new Vector2(x, y);
                        Quaternion targetRot = Quaternion.Euler(0, 0, angle);

                        rect.anchoredPosition = targetPos;
                        rect.rotation = targetRot;

                        // Hacer que la carta del centro esté más adelante
                        int siblingIndex = Mathf.Abs(i - cardCount / 2);
                        rect.SetSiblingIndex(cardCount - siblingIndex - 1);
                    }
                }
            }
        }

        /// <summary>
        /// Animación de robar carta
        /// </summary>
        private IEnumerator CardDrawAnimation(GameObject card)
        {
            if (card == null) yield break;

            RectTransform rect = card.GetComponent<RectTransform>();
            CanvasGroup canvasGroup = card.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup = card.AddComponent<CanvasGroup>();
            }

            // Empezar desde arriba y transparente
            Vector2 finalPos = rect.anchoredPosition;
            Vector2 startPos = finalPos + Vector2.up * 500f;

            rect.anchoredPosition = startPos;
            canvasGroup.alpha = 0f;

            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                rect.anchoredPosition = Vector2.Lerp(startPos, finalPos, t);
                canvasGroup.alpha = t;

                yield return null;
            }

            rect.anchoredPosition = finalPos;
            canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// Obtiene la visualización de una carta
        /// </summary>
        public GameObject GetCardVisual(Card card)
        {
            if (cardVisualsMap.ContainsKey(card))
            {
                return cardVisualsMap[card];
            }
            return null;
        }

        /// <summary>
        /// Actualiza todas las cartas en la mano
        /// </summary>
        public void RefreshHand(List<Card> cards)
        {
            ClearHand();

            foreach (Card card in cards)
            {
                AddCard(card);
            }
        }
    }
}
