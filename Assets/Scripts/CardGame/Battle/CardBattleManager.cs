using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGame.Core;

namespace CardGame.Battle
{
    /// <summary>
    /// Gestor principal de la batalla de cartas.
    /// Controla el flujo del juego, turnos, y el estado general de la batalla.
    /// </summary>
    public class CardBattleManager : MonoBehaviour
    {
        public static CardBattleManager Instance { get; private set; }

        [Header("Configuración de la Batalla")]
        [SerializeField] private int startingPlayerHealth = 30;
        [SerializeField] private int startingMana = 1;
        [SerializeField] private int deckSize = 20;

        [Header("Referencias")]
        [SerializeField] private BattleField battleField;

        // Jugadores
        public BattlePlayer Player { get; private set; }
        public BattlePlayer Opponent { get; private set; }
        public BattlePlayer CurrentTurnPlayer { get; private set; }

        // Estado de la batalla
        public BattleState State { get; private set; }
        public int TurnNumber { get; private set; }

        // Selección de ataque
        public Card SelectedAttackingCard { get; private set; }

        // Eventos
        public event Action<BattlePlayer> OnTurnStarted;
        public event Action<BattlePlayer> OnTurnEnded;
        public event Action<Card, Card> OnCardAttacked;
        public event Action<BattlePlayer> OnBattleWon;
        public event Action<BattlePlayer> OnBattleLost;
        public event Action OnBattleEnded;
        /// <summary>Fired when the selected attacking card changes (null = cleared)</summary>
        public event Action<Card> OnAttackSelectionChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        /// <summary>
        /// Inicia una nueva batalla
        /// </summary>
        public void StartBattle(List<Card> playerDeck = null, List<Card> opponentDeck = null, string opponentName = "Oponente")
        {
            State = BattleState.Initializing;
            TurnNumber = 0;

            // Crear mazos si no se proporcionan
            if (playerDeck == null || playerDeck.Count == 0)
            {
                playerDeck = CardDatabase.Instance.CreateRandomDeck(deckSize);
            }

            if (opponentDeck == null || opponentDeck.Count == 0)
            {
                opponentDeck = CardDatabase.Instance.CreateRandomDeck(deckSize);
            }

            // Crear jugadores
            Player = new BattlePlayer("Jugador", false, playerDeck, startingPlayerHealth, startingMana);
            Opponent = new BattlePlayer(opponentName, true, opponentDeck, startingPlayerHealth, startingMana);

            // Suscribirse a eventos de destrucción de cartas
            SubscribeToPlayerEvents(Player);
            SubscribeToPlayerEvents(Opponent);

            // Inicializar jugadores
            Player.Initialize();
            Opponent.Initialize();

            // El jugador empieza primero
            CurrentTurnPlayer = Player;
            State = BattleState.PlayerTurn;

            Debug.Log("=== BATALLA INICIADA ===");
            Debug.Log($"{Player.PlayerName} vs {Opponent.PlayerName}");

            // Comenzar el primer turno
            StartCoroutine(StartTurnCoroutine());
        }

        /// <summary>
        /// Suscribe a los eventos de un jugador
        /// </summary>
        private void SubscribeToPlayerEvents(BattlePlayer player)
        {
            player.OnCardDestroyed += (card) =>
            {
                if (battleField != null)
                {
                    battleField.RemoveCardFromField(card);
                }
            };
        }

        /// <summary>
        /// Inicia el turno del jugador actual
        /// </summary>
        private IEnumerator StartTurnCoroutine()
        {
            TurnNumber++;
            CurrentTurnPlayer.StartTurn(TurnNumber);
            OnTurnStarted?.Invoke(CurrentTurnPlayer);

            // Si es el turno de la IA, ejecutar su lógica
            if (CurrentTurnPlayer.IsAI)
            {
                yield return new WaitForSeconds(0.5f); // Pequeña pausa para que sea visible
                yield return StartCoroutine(ExecuteAITurn());
            }
        }

        /// <summary>
        /// Ejecuta el turno de la IA
        /// </summary>
        private IEnumerator ExecuteAITurn()
        {
            var ai = GetComponent<CardGame.AI.CardGameAI>();
            if (ai != null)
            {
                yield return StartCoroutine(ai.ExecuteTurn(Opponent, Player));
            }

            // Finalizar turno automáticamente
            yield return new WaitForSeconds(1f);
            EndTurn();
        }

        /// <summary>
        /// Finaliza el turno actual
        /// </summary>
        public void EndTurn()
        {
            OnTurnEnded?.Invoke(CurrentTurnPlayer);

            // Cambiar al siguiente jugador
            CurrentTurnPlayer = (CurrentTurnPlayer == Player) ? Opponent : Player;
            State = CurrentTurnPlayer.IsAI ? BattleState.OpponentTurn : BattleState.PlayerTurn;

            // Iniciar el siguiente turno
            StartCoroutine(StartTurnCoroutine());
        }

        /// <summary>
        /// El jugador juega una carta
        /// </summary>
        public bool PlayerPlayCard(Card card, int fieldPosition = -1)
        {
            if (State != BattleState.PlayerTurn)
            {
                Debug.LogWarning("No es el turno del jugador");
                return false;
            }

            if (Player.PlayCard(card, fieldPosition))
            {
                // Actualizar visualización en el campo
                if (battleField != null)
                {
                    battleField.AddCardToField(card, true);
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// El jugador ataca con una carta
        /// </summary>
        public bool PlayerAttackWithCard(Card attackingCard, Card targetCard)
        {
            if (State != BattleState.PlayerTurn)
            {
                Debug.LogWarning("No es el turno del jugador");
                return false;
            }

            if (!Player.Field.Contains(attackingCard))
            {
                Debug.LogWarning("La carta atacante no está en tu campo");
                return false;
            }

            if (!Opponent.Field.Contains(targetCard))
            {
                Debug.LogWarning("La carta objetivo no está en el campo del oponente");
                return false;
            }

            return ExecuteAttack(attackingCard, targetCard);
        }

        /// <summary>
        /// Ejecuta un ataque entre dos cartas
        /// </summary>
        public bool ExecuteAttack(Card attacker, Card target)
        {
            if (!CardAttackRules.CanAttack(attacker, target))
            {
                string reason = CardAttackRules.GetAttackInvalidReason(attacker, target);
                Debug.LogWarning($"Ataque inválido: {reason}");
                return false;
            }

            // Realizar el ataque
            if (attacker.Attack(target))
            {
                OnCardAttacked?.Invoke(attacker, target);

                // Animación de ataque
                if (battleField != null)
                {
                    StartCoroutine(battleField.AnimateAttack(attacker, target));
                }

                // Verificar si la carta objetivo fue destruida
                if (target.CurrentHealth <= 0)
                {
                    DestroyCard(target);
                }

                // Verificar condiciones de victoria
                CheckWinConditions();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Destruye una carta del campo
        /// </summary>
        private void DestroyCard(Card card)
        {
            if (Player.Field.Contains(card))
            {
                Player.DestroyCard(card);
            }
            else if (Opponent.Field.Contains(card))
            {
                Opponent.DestroyCard(card);
            }
        }

        /// <summary>
        /// Verifica las condiciones de victoria
        /// </summary>
        private void CheckWinConditions()
        {
            if (Player.IsDefeated())
            {
                EndBattle(Opponent);
            }
            else if (Opponent.IsDefeated())
            {
                EndBattle(Player);
            }
        }

        /// <summary>
        /// Finaliza la batalla
        /// </summary>
        private void EndBattle(BattlePlayer winner)
        {
            State = BattleState.Ended;

            Debug.Log($"=== BATALLA TERMINADA ===");
            Debug.Log($"¡{winner.PlayerName} ha ganado!");

            if (winner == Player)
            {
                OnBattleWon?.Invoke(Player);
            }
            else
            {
                OnBattleLost?.Invoke(Opponent);
            }

            OnBattleEnded?.Invoke();
        }

        /// <summary>
        /// Obtiene las cartas que pueden ser atacadas por una carta
        /// </summary>
        public List<Card> GetValidAttackTargets(Card attackingCard)
        {
            if (attackingCard == null) return new List<Card>();

            BattlePlayer enemyPlayer = Player.Field.Contains(attackingCard) ? Opponent : Player;
            return CardAttackRules.GetValidTargets(attackingCard, enemyPlayer.Field);
        }

        /// <summary>
        /// Resalta objetivos válidos en el campo de batalla
        /// </summary>
        public void HighlightValidTargets(Card attackingCard)
        {
            if (battleField == null) return;

            List<Card> targets = GetValidAttackTargets(attackingCard);
            battleField.HighlightValidTargets(attackingCard, targets);
        }

        /// <summary>
        /// Selecciona la carta atacante y resalta objetivos válidos
        /// </summary>
        public void SelectAttackingCard(Card card)
        {
            SelectedAttackingCard = card;
            HighlightValidTargets(card);
            OnAttackSelectionChanged?.Invoke(card);
        }

        /// <summary>
        /// Limpia la selección de ataque y los resaltados
        /// </summary>
        public void ClearAttackSelection()
        {
            SelectedAttackingCard = null;
            ClearHighlights();
            OnAttackSelectionChanged?.Invoke(null);
        }

        /// <summary>
        /// Limpia todos los resaltados
        /// </summary>
        public void ClearHighlights()
        {
            if (battleField != null)
            {
                battleField.ClearAllHighlights();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }

    /// <summary>
    /// Estados posibles de la batalla
    /// </summary>
    public enum BattleState
    {
        Initializing,
        PlayerTurn,
        OpponentTurn,
        Ended
    }
}
