using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGame.Core;
using CardGame.Battle;

namespace CardGame.Integration
{
    public class BattleSceneController : MonoBehaviour
    {
        [Header("Referencias de la Escena")]
        [SerializeField] private CardBattleManager battleManager;
        [SerializeField] private CardGame.UI.BattleUI battleUI;
        [SerializeField] private CardGame.UI.HandUI playerHandUI;
        [SerializeField] private CardGame.UI.HandUI opponentHandUI;
        [SerializeField] private BattleField battleField;

        [Header("Configuración")]
        [SerializeField] private int deckSize = 20;

        private void Start()
        {
            if (battleManager == null) battleManager = FindObjectOfType<CardBattleManager>();
            if (battleUI == null) battleUI = FindObjectOfType<CardGame.UI.BattleUI>();
            if (battleField == null) battleField = FindObjectOfType<BattleField>();

            if (playerHandUI == null || opponentHandUI == null)
            {
                var hands = FindObjectsOfType<CardGame.UI.HandUI>();
                foreach (var h in hands)
                {
                    if (h != null && h.name.ToLower().Contains("player"))
                        playerHandUI = h;
                    else if (h != null && h.name.ToLower().Contains("opponent"))
                        opponentHandUI = h;
                }

                if (playerHandUI == null && hands.Length > 0) playerHandUI = hands[0];
                if (opponentHandUI == null && hands.Length > 1) opponentHandUI = hands[1];
            }

            StartCoroutine(InitializeBattle());
        }

        private IEnumerator InitializeBattle()
        {
            yield return new WaitForSeconds(0.5f);

            var config = CardBattleStarter.GetCurrentBattleConfig();

            List<Card> playerDeck;
            List<Card> opponentDeck;

            if (config != null && config.playerDeckCardNames != null && config.playerDeckCardNames.Count > 0)
                playerDeck = CardDatabase.Instance.CreateCustomDeck(config.playerDeckCardNames);
            else
                playerDeck = CardDatabase.Instance.CreateRandomDeck(deckSize);

            if (config != null && config.opponentDeckCardNames != null && config.opponentDeckCardNames.Count > 0)
                opponentDeck = CardDatabase.Instance.CreateCustomDeck(config.opponentDeckCardNames);
            else
                opponentDeck = CardDatabase.Instance.CreateRandomDeck(deckSize);

            string opponentName = config?.opponentName ?? "Oponente";

            if (battleManager == null)
            {
                Debug.LogError("No se encontró CardBattleManager en la escena.");
                yield break;
            }

            battleManager.StartBattle(playerDeck, opponentDeck, opponentName);

            battleManager.OnBattleEnded += OnBattleEnded;

            if (battleUI != null)
            {
                battleUI.Initialize(battleManager.Player, battleManager.Opponent);
            }

            battleManager.BeginBattle();

            if (battleManager.Player != null)
            {
                battleManager.Player.OnCardDrawn += OnPlayerCardDrawn;
                battleManager.Player.OnCardPlayed += OnPlayerCardPlayed;
            }

            if (battleManager.Opponent != null)
            {
                battleManager.Opponent.OnCardDrawn += OnOpponentCardDrawn;
            }

            UpdatePlayerHand();
            UpdateOpponentHand();

        }

        private void OnPlayerCardDrawn(Card card)
        {
            if (playerHandUI != null) playerHandUI.AddCard(card, false);
        }


        private void OnPlayerCardPlayed(Card card)
        {
            if (playerHandUI != null) playerHandUI.RemoveCard(card);
        }

        private void OnOpponentCardDrawn(Card card)
        {
            if (opponentHandUI != null) opponentHandUI.AddCard(card, true);
        }




        private void UpdatePlayerHand()
        {
            if (playerHandUI != null && battleManager != null && battleManager.Player != null)
                playerHandUI.RefreshHand(battleManager.Player.Hand, true);
        }

        private void OnBattleEnded()
        {
            Debug.Log("Batalla terminada");
            StartCoroutine(ReturnToVNAfterDelay(3f));
        }

        private IEnumerator ReturnToVNAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            bool playerWon = battleManager != null
                            && battleManager.Player != null
                            && !battleManager.Player.IsDefeated();
            CardBattleStarter.ReturnFromBattle(playerWon);
        }

        public void ReturnToVNImmediately()
        {
            bool playerWon = battleManager != null
                            && battleManager.Player != null
                            && !battleManager.Player.IsDefeated();
            CardBattleStarter.ReturnFromBattle(playerWon);
        }

        public void RestartBattle()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }

        private void OnDestroy()
        {
            if (battleManager != null) battleManager.OnBattleEnded -= OnBattleEnded;

            if (battleManager != null && battleManager.Player != null)
            {
                battleManager.Player.OnCardDrawn -= OnPlayerCardDrawn;
                battleManager.Player.OnCardPlayed -= OnPlayerCardPlayed;
            }

            if (battleManager != null && battleManager.Opponent != null)
            {
                battleManager.Opponent.OnCardDrawn -= OnOpponentCardDrawn;
            }
        }
        private void UpdateOpponentHand()
        {
            if (opponentHandUI != null && battleManager != null && battleManager.Opponent != null)
                opponentHandUI.RefreshHand(battleManager.Opponent.Hand, true);


        }

    }
}
