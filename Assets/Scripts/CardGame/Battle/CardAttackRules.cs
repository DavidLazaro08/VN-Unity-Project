using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGame.Core;

namespace CardGame.Battle
{
    /// <summary>
    /// Clase estática que valida las reglas de ataque entre cartas según sus colores.
    /// Implementa el sistema piedra-papel-tijera: Rojo → Verde → Azul → Rojo
    /// </summary>
    public static class CardAttackRules
    {
        /// <summary>
        /// Verifica si una carta atacante puede atacar a una carta objetivo
        /// </summary>
        public static bool CanAttack(Card attacker, Card target)
        {
            if (attacker == null || target == null)
            {
                Debug.LogWarning("CardAttackRules: Carta nula en validación de ataque");
                return false;
            }

            return attacker.Data.cardColor.CanAttack(target.Data.cardColor);
        }

        /// <summary>
        /// Obtiene una descripción de por qué un ataque no es válido
        /// </summary>
        public static string GetAttackInvalidReason(Card attacker, Card target)
        {
            if (attacker == null) return "El atacante no existe";
            if (target == null) return "El objetivo no existe";

            if (!attacker.CanAttackThisTurn)
                return $"{attacker.Data.cardName} no puede atacar este turno";

            if (attacker.HasAttackedThisTurn)
                return $"{attacker.Data.cardName} ya ha atacado este turno";

            if (!attacker.Data.cardColor.CanAttack(target.Data.cardColor))
            {
                CardColor attackerColor = attacker.Data.cardColor;
                CardColor targetColor = target.Data.cardColor;
                CardColor validTarget = attackerColor.GetTargetColor();

                return $"Las cartas {attackerColor.GetLocalizedName()}s solo pueden atacar a cartas {validTarget.GetLocalizedName()}s. " +
                       $"No pueden atacar a cartas {targetColor.GetLocalizedName()}s.";
            }

            return "Ataque válido";
        }

        /// <summary>
        /// Obtiene todas las cartas objetivo válidas de una lista
        /// </summary>
        public static List<Card> GetValidTargets(Card attacker, List<Card> potentialTargets)
        {
            List<Card> validTargets = new List<Card>();

            if (attacker == null || potentialTargets == null)
                return validTargets;

            CardColor targetColor = attacker.Data.cardColor.GetTargetColor();

            foreach (Card target in potentialTargets)
            {
                if (target != null && target.Data.cardColor == targetColor)
                {
                    validTargets.Add(target);
                }
            }

            return validTargets;
        }

        /// <summary>
        /// Obtiene un texto descriptivo de las reglas de ataque para un color
        /// </summary>
        public static string GetColorRuleDescription(CardColor color)
        {
            CardColor target = color.GetTargetColor();
            return $"Las cartas {color.GetLocalizedName()}s pueden atacar a cartas {target.GetLocalizedName()}s";
        }

        /// <summary>
        /// Obtiene el texto completo de las reglas del juego
        /// </summary>
        public static string GetFullRulesText()
        {
            return "=== REGLAS DE ATAQUE ===\n\n" +
                   "• Las cartas ROJAS atacan a cartas VERDES\n" +
                   "• Las cartas VERDES atacan a cartas AZULES\n" +
                   "• Las cartas AZULES atacan a cartas ROJAS\n\n" +
                   "¡Elige sabiamente el color de tus cartas para derrotar a tu oponente!";
        }
    }
}
