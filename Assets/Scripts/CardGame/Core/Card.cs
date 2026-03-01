using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame.Core
{
    /// <summary>
    /// Representa una instancia de carta en el juego con su estado actual.
    /// Esta clase envuelve un CardData y mantiene el estado dinámico de la carta durante la batalla.
    /// </summary>
    [System.Serializable]
    public class Card
    {
        // Referencia a los datos base de la carta
        public CardData Data { get; private set; }

        // Estado dinámico de la carta
        public int CurrentHealth { get; private set; }
        public int CurrentDamage { get; private set; }
        public bool HasAttackedThisTurn { get; set; }
        public bool CanAttackThisTurn { get; set; }
        
        // ID único para esta instancia de carta
        public string InstanceId { get; private set; }

        // Estado de la carta
        public CardLocation Location { get; set; }
        public int FieldPosition { get; set; } // Posición en el campo (0-6)

        // Eventos
        public event Action<Card> OnCardDestroyed;
        public event Action<Card, int> OnHealthChanged;
        public event Action<Card, int> OnDamageChanged;

        /// <summary>
        /// Constructor de la carta
        /// </summary>
        public Card(CardData cardData)
        {
            if (cardData == null)
            {
                Debug.LogError("No se puede crear una carta con CardData nulo");
                return;
            }

            Data = cardData;
            CurrentHealth = cardData.baseHealth;
            CurrentDamage = cardData.baseDamage;
            HasAttackedThisTurn = false;
            CanAttackThisTurn = false;
            Location = CardLocation.Deck;
            FieldPosition = -1;
            InstanceId = System.Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Recibe daño y reduce la vida de la carta
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (damage < 0) damage = 0;

            int previousHealth = CurrentHealth;
            CurrentHealth -= damage;

            OnHealthChanged?.Invoke(this, CurrentHealth);

            if (CurrentHealth <= 0)
            {
                CurrentHealth = 0;
                Die();
            }

            Debug.Log($"{Data.cardName} recibe {damage} de daño. Vida: {previousHealth} -> {CurrentHealth}");
        }

        /// <summary>
        /// Ataca a otra carta
        /// </summary>
        public bool Attack(Card target)
        {
            if (target == null)
            {
                Debug.LogWarning("No se puede atacar a una carta nula");
                return false;
            }

            // Verificar que puede atacar según las reglas de color
            if (!Data.cardColor.CanAttack(target.Data.cardColor))
            {
                Debug.LogWarning($"{Data.cardName} ({Data.cardColor}) no puede atacar a {target.Data.cardName} ({target.Data.cardColor})");
                return false;
            }

            if (!CanAttackThisTurn || HasAttackedThisTurn)
            {
                Debug.LogWarning($"{Data.cardName} no puede atacar este turno");
                return false;
            }

            // Realizar el ataque
            Debug.Log($"{Data.cardName} ataca a {target.Data.cardName} por {CurrentDamage} de daño");
            target.TakeDamage(CurrentDamage);
            HasAttackedThisTurn = true;

            return true;
        }

        /// <summary>
        /// Cura la carta
        /// </summary>
        public void Heal(int amount)
        {
            if (amount < 0) amount = 0;

            int previousHealth = CurrentHealth;
            CurrentHealth += amount;

            // No puede exceder la vida máxima
            if (CurrentHealth > Data.baseHealth)
            {
                CurrentHealth = Data.baseHealth;
            }

            OnHealthChanged?.Invoke(this, CurrentHealth);
            Debug.Log($"{Data.cardName} se cura {amount}. Vida: {previousHealth} -> {CurrentHealth}");
        }

        /// <summary>
        /// Modifica el daño de la carta
        /// </summary>
        public void ModifyDamage(int modifier)
        {
            int previousDamage = CurrentDamage;
            CurrentDamage += modifier;

            if (CurrentDamage < 0) CurrentDamage = 0;

            OnDamageChanged?.Invoke(this, CurrentDamage);
            Debug.Log($"{Data.cardName} daño modificado: {previousDamage} -> {CurrentDamage}");
        }

        /// <summary>
        /// Resetea el estado de ataque al inicio del turno
        /// </summary>
        public void ResetTurnState()
        {
            HasAttackedThisTurn = false;
            CanAttackThisTurn = true;
        }

        /// <summary>
        /// La carta muere y es destruida
        /// </summary>
        private void Die()
        {
            Debug.Log($"{Data.cardName} ha sido destruida");
            Location = CardLocation.Graveyard;
            OnCardDestroyed?.Invoke(this);
        }

        /// <summary>
        /// Verifica si la carta puede atacar a un objetivo específico
        /// </summary>
        public bool CanAttackTarget(Card target)
        {
            if (target == null) return false;
            if (!CanAttackThisTurn || HasAttackedThisTurn) return false;
            return Data.cardColor.CanAttack(target.Data.cardColor);
        }

        /// <summary>
        /// Obtiene información resumida de la carta
        /// </summary>
        public override string ToString()
        {
            return $"{Data.cardName} ({Data.cardColor}) - HP: {CurrentHealth}/{Data.baseHealth}, ATK: {CurrentDamage}";
        }
    }

    /// <summary>
    /// Define las ubicaciones posibles de una carta
    /// </summary>
    public enum CardLocation
    {
        Deck,       // En el mazo
        Hand,       // En la mano del jugador
        Field,      // En el campo de batalla
        Graveyard   // En el cementerio (destruida)
    }
}
