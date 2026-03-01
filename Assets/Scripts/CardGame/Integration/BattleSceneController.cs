using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGame.Core;
using CardGame.Battle;

namespace CardGame.Integration
{
    /// <summary>
    /// Controlador de la escena de batalla.
    /// Inicializa la batalla con la configuración recibida y gestiona el flujo completo.
    /// </summary>
    public class BattleSceneController : MonoBehaviour
    {
        [Header("Referencias de la Escena")]
        [SerializeField] private CardBattleManager battleManager;
        [SerializeField] private UI.BattleUI battleUI;
        [SerializeField] private UI.HandUI playerHandUI;
        [SerializeField] private UI.HandUI opponentHandUI;
        [SerializeField] private BattleField battleField;

        [Header("Configuración")]
        [SerializeField] private int deckSize = 20;

        private void Start()
        {
            // Buscar referencias si no están asignadas
            if (battleManager == null)
                battleManager = FindObjectOfType<CardBattleManager>();

            if (battleUI == null)
                battleUI = FindObjectOfType<UI.BattleUI>();

            if (playerHandUI == null)
            {
                var handUIs = FindObjectsOfType<UI.HandUI>();
                if (handUIs.Length > 0) playerHandUI = handUIs[0];
            }

            if (battleField == null)
                battleField = FindObjectOfType<BattleField>();

            // Iniciar la batalla
            StartCoroutine(InitializeBattle());
        }

        /// <summary>
        /// Inicializa la batalla con la configuración
        /// </summary>
        private IEnumerator InitializeBattle()
        {
            // Pequeña pausa para que cargue la escena
            yield return new WaitForSeconds(0.5f);

            // Obtener la configuración de la batalla
            BattleConfiguration config = CardBattleStarter.GetCurrentBattleConfig();

            List<Card> playerDeck;
            List<Card> opponentDeck;

            // Crear mazos
            if (config != null && config.playerDeckCardNames != null && config.playerDeckCardNames.Count > 0)
            {
                playerDeck = CardDatabase.Instance.CreateCustomDeck(config.playerDeckCardNames);
            }
            else
            {
                playerDeck = CardDatabase.Instance.CreateRandomDeck(deckSize);
                Debug.Log("Usando mazo aleatorio para el jugador");
            }

            if (config != null && config.opponentDeckCardNames != null && config.opponentDeckCardNames.Count > 0)
            {
                opponentDeck = CardDatabase.Instance.CreateCustomDeck(config.opponentDeckCardNames);
            }
            else
            {
                opponentDeck = CardDatabase.Instance.CreateRandomDeck(deckSize);
                Debug.Log("Usando mazo aleatorio para el oponente");
            }

            string opponentName = config?.opponentName ?? "Oponente";

            // Iniciar batalla
            if (battleManager != null)
            {
                battleManager.StartBattle(playerDeck, opponentDeck, opponentName);

                // Suscribirse a eventos de batalla
                battleManager.OnBattleEnded += OnBattleEnded;

                // Inicializar UI
                if (battleUI != null)
                {
                    battleUI.Initialize(battleManager.Player, battleManager.Opponent);
                }

                // Suscribirse a eventos de los jugadores para actualizar la mano
                battleManager.Player.OnCardDrawn += OnPlayerCardDrawn;
                battleManager.Player.OnCardPlayed += OnPlayerCardPlayed;

                battleManager.Opponent.OnCardDrawn += OnOpponentCardDrawn;
                battleManager.Opponent.OnCardPlayed += OnOpponentCardPlayed;

                // Mostrar manos iniciales
                UpdatePlayerHand();
                UpdateOpponentHand();
            }
            else
            {
                Debug.LogError("No se encontró CardBattleManager en la escena");
            }
        }

        /// <summary>
        /// Callback cuando el jugador roba una carta
        /// </summary>
        private void OnPlayerCardDrawn(Card card)
        {
            if (playerHandUI != null)
            {
                playerHandUI.AddCard(card);
            }
        }

        /// <summary>
        /// Callback cuando el jugador juega una carta
        /// </summary>
        private void OnPlayerCardPlayed(Card card)
        {
            if (playerHandUI != null)
            {
                playerHandUI.RemoveCard(card);
            }
        }

        /// <summary>
        /// Callback cuando el oponente roba una carta
        /// </summary>
        private void OnOpponentCardDrawn(Card card)
        {
            if (opponentHandUI != null)
            {
                opponentHandUI.AddCard(card);
            }
        }

        /// <summary>
        /// Callback cuando el oponente (IA) juega una carta
        /// </summary>
        private void OnOpponentCardPlayed(Card card)
        {
            // Quitar de la mano visual del oponente
            if (opponentHandUI != null)
            {
                opponentHandUI.RemoveCard(card);
            }

            // Añadir al campo visual del oponente (isPlayerCard = false)
            if (battleField != null)
            {
                battleField.AddCardToField(card, false);
            }
        }

        /// <summary>
        /// Actualiza la mano del jugador
        /// </summary>
        private void UpdatePlayerHand()
        {
            if (playerHandUI != null && battleManager != null && battleManager.Player != null)
            {
                playerHandUI.RefreshHand(battleManager.Player.Hand);
            }
        }

        /// <summary>
        /// Actualiza la mano inicial del oponente
        /// </summary>
        private void UpdateOpponentHand()
        {
            if (opponentHandUI != null && battleManager != null && battleManager.Opponent != null)
            {
                opponentHandUI.RefreshHand(battleManager.Opponent.Hand);
            }
        }

        /// <summary>
        /// Callback cuando termina la batalla
        /// </summary>
        private void OnBattleEnded()
        {
            Debug.Log("Batalla terminada");

            // Esperar un poco antes de volver a la escena anterior
            StartCoroutine(ReturnToVNAfterDelay(3f));
        }

        /// <summary>
        /// Retorna a la VN después de un delay
        /// </summary>
        private IEnumerator ReturnToVNAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            bool playerWon = battleManager != null && !battleManager.Player.IsDefeated();
            CardBattleStarter.ReturnFromBattle(playerWon);
        }

        /// <summary>
        /// Llamado desde un botón UI para volver inmediatamente
        /// </summary>
        public void ReturnToVNImmediately()
        {
            bool playerWon = battleManager != null && !battleManager.Player.IsDefeated();
            CardBattleStarter.ReturnFromBattle(playerWon);
        }

        /// <summary>
        /// Llamado desde un botón UI para reiniciar la batalla
        /// </summary>
        public void RestartBattle()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }

        private void OnDestroy()
        {
            // Limpiar suscripciones
            if (battleManager != null)
            {
                battleManager.OnBattleEnded -= OnBattleEnded;

                if (battleManager.Player != null)
                {
                    battleManager.Player.OnCardDrawn -= OnPlayerCardDrawn;
                    battleManager.Player.OnCardPlayed -= OnPlayerCardPlayed;
                }

                if (battleManager.Opponent != null)
                {
                    battleManager.Opponent.OnCardDrawn -= OnOpponentCardDrawn;
                    battleManager.Opponent.OnCardPlayed -= OnOpponentCardPlayed;
                }
            }
        }
    }
}
