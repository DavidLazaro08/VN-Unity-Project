using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CardGame.Core;
using CardGame.Battle;

namespace CardGame.AI
{
    /// <summary>
    /// IA básica para el oponente en la batalla de cartas.
    /// Implementa lógica simple: jugar cartas con suficiente maná y atacar con cartas válidas.
    /// </summary>
    [RequireComponent(typeof(CardBattleManager))]
    public class CardGameAI : MonoBehaviour
    {
        [Header("Configuración de IA")]
        [Tooltip("Tiempo de espera entre acciones (para que sea visible)")]
        [SerializeField] private float actionDelay = 0.8f;

        [Tooltip("Probabilidad de jugar una carta si tiene maná suficiente (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float playCardChance = 0.8f;

        /// <summary>
        /// Ejecuta el turno de la IA
        /// </summary>
        public IEnumerator ExecuteTurn(BattlePlayer aiPlayer, BattlePlayer humanPlayer)
        {
            Debug.Log($"=== Turno de IA: {aiPlayer.PlayerName} ===");

            // Fase 1: Jugar cartas de la mano
            yield return StartCoroutine(PlayCardsPhase(aiPlayer));

            // Fase 2: Atacar con cartas en el campo
            yield return StartCoroutine(AttackPhase(aiPlayer, humanPlayer));

            Debug.Log($"=== Fin del turno de IA ===");
        }

        /// <summary>
        /// Fase de jugar cartas de la mano
        /// </summary>
        private IEnumerator PlayCardsPhase(BattlePlayer aiPlayer)
        {
            // Intentar jugar cartas mientras tenga maná y espacio en el campo
            bool playedCard = true;

            while (playedCard && aiPlayer.Field.Count < BattlePlayer.MAX_FIELD_SIZE)
            {
                playedCard = false;

                // Obtener cartas jugables (que se puedan pagar)
                List<Card> playableCards = GetPlayableCards(aiPlayer);

                if (playableCards.Count > 0)
                {
                    // Decidir si jugar una carta (con probabilidad configurable)
                    if (Random.value < playCardChance)
                    {
                        // Seleccionar la carta a jugar (estrategia simple: la más barata primero)
                        Card cardToPlay = SelectCardToPlay(playableCards);

                        if (cardToPlay != null)
                        {
                            Debug.Log($"IA juega: {cardToPlay.Data.cardName}");
                            aiPlayer.PlayCard(cardToPlay);
                            playedCard = true;

                            yield return new WaitForSeconds(actionDelay);
                        }
                    }
                    else
                    {
                        // Decidió no jugar más cartas este turno
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Fase de ataque con cartas en el campo
        /// </summary>
        private IEnumerator AttackPhase(BattlePlayer aiPlayer, BattlePlayer humanPlayer)
        {
            // Obtener todas las cartas que pueden atacar
            List<Card> attackers = aiPlayer.Field.Where(c => c.CanAttackThisTurn && !c.HasAttackedThisTurn).ToList();

            foreach (Card attacker in attackers)
            {
                // Obtener objetivos válidos según las reglas de color
                List<Card> validTargets = CardAttackRules.GetValidTargets(attacker, humanPlayer.Field);

                if (validTargets.Count > 0)
                {
                    // Seleccionar objetivo (estrategia: atacar la carta más débil primero)
                    Card target = SelectAttackTarget(validTargets);

                    if (target != null)
                    {
                        Debug.Log($"IA ataca: {attacker.Data.cardName} -> {target.Data.cardName}");
                        
                        // Ejecutar el ataque a través del battle manager
                        if (CardBattleManager.Instance != null)
                        {
                            CardBattleManager.Instance.ExecuteAttack(attacker, target);
                        }

                        yield return new WaitForSeconds(actionDelay);
                    }
                }
                else
                {
                    // No hay objetivos válidos, podría atacar directamente al jugador (feature opcional)
                    // Por ahora, simplemente no hace nada
                    Debug.Log($"{attacker.Data.cardName} no tiene objetivos válidos");
                }
            }
        }

        /// <summary>
        /// Obtiene las cartas que se pueden jugar según el maná disponible
        /// </summary>
        private List<Card> GetPlayableCards(BattlePlayer aiPlayer)
        {
            List<Card> playableCards = new List<Card>();

            foreach (Card card in aiPlayer.Hand)
            {
                if (card.Data.manaCost <= aiPlayer.CurrentMana)
                {
                    playableCards.Add(card);
                }
            }

            return playableCards;
        }

        /// <summary>
        /// Selecciona qué carta jugar de la mano
        /// Estrategia: Jugar la carta más barata primero, luego por mayor daño
        /// </summary>
        private Card SelectCardToPlay(List<Card> playableCards)
        {
            if (playableCards.Count == 0) return null;

            // Ordenar por coste (menor primero), luego por daño (mayor primero)
            var sortedCards = playableCards
                .OrderBy(c => c.Data.manaCost)
                .ThenByDescending(c => c.CurrentDamage)
                .ToList();

            return sortedCards[0];
        }

        /// <summary>
        /// Selecciona el objetivo a atacar
        /// Estrategia: Atacar la carta con menos vida que pueda ser destruida
        /// </summary>
        private Card SelectAttackTarget(List<Card> validTargets)
        {
            if (validTargets.Count == 0) return null;

            // Estrategia 1: Si puede destruir una carta, priorizar eso
            foreach (Card target in validTargets.OrderBy(c => c.CurrentHealth))
            {
                // Si el ataque destruirá la carta, elegir esa
                // (asumimos que tenemos acceso a la carta atacante, pero esto es simplificado)
                return target;
            }

            // Estrategia 2: Atacar la carta más débil
            return validTargets.OrderBy(c => c.CurrentHealth).First();
        }

        /// <summary>
        /// Evalúa el valor de una carta (para decisiones más complejas)
        /// </summary>
        private int EvaluateCardValue(Card card)
        {
            if (card == null || card.Data == null) return 0;

            // Valor simple: suma de estadísticas
            int value = card.CurrentDamage + card.CurrentHealth;

            // Bonificación por coste bajo (cartas baratas son eficientes)
            value += (10 - card.Data.manaCost);

            return value;
        }

        /// <summary>
        /// Estrategia alternativa: IA más agresiva
        /// </summary>
        private Card SelectCardToPlayAggressive(List<Card> playableCards)
        {
            if (playableCards.Count == 0) return null;

            // Priorizar cartas con más daño
            return playableCards.OrderByDescending(c => c.CurrentDamage).First();
        }

        /// <summary>
        /// Estrategia alternativa: IA defensiva
        /// </summary>
        private Card SelectCardToPlayDefensive(List<Card> playableCards)
        {
            if (playableCards.Count == 0) return null;

            // Priorizar cartas con más vida
            return playableCards.OrderByDescending(c => c.CurrentHealth).First();
        }
    }
}
