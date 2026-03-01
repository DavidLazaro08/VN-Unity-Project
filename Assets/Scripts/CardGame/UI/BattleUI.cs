using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CardGame.Battle;

namespace CardGame.UI
{
    /// <summary>
    /// UI principal de la batalla.
    /// Muestra información general: maná, vida, turno, botones
    /// </summary>
    public class BattleUI : MonoBehaviour
    {
        [Header("Player UI")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI playerHealthText;
        [SerializeField] private TextMeshProUGUI playerManaText;
        [SerializeField] private Slider playerHealthBar;
        [SerializeField] private Image playerHealthBarFill;

        [Header("Opponent UI")]
        [SerializeField] private TextMeshProUGUI opponentNameText;
        [SerializeField] private TextMeshProUGUI opponentHealthText;
        [SerializeField] private TextMeshProUGUI opponentManaText;
        [SerializeField] private Slider opponentHealthBar;
        [SerializeField] private Image opponentHealthBarFill;

        [Header("Turn Info")]
        [SerializeField] private TextMeshProUGUI turnNumberText;
        [SerializeField] private TextMeshProUGUI turnPlayerText;
        [SerializeField] private GameObject playerTurnIndicator;
        [SerializeField] private GameObject opponentTurnIndicator;

        [Header("Buttons")]
        [SerializeField] private Button endTurnButton;
        [SerializeField] private TextMeshProUGUI endTurnButtonText;

        [Header("Messages")]
        [SerializeField] private GameObject messagePanel;
        [SerializeField] private TextMeshProUGUI messageText;

        [Header("Victory/Defeat")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;
        [SerializeField] private Button continueButton;

        [Header("Rules")]
        [SerializeField] private GameObject rulesPanel;
        [SerializeField] private TextMeshProUGUI rulesText;

        private BattlePlayer player;
        private BattlePlayer opponent;

        private void Start()
        {
            // Configurar botón de finalizar turno
            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
            }

            // Suscribirse a eventos del battle manager
            if (CardBattleManager.Instance != null)
            {
                SubscribeToBattleEvents();
            }

            // Ocultar paneles
            if (messagePanel != null) messagePanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);
            if (rulesPanel != null) rulesPanel.SetActive(false);
        }

        /// <summary>
        /// Inicializa la UI con los jugadores
        /// </summary>
        public void Initialize(BattlePlayer playerData, BattlePlayer opponentData)
        {
            player = playerData;
            opponent = opponentData;

            // Configurar nombres
            if (playerNameText != null)
                playerNameText.text = player.PlayerName;

            if (opponentNameText != null)
                opponentNameText.text = opponent.PlayerName;

            // Configurar barras de vida máxima
            if (playerHealthBar != null)
            {
                playerHealthBar.maxValue = player.MaxPlayerHealth;
                playerHealthBar.value = player.PlayerHealth;
            }

            if (opponentHealthBar != null)
            {
                opponentHealthBar.maxValue = opponent.MaxPlayerHealth;
                opponentHealthBar.value = opponent.PlayerHealth;
            }

            // Suscribirse a eventos de los jugadores
            player.OnManaChanged += UpdatePlayerMana;
            player.OnHealthChanged += UpdatePlayerHealth;

            opponent.OnManaChanged += UpdateOpponentMana;
            opponent.OnHealthChanged += UpdateOpponentHealth;

            // Actualizar UI inicial
            UpdatePlayerMana(player.CurrentMana);
            UpdatePlayerHealth(player.PlayerHealth);
            UpdateOpponentMana(opponent.CurrentMana);
            UpdateOpponentHealth(opponent.PlayerHealth);
        }

        /// <summary>
        /// Suscribe a los eventos del battle manager
        /// </summary>
        private void SubscribeToBattleEvents()
        {
            CardBattleManager.Instance.OnTurnStarted += OnTurnStarted;
            CardBattleManager.Instance.OnTurnEnded += OnTurnEnded;
            CardBattleManager.Instance.OnBattleWon += OnBattleWon;
            CardBattleManager.Instance.OnBattleLost += OnBattleLost;
        }

        /// <summary>
        /// Actualiza el maná del jugador
        /// </summary>
        private void UpdatePlayerMana(int mana)
        {
            if (playerManaText != null && player != null)
                playerManaText.text = $"{mana}/{player.MaxMana}";
        }

        /// <summary>
        /// Actualiza la vida del jugador
        /// </summary>
        private void UpdatePlayerHealth(int health)
        {
            if (playerHealthText != null)
                playerHealthText.text = health.ToString();

            if (playerHealthBar != null)
                playerHealthBar.value = health;

            // Cambiar color de la barra según el porcentaje de vida
            if (playerHealthBarFill != null && player != null)
            {
                float healthPercent = (float)health / player.MaxPlayerHealth;
                playerHealthBarFill.color = Color.Lerp(Color.red, Color.green, healthPercent);
            }
        }

        /// <summary>
        /// Actualiza el maná del oponente
        /// </summary>
        private void UpdateOpponentMana(int mana)
        {
            if (opponentManaText != null && opponent != null)
                opponentManaText.text = $"{mana}/{opponent.MaxMana}";
        }

        /// <summary>
        /// Actualiza la vida del oponente
        /// </summary>
        private void UpdateOpponentHealth(int health)
        {
            if (opponentHealthText != null)
                opponentHealthText.text = health.ToString();

            if (opponentHealthBar != null)
                opponentHealthBar.value = health;

            // Cambiar color de la barra según el porcentaje de vida
            if (opponentHealthBarFill != null && opponent != null)
            {
                float healthPercent = (float)health / opponent.MaxPlayerHealth;
                opponentHealthBarFill.color = Color.Lerp(Color.red, Color.green, healthPercent);
            }
        }

        /// <summary>
        /// Callback cuando inicia un turno
        /// </summary>
        private void OnTurnStarted(BattlePlayer turnPlayer)
        {
            if (turnPlayer == null || player == null || opponent == null)
            {
                Debug.LogError("BattleUI no inicializado (player/opponent/turnPlayer null). ¿Llamaste a battleUI.Initialize(player, opponent)?");
                return;
            }

            if (turnNumberText != null && CardBattleManager.Instance != null)
                turnNumberText.text = $"Turno {CardBattleManager.Instance.TurnNumber}";

            bool isPlayerTurn = (turnPlayer == player);

            if (turnPlayerText != null)
                turnPlayerText.text = isPlayerTurn ? "Tu Turno" : $"Turno de {opponent.PlayerName}";

            if (playerTurnIndicator != null) playerTurnIndicator.SetActive(isPlayerTurn);
            if (opponentTurnIndicator != null) opponentTurnIndicator.SetActive(!isPlayerTurn);

            if (endTurnButton != null) endTurnButton.interactable = isPlayerTurn;

            ShowMessage(isPlayerTurn ? "¡Tu turno!" : $"Turno de {opponent.PlayerName}", 1.5f);
        }


        /// <summary>
        /// Callback cuando termina un turno
        /// </summary>
        private void OnTurnEnded(BattlePlayer turnPlayer)
        {
            // Animaciones o efectos de fin de turno
        }

        /// <summary>
        /// Callback cuando el jugador gana
        /// </summary>
        private void OnBattleWon(BattlePlayer winner)
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }

            ShowMessage("¡VICTORIA!", 0f);
        }

        /// <summary>
        /// Callback cuando el jugador pierde
        /// </summary>
        private void OnBattleLost(BattlePlayer loser)
        {
            if (defeatPanel != null)
            {
                defeatPanel.SetActive(true);
            }

            ShowMessage("DERROTA", 0f);
        }

        /// <summary>
        /// Muestra un mensaje temporal
        /// </summary>
        public void ShowMessage(string message, float duration = 2f)
        {
            if (messagePanel == null || messageText == null) return;

            messageText.text = message;
            messagePanel.SetActive(true);

            if (duration > 0f)
            {
                StartCoroutine(HideMessageAfterDelay(duration));
            }
        }

        /// <summary>
        /// Oculta el mensaje después de un retraso
        /// </summary>
        private IEnumerator HideMessageAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (messagePanel != null)
                messagePanel.SetActive(false);
        }

        /// <summary>
        /// Callback del botón de finalizar turno
        /// </summary>
        private void OnEndTurnButtonClicked()
        {
            if (CardBattleManager.Instance != null)
            {
                CardBattleManager.Instance.EndTurn();
            }
        }

        /// <summary>
        /// Muestra el panel de reglas
        /// </summary>
        public void ShowRules()
        {
            if (rulesPanel != null && rulesText != null)
            {
                rulesText.text = CardGame.Battle.CardAttackRules.GetFullRulesText();
                rulesPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Oculta el panel de reglas
        /// </summary>
        public void HideRules()
        {
            if (rulesPanel != null)
                rulesPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            // Desuscribirse de eventos
            if (player != null)
            {
                player.OnManaChanged -= UpdatePlayerMana;
                player.OnHealthChanged -= UpdatePlayerHealth;
            }

            if (opponent != null)
            {
                opponent.OnManaChanged -= UpdateOpponentMana;
                opponent.OnHealthChanged -= UpdateOpponentHealth;
            }

            if (CardBattleManager.Instance != null)
            {
                CardBattleManager.Instance.OnTurnStarted -= OnTurnStarted;
                CardBattleManager.Instance.OnTurnEnded -= OnTurnEnded;
                CardBattleManager.Instance.OnBattleWon -= OnBattleWon;
                CardBattleManager.Instance.OnBattleLost -= OnBattleLost;
            }
        }
    }
}
