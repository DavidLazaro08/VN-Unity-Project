using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CardGame.Core
{
    /// <summary>
    /// Singleton que gestiona la base de datos de todas las cartas disponibles.
    /// Carga automáticamente todas las cartas desde Resources/CardGame/Cards/
    /// </summary>
    public class CardDatabase : MonoBehaviour
    {
        private static CardDatabase _instance;
        public static CardDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("CardDatabase");
                    _instance = go.AddComponent<CardDatabase>();
                    DontDestroyOnLoad(go);
                    _instance.LoadAllCards();
                }
                return _instance;
            }
        }

        // Diccionario de todas las cartas disponibles
        private Dictionary<string, CardData> allCards = new Dictionary<string, CardData>();

        // Lista de todas las cartas
        private List<CardData> cardsList = new List<CardData>();

        /// <summary>
        /// Carga todas las cartas desde Resources
        /// </summary>
        private void LoadAllCards()
        {
            CardData[] cards = Resources.LoadAll<CardData>("CardGame/Cards");
            
            allCards.Clear();
            cardsList.Clear();

            foreach (CardData card in cards)
            {
                if (card != null && !string.IsNullOrEmpty(card.cardName))
                {
                    if (!allCards.ContainsKey(card.cardName))
                    {
                        allCards.Add(card.cardName, card);
                        cardsList.Add(card);
                    }
                    else
                    {
                        Debug.LogWarning($"Carta duplicada encontrada: {card.cardName}");
                    }
                }
            }

            Debug.Log($"CardDatabase: {allCards.Count} cartas cargadas");
        }

        /// <summary>
        /// Obtiene una carta por su nombre
        /// </summary>
        public CardData GetCard(string cardName)
        {
            if (allCards.ContainsKey(cardName))
            {
                return allCards[cardName];
            }

            Debug.LogWarning($"Carta no encontrada: {cardName}");
            return null;
        }

        /// <summary>
        /// Crea una instancia de una carta por su nombre
        /// </summary>
        public Card CreateCardInstance(string cardName)
        {
            CardData data = GetCard(cardName);
            if (data != null)
            {
                return new Card(data);
            }
            return null;
        }

        /// <summary>
        /// Obtiene todas las cartas de un color específico
        /// </summary>
        public List<CardData> GetCardsByColor(CardColor color)
        {
            return cardsList.Where(card => card.cardColor == color).ToList();
        }

        /// <summary>
        /// Obtiene todas las cartas con un coste de maná específico
        /// </summary>
        public List<CardData> GetCardsByManaCost(int manaCost)
        {
            return cardsList.Where(card => card.manaCost == manaCost).ToList();
        }

        /// <summary>
        /// Obtiene una lista de todas las cartas disponibles
        /// </summary>
        public List<CardData> GetAllCards()
        {
            return new List<CardData>(cardsList);
        }

        /// <summary>
        /// Obtiene cartas aleatorias para crear un mazo
        /// </summary>
        public List<Card> CreateRandomDeck(int deckSize = 20)
        {
            if (cardsList.Count == 0)
            {
                Debug.LogError("No hay cartas disponibles para crear un mazo");
                return new List<Card>();
            }

            List<Card> deck = new List<Card>();

            for (int i = 0; i < deckSize; i++)
            {
                CardData randomData = cardsList[Random.Range(0, cardsList.Count)];
                deck.Add(new Card(randomData));
            }

            // Mezclar el mazo
            ShuffleDeck(deck);

            Debug.Log($"Mazo aleatorio creado con {deck.Count} cartas");
            return deck;
        }

        /// <summary>
        /// Crea un mazo personalizado a partir de nombres de cartas
        /// </summary>
        public List<Card> CreateCustomDeck(List<string> cardNames)
        {
            List<Card> deck = new List<Card>();

            foreach (string cardName in cardNames)
            {
                Card card = CreateCardInstance(cardName);
                if (card != null)
                {
                    deck.Add(card);
                }
                else
                {
                    Debug.LogWarning($"No se pudo agregar la carta: {cardName}");
                }
            }

            ShuffleDeck(deck);
            Debug.Log($"Mazo personalizado creado con {deck.Count} cartas");
            return deck;
        }

        /// <summary>
        /// Mezcla un mazo de cartas
        /// </summary>
        public void ShuffleDeck(List<Card> deck)
        {
            for (int i = 0; i < deck.Count; i++)
            {
                Card temp = deck[i];
                int randomIndex = Random.Range(i, deck.Count);
                deck[i] = deck[randomIndex];
                deck[randomIndex] = temp;
            }
        }

        /// <summary>
        /// Recarga todas las cartas (útil para desarrollo)
        /// </summary>
        public void ReloadCards()
        {
            LoadAllCards();
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                LoadAllCards();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
    }
}
