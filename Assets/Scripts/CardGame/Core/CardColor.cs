using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame.Core
{
    /// <summary>
    /// Define los tres colores de cartas y sus relaciones de ataque tipo piedra-papel-tijera.
    /// Rojo ataca Verde, Verde ataca Azul, Azul ataca Rojo.
    /// </summary>
    public enum CardColor
    {
        Red,    // Rojo ataca a Verde
        Green,  // Verde ataca a Azul
        Blue    // Azul ataca a Rojo
    }

    /// <summary>
    /// Clase de utilidad para trabajar con colores de cartas
    /// </summary>
    public static class CardColorExtensions
    {
        /// <summary>
        /// Obtiene el color que esta carta puede atacar
        /// </summary>
        public static CardColor GetTargetColor(this CardColor color)
        {
            switch (color)
            {
                case CardColor.Red:
                    return CardColor.Green;
                case CardColor.Green:
                    return CardColor.Blue;
                case CardColor.Blue:
                    return CardColor.Red;
                default:
                    return CardColor.Red;
            }
        }

        /// <summary>
        /// Verifica si esta carta puede atacar a una carta del color objetivo
        /// </summary>
        public static bool CanAttack(this CardColor attackerColor, CardColor targetColor)
        {
            return attackerColor.GetTargetColor() == targetColor;
        }

        /// <summary>
        /// Obtiene el color en formato Unity.Color para UI
        /// </summary>
        public static Color GetUIColor(this CardColor cardColor)
        {
            switch (cardColor)
            {
                case CardColor.Red:
                    return new Color(0.9f, 0.2f, 0.2f); // Rojo vibrante
                case CardColor.Green:
                    return new Color(0.2f, 0.9f, 0.4f); // Verde neón
                case CardColor.Blue:
                    return new Color(0.2f, 0.5f, 1.0f); // Azul cyberpunk
                default:
                    return Color.white;
            }
        }

        /// <summary>
        /// Obtiene el nombre localizado del color
        /// </summary>
        public static string GetLocalizedName(this CardColor color)
        {
            switch (color)
            {
                case CardColor.Red:
                    return "Rojo";
                case CardColor.Green:
                    return "Verde";
                case CardColor.Blue:
                    return "Azul";
                default:
                    return "Desconocido";
            }
        }
    }
}
