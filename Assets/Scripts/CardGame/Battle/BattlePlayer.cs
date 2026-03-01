using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGame.Core;

namespace CardGame.Battle
{
    /// <summary>
    /// Representa a un jugador en la batalla (humano o IA)
    /// </summary>
    public class BattlePlayer
    {
        // Información del jugador
        public string PlayerName { get; private set; }
        public bool IsAI { get; private set; }

        // Recursos
        public int MaxMana { get; private set; }
        public int CurrentMana { get; private set; }
        public int PlayerHealth { get; private set; }
        public int MaxPlayerHealth { get; private set; }

        // Cartas
        public List<Card> Deck { get; private set; }
        public List<Card> Hand { get; private set; }
        public List<Card> Field { get; private set; }
        public List<Card> Graveyard { get; private set; }

        // Configuración
        public const int MAX_HAND_SIZE = 10;
        public const int MAX_FIELD_SIZE = 7;
        public const int STARTING_HAND_SIZE = 4;

        // Eventos
        public event Action<int> OnManaChanged;
        public event Action<int> OnHealthChanged;
        public event Action<Card> OnCardDrawn;
        public event Action<Card> OnCardPlayed;
        public event Action<Card> OnCardDestroyed;

        /// <summary>
        /// Constructor del jugador
        /// </summary>
        public BattlePlayer(string name, bool isAI, List<Card> deck, int startingHealth = 30, int startingMana = 1)
        {
            PlayerName = name;
            IsAI = isAI;
            Deck = new List<Card>(deck);
            Hand = new List<Card>();
            Field = new List<Card>();
            Graveyard = new List<Card>();

            MaxPlayerHealth = startingHealth;
            PlayerHealth = startingHealth;
            MaxMana = startingMana;
            CurrentMana = startingMana;
        }

        /// <summary>
        /// Inicializa el jugador robando la mano inicial
        /// </summary>
        public void Initialize()
        {
            DrawCards(STARTING_HAND_SIZE);
            Debug.Log($"{PlayerName} inicia con {Hand.Count} cartas en mano");
        }

        /// <summary>
        /// Inicia un nuevo turno para el jugador
        /// </summary>
        public void StartTurn(int turnNumber)
        {
            // Incrementar maná máximo (hasta un límite)
            if (MaxMana < 10)
            {
                MaxMana++;
            }

            // Restaurar maná
            CurrentMana = MaxMana;
            OnManaChanged?.Invoke(CurrentMana);

            // Robar una carta
            DrawCards(1);

            // Resetear estado de ataque de las cartas en el campo
            foreach (Card card in Field)
            {
                card.ResetTurnState();
            }

            Debug.Log($"=== Turno {turnNumber}: {PlayerName} ===");
            Debug.Log($"Maná: {CurrentMana}/{MaxMana} | Vida: {PlayerHealth}/{MaxPlayerHealth}");
        }

        /// <summary>
        /// Roba cartas del mazo
        /// </summary>
        public void DrawCards(int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                if (Deck.Count == 0)
                {
                    Debug.LogWarning($"{PlayerName} no tiene más cartas en el mazo");
                    TakeDamage(1); // Fatigue damage
                    continue;
                }

                if (Hand.Count >= MAX_HAND_SIZE)
                {
                    Debug.LogWarning($"{PlayerName} tiene la mano llena, carta descartada");
                    Card discarded = Deck[0];
                    Deck.RemoveAt(0);
                    Graveyard.Add(discarded);
                    continue;
                }

                Card card = Deck[0];
                Deck.RemoveAt(0);
                Hand.Add(card);
                card.Location = CardLocation.Hand;

                OnCardDrawn?.Invoke(card);
                Debug.Log($"{PlayerName} roba {card.Data.cardName}");
            }
        }

        /// <summary>
        /// Juega una carta de la mano al campo
        /// </summary>
        public bool PlayCard(Card card, int fieldPosition = -1)
        {
            if (card == null || !Hand.Contains(card))
            {
                Debug.LogWarning("Carta inválida o no está en la mano");
                return false;
            }

            // Verificar coste de maná
            if (CurrentMana < card.Data.manaCost)
            {
                Debug.LogWarning($"No hay suficiente maná ({CurrentMana}/{card.Data.manaCost}) para jugar {card.Data.cardName}");
                return false;
            }

            // Verificar espacio en el campo
            if (Field.Count >= MAX_FIELD_SIZE)
            {
                Debug.LogWarning("El campo está lleno");
                return false;
            }

            // Consumir maná
            CurrentMana -= card.Data.manaCost;
            OnManaChanged?.Invoke(CurrentMana);

            // Mover carta de mano a campo
            Hand.Remove(card);
            Field.Add(card);
            card.Location = CardLocation.Field;

            // Establecer posición en el campo
            if (fieldPosition >= 0 && fieldPosition < Field.Count)
            {
                card.FieldPosition = fieldPosition;
            }
            else
            {
                card.FieldPosition = Field.Count - 1;
            }

            // Las cartas no pueden atacar el turno que son jugadas (summon sickness)
            card.CanAttackThisTurn = false;

            OnCardPlayed?.Invoke(card);
            Debug.Log($"{PlayerName} juega {card.Data.cardName} (Maná: {CurrentMana}/{MaxMana})");

            return true;
        }

        /// <summary>
        /// Recibe daño el jugador
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (damage < 0) damage = 0;

            PlayerHealth -= damage;
            if (PlayerHealth < 0) PlayerHealth = 0;

            OnHealthChanged?.Invoke(PlayerHealth);
            Debug.Log($"{PlayerName} recibe {damage} de daño. Vida: {PlayerHealth}/{MaxPlayerHealth}");
        }

        /// <summary>
        /// Se cura el jugador
        /// </summary>
        public void Heal(int amount)
        {
            if (amount < 0) amount = 0;

            PlayerHealth += amount;
            if (PlayerHealth > MaxPlayerHealth)
                PlayerHealth = MaxPlayerHealth;

            OnHealthChanged?.Invoke(PlayerHealth);
            Debug.Log($"{PlayerName} se cura {amount}. Vida: {PlayerHealth}/{MaxPlayerHealth}");
        }

        /// <summary>
        /// Destruye una carta del campo
        /// </summary>
        public void DestroyCard(Card card)
        {
            if (card == null || !Field.Contains(card))
            {
                Debug.LogWarning("Carta inválida o no está en el campo");
                return;
            }

            Field.Remove(card);
            Graveyard.Add(card);
            card.Location = CardLocation.Graveyard;

            OnCardDestroyed?.Invoke(card);
            Debug.Log($"{card.Data.cardName} de {PlayerName} ha sido destruida");

            // Reorganizar posiciones en el campo
            for (int i = 0; i < Field.Count; i++)
            {
                Field[i].FieldPosition = i;
            }
        }

        /// <summary>
        /// Verifica si el jugador está derrotado
        /// </summary>
        public bool IsDefeated()
        {
            return PlayerHealth <= 0;
        }

        /// <summary>
        /// Obtiene información del estado del jugador
        /// </summary>
        public override string ToString()
        {
            return $"{PlayerName} - HP: {PlayerHealth}/{MaxPlayerHealth}, Maná: {CurrentMana}/{MaxMana}, " +
                   $"Mano: {Hand.Count}, Campo: {Field.Count}/{MAX_FIELD_SIZE}, Mazo: {Deck.Count}";
        }
    }
}
