using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using CardGame.Core;
using CardGame.Battle;

namespace CardGame.Integration
{
    /// <summary>
    /// Script para iniciar una batalla de cartas desde la novela visual.
    /// Se puede llamar desde eventos de diálogo o triggers de escena.
    /// </summary>
    public class CardBattleStarter : MonoBehaviour
    {
        [Header("Configuración de Batalla")]
        [Tooltip("Nombre de la escena de batalla")]
        [SerializeField] private string battleSceneName = "Scene_CardBattle";

        [Tooltip("Mazo del jugador (opcional, se genera aleatoriamente si está vacío)")]
        [SerializeField] private List<string> playerDeckCardNames = new List<string>();

        [Tooltip("Nombre del oponente")]
        [SerializeField] private string opponentName = "Oponente";

        [Tooltip("Mazo del oponente (opcional, se genera aleatoriamente si está vacío)")]
        [SerializeField] private List<string> opponentDeckCardNames = new List<string>();

        [Header("Configuración de Resultados")]
        [Tooltip("Escena a la que volver después de la batalla")]
        [SerializeField] private string returnSceneName = "";

        // Configuración estática para pasar datos entre escenas
        private static BattleConfiguration currentBattleConfig;

        /// <summary>
        /// Inicia una batalla de cartas con configuración personalizada
        /// </summary>
        public void StartBattle()
        {
            StartBattle(opponentName, playerDeckCardNames, opponentDeckCardNames);
        }

        /// <summary>
        /// Inicia una batalla con parámetros específicos
        /// </summary>
        public static void StartBattle(string opponentName, List<string> playerDeck = null, List<string> opponentDeck = null, string returnScene = "")
        {
            // Guardar configuración de la batalla
            currentBattleConfig = new BattleConfiguration
            {
                opponentName = opponentName,
                playerDeckCardNames = playerDeck ?? new List<string>(),
                opponentDeckCardNames = opponentDeck ?? new List<string>(),
                returnSceneName = !string.IsNullOrEmpty(returnScene) ? returnScene : SceneManager.GetActiveScene().name
            };

            Debug.Log($"Iniciando batalla contra {opponentName}");
            
            // Cargar escena de batalla
            SceneManager.LoadScene("Scene_CardBattle");
        }

        /// <summary>
        /// Obtiene la configuración actual de la batalla
        /// </summary>
        public static BattleConfiguration GetCurrentBattleConfig()
        {
            return currentBattleConfig;
        }

        /// <summary>
        /// Retorna a la escena anterior después de la batalla
        /// </summary>
        public static void ReturnFromBattle(bool playerWon)
        {
            if (currentBattleConfig != null && !string.IsNullOrEmpty(currentBattleConfig.returnSceneName))
            {
                // Guardar resultado de la batalla
                SaveBattleResult(playerWon);

                Debug.Log($"Retornando a {currentBattleConfig.returnSceneName} - Victoria: {playerWon}");
                SceneManager.LoadScene(currentBattleConfig.returnSceneName);
            }
            else
            {
                Debug.LogWarning("No hay escena de retorno configurada");
            }
        }

        /// <summary>
        /// Guarda el resultado de la batalla (puede integrarse con el sistema de guardado)
        /// </summary>
        private static void SaveBattleResult(bool playerWon)
        {
            // Aquí puedes integrar con el sistema de guardado de tu VN
            PlayerPrefs.SetInt("LastBattleResult", playerWon ? 1 : 0);
            PlayerPrefs.SetString("LastBattleOpponent", currentBattleConfig?.opponentName ?? "Unknown");
            PlayerPrefs.Save();

            Debug.Log($"Resultado de batalla guardado: {(playerWon ? "Victoria" : "Derrota")}");
        }

        /// <summary>
        /// Obtiene el resultado de la última batalla
        /// </summary>
        public static bool GetLastBattleResult()
        {
            return PlayerPrefs.GetInt("LastBattleResult", 0) == 1;
        }

        /// <summary>
        /// Limpia la configuración de batalla
        /// </summary>
        public static void ClearBattleConfig()
        {
            currentBattleConfig = null;
        }

        // ===== Métodos de ejemplo para llamar desde el sistema de diálogo =====

        /// <summary>
        /// Ejemplo: Batalla de tutorial
        /// </summary>
        public void StartTutorialBattle()
        {
            List<string> tutorialPlayerDeck = new List<string>();
            List<string> tutorialOpponentDeck = new List<string>();
            
            // Aquí podrías definir mazos específicos para el tutorial
            
            StartBattle("Instructor de Tutorial", tutorialPlayerDeck, tutorialOpponentDeck);
        }

        /// <summary>
        /// Ejemplo: Batalla del Capítulo 1
        /// </summary>
        public void StartChapter1Battle()
        {
            StartBattle("Enemigo del Capítulo 1");
        }

        /// <summary>
        /// Ejemplo: Batalla de jefe final
        /// </summary>
        public void StartBossBattle()
        {
            StartBattle("Jefe Final");
        }
    }

    /// <summary>
    /// Configuración de una batalla
    /// </summary>
    [System.Serializable]
    public class BattleConfiguration
    {
        public string opponentName;
        public List<string> playerDeckCardNames;
        public List<string> opponentDeckCardNames;
        public string returnSceneName;
    }
}
