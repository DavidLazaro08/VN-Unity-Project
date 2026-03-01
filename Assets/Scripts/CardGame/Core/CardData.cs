using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame.Core
{
    /// <summary>
    /// ScriptableObject que define las propiedades base de una carta.
    /// Permite crear cartas desde el editor de Unity sin código.
    /// </summary>
    [CreateAssetMenu(fileName = "New Card", menuName = "Card Game/Card Data")]
    public class CardData : ScriptableObject
    {
        [Header("Información Básica")]
        [Tooltip("Nombre de la carta")]
        public string cardName = "Nueva Carta";

        [Tooltip("Descripción o efecto de la carta")]
        [TextArea(3, 5)]
        public string description = "";

        [Header("Estadísticas")]
        [Tooltip("Coste de maná para jugar esta carta")]
        [Range(0, 10)]
        public int manaCost = 1;

        [Tooltip("Vida base de la carta")]
        [Range(1, 20)]
        public int baseHealth = 3;

        [Tooltip("Daño base de la carta")]
        [Range(0, 15)]
        public int baseDamage = 2;

        [Header("Color y Arte")]
        [Tooltip("Color de la carta (determina a quién puede atacar)")]
        public CardColor cardColor = CardColor.Red;

        [Tooltip("Imagen/arte de la carta")]
        public Sprite cardArt;

        [Header("Audio")]
        [Tooltip("Sonido al jugar la carta (opcional)")]
        public AudioClip playSound;

        [Tooltip("Sonido al atacar con la carta (opcional)")]
        public AudioClip attackSound;

        /// <summary>
        /// Obtiene una descripción formateada de la carta
        /// </summary>
        public string GetFormattedDescription()
        {
            return $"<b>{cardName}</b>\n" +
                   $"Coste: {manaCost} | Daño: {baseDamage} | Vida: {baseHealth}\n" +
                   $"Color: {cardColor.GetLocalizedName()}\n" +
                   $"{description}";
        }

        /// <summary>
        /// Valida que los datos de la carta sean correctos
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(cardName))
            {
                cardName = "Nueva Carta";
            }

            if (manaCost < 0) manaCost = 0;
            if (baseHealth < 1) baseHealth = 1;
            if (baseDamage < 0) baseDamage = 0;
        }
    }
}
