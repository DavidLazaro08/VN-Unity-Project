using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGame.Core;

namespace CardGame.Battle
{
    /// <summary>
    /// Gestiona el campo de batalla donde se despliegan las cartas
    /// </summary>
    public class BattleField : MonoBehaviour
    {
        [Header("Configuración del Campo")]
        [Tooltip("Posiciones donde se pueden colocar las cartas del jugador")]
        public Transform[] playerCardSlots = new Transform[BattlePlayer.MAX_FIELD_SIZE];

        [Tooltip("Posiciones donde se pueden colocar las cartas del oponente")]
        public Transform[] opponentCardSlots = new Transform[BattlePlayer.MAX_FIELD_SIZE];

        [Header("Referencias")]
        public GameObject cardUIPrefab; // Prefab de la carta visual

        // Diccionario para rastrear las visualizaciones de las cartas
        private Dictionary<Card, GameObject> cardVisuals = new Dictionary<Card, GameObject>();

        /// <summary>
        /// Añade una carta visual al campo del jugador
        /// </summary>
        public GameObject AddCardToField(Card card, bool isPlayerCard)
        {
            if (card == null) return null;

            Transform[] slots = isPlayerCard ? playerCardSlots : opponentCardSlots;
            int position = card.FieldPosition;

            if (position < 0 || position >= slots.Length)
            {
                Debug.LogError($"Posición de campo inválida: {position}");
                return null;
            }

            // Crear la visualización de la carta
            GameObject cardVisual = Instantiate(cardUIPrefab, slots[position]);
            
            // Configurar la UI de la carta
            var cardUI = cardVisual.GetComponent<CardGame.UI.CardUI>();
            if (cardUI != null)
            {
                cardUI.SetCard(card, !isPlayerCard);
            }

            // Guardar referencia
            cardVisuals[card] = cardVisual;

            return cardVisual;
        }

        /// <summary>
        /// Remueve una carta visual del campo
        /// </summary>
        public void RemoveCardFromField(Card card)
        {
            if (card == null) return;

            if (cardVisuals.ContainsKey(card))
            {
                GameObject visual = cardVisuals[card];
                cardVisuals.Remove(card);

                // Animación de destrucción (opcional)
                Destroy(visual);
            }
        }

        /// <summary>
        /// Actualiza la posición visual de todas las cartas
        /// </summary>
        public void RefreshFieldLayout(List<Card> playerCards, List<Card> opponentCards)
        {
            // Limpiar visualizaciones antiguas
            foreach (var visual in cardVisuals.Values)
            {
                if (visual != null) Destroy(visual);
            }
            cardVisuals.Clear();

            // Recrear visualizaciones del jugador
            foreach (Card card in playerCards)
            {
                AddCardToField(card, true);
            }

            // Recrear visualizaciones del oponente
            foreach (Card card in opponentCards)
            {
                AddCardToField(card, false);
            }
        }

        /// <summary>
        /// Resalta las cartas que pueden ser objetivo de un ataque
        /// </summary>
        public void HighlightValidTargets(Card attackingCard, List<Card> potentialTargets)
        {
            // Primero, quitar el resaltado de todas las cartas
            ClearAllHighlights();

            if (attackingCard == null) return;

            // Obtener objetivos válidos según las reglas de color
            List<Card> validTargets = CardAttackRules.GetValidTargets(attackingCard, potentialTargets);

            // Resaltar los objetivos válidos
            foreach (Card target in validTargets)
            {
                if (cardVisuals.ContainsKey(target))
                {
                    var cardUI = cardVisuals[target].GetComponent<CardGame.UI.CardUI>();
                    if (cardUI != null)
                    {
                        cardUI.SetHighlight(true);
                    }
                }
            }
        }

        /// <summary>
        /// Quita el resaltado de todas las cartas
        /// </summary>
        public void ClearAllHighlights()
        {
            foreach (var visual in cardVisuals.Values)
            {
                if (visual != null)
                {
                    var cardUI = visual.GetComponent<CardGame.UI.CardUI>();
                    if (cardUI != null)
                    {
                        cardUI.SetHighlight(false);
                    }
                }
            }
        }

        /// <summary>
        /// Obtiene la visualización de una carta
        /// </summary>
        public GameObject GetCardVisual(Card card)
        {
            if (cardVisuals.ContainsKey(card))
            {
                return cardVisuals[card];
            }
            return null;
        }

        /// <summary>
        /// Animación de ataque entre dos cartas
        /// </summary>
        public IEnumerator AnimateAttack(Card attacker, Card target)
        {
            GameObject attackerVisual = GetCardVisual(attacker);
            GameObject targetVisual = GetCardVisual(target);

            if (attackerVisual == null || targetVisual == null)
            {
                yield break;
            }

            Vector3 originalPos = attackerVisual.transform.position;
            Vector3 targetPos = targetVisual.transform.position;

            // Mover hacia el objetivo
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                attackerVisual.transform.position = Vector3.Lerp(originalPos, targetPos, t);
                yield return null;
            }

            // Pequeña pausa en el impacto
            yield return new WaitForSeconds(0.1f);

            // Regresar a la posición original
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                attackerVisual.transform.position = Vector3.Lerp(targetPos, originalPos, t);
                yield return null;
            }

            attackerVisual.transform.position = originalPos;
        }

        private void Awake()
        {
            // Crear slots si no existen
            if (playerCardSlots[0] == null)
            {
                CreateDefaultSlots();
            }
        }

        /// <summary>
        /// Crea slots por defecto si no están asignados
        /// </summary>
        private void CreateDefaultSlots()
        {
            // Crear contenedor para slots del jugador
            GameObject playerSlotContainer = new GameObject("PlayerCardSlots");
            playerSlotContainer.transform.SetParent(transform);
            playerSlotContainer.transform.localPosition = new Vector3(0, -3, 0);

            for (int i = 0; i < BattlePlayer.MAX_FIELD_SIZE; i++)
            {
                GameObject slot = new GameObject($"PlayerSlot_{i}");
                slot.transform.SetParent(playerSlotContainer.transform);
                slot.transform.localPosition = new Vector3((i - 3) * 2, 0, 0);
                playerCardSlots[i] = slot.transform;
            }

            // Crear contenedor para slots del oponente
            GameObject opponentSlotContainer = new GameObject("OpponentCardSlots");
            opponentSlotContainer.transform.SetParent(transform);
            opponentSlotContainer.transform.localPosition = new Vector3(0, 3, 0);

            for (int i = 0; i < BattlePlayer.MAX_FIELD_SIZE; i++)
            {
                GameObject slot = new GameObject($"OpponentSlot_{i}");
                slot.transform.SetParent(opponentSlotContainer.transform);
                slot.transform.localPosition = new Vector3((i - 3) * 2, 0, 0);
                opponentCardSlots[i] = slot.transform;
            }
        }
    }
}
