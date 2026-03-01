using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using CardGame.Core;
using CardGame.Battle;

namespace CardGame.UI
{
    /// <summary>
    /// Componente visual de una carta.
    /// Muestra todos los atributos y gestiona la interacción (drag & drop, click).
    /// </summary>
    public class CardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [Header("Referencias UI")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI manaCostText;
        [SerializeField] private TextMeshProUGUI damageText;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image cardArtImage;
        [SerializeField] private Image cardFrameImage;
        [SerializeField] private Image cardBackgroundImage;

        [Header("Efectos Visuales")]
        [SerializeField] private GameObject highlightEffect;
        [SerializeField] private GameObject selectedEffect;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Configuración")]
        [SerializeField] private bool isDraggable = true;
        [SerializeField] private bool isClickable = true;

        // Datos de la carta
        public Card CardData { get; private set; }
        private bool isEnemyCard = false;

        // Drag & Drop
        private Canvas canvas;
        private RectTransform rectTransform;
        private Vector3 originalPosition;
        private Transform originalParent;
        private int originalSiblingIndex;

        // Estado
        private bool isSelected = false;
        private bool isHighlighted = false;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Crear elementos UI si no existen
            CreateUIElementsIfMissing();
        }

        /// <summary>
        /// Configura la carta con sus datos
        /// </summary>
        public void SetCard(Card card, bool isEnemy = false)
        {
            CardData = card;
            isEnemyCard = isEnemy;

            UpdateVisuals();

            // Suscribirse a eventos de la carta
            card.OnHealthChanged += OnHealthChanged;
            card.OnDamageChanged += OnDamageChanged;
            card.OnCardDestroyed += OnCardDestroyed;

            // Si es carta del jugador, suscribirse al cambio de selección de ataque
            if (!isEnemy && CardBattleManager.Instance != null)
            {
                CardBattleManager.Instance.OnAttackSelectionChanged += OnAttackSelectionChangedHandler;
            }
        }

        /// <summary>
        /// Actualiza todos los elementos visuales de la carta
        /// </summary>
        private void UpdateVisuals()
        {
            if (CardData == null || CardData.Data == null) return;

            // Actualizar textos
            if (nameText != null)
                nameText.text = CardData.Data.cardName;

            if (manaCostText != null)
                manaCostText.text = CardData.Data.manaCost.ToString();

            if (damageText != null)
                damageText.text = CardData.CurrentDamage.ToString();

            if (healthText != null)
                healthText.text = CardData.CurrentHealth.ToString();

            if (descriptionText != null)
                descriptionText.text = CardData.Data.description;

            // Actualizar imagen
            if (cardArtImage != null && CardData.Data.cardArt != null)
                cardArtImage.sprite = CardData.Data.cardArt;

            // Actualizar color del marco según el color de la carta
            UpdateCardColor();
        }

        /// <summary>
        /// Actualiza el color del marco de la carta
        /// </summary>
        private void UpdateCardColor()
        {
            if (CardData == null) return;

            Color cardColor = CardData.Data.cardColor.GetUIColor();

            if (cardFrameImage != null)
                cardFrameImage.color = cardColor;

            if (cardBackgroundImage != null)
            {
                Color bgColor = cardColor;
                bgColor.a = 0.3f;
                cardBackgroundImage.color = bgColor;
            }
        }

        /// <summary>
        /// Callback cuando la vida de la carta cambia
        /// </summary>
        private void OnHealthChanged(Card card, int newHealth)
        {
            if (healthText != null)
                healthText.text = newHealth.ToString();

            // Animación de daño (opcional)
            StartCoroutine(FlashRed());
        }

        /// <summary>
        /// Callback cuando el daño de la carta cambia
        /// </summary>
        private void OnDamageChanged(Card card, int newDamage)
        {
            if (damageText != null)
                damageText.text = newDamage.ToString();
        }

        /// <summary>
        /// Callback cuando la carta es destruida
        /// </summary>
        private void OnCardDestroyed(Card card)
        {
            // Animación de destrucción
            StartCoroutine(DestroyAnimation());
        }

        /// <summary>
        /// Resalta la carta (objetivo válido)
        /// </summary>
        public void SetHighlight(bool highlight)
        {
            isHighlighted = highlight;
            if (highlightEffect != null)
                highlightEffect.SetActive(highlight);
        }

        /// <summary>
        /// Marca la carta como seleccionada
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            if (selectedEffect != null)
                selectedEffect.SetActive(selected);
        }

        /// <summary>
        /// Callback del sistema de selección de ataque del manager
        /// </summary>
        private void OnAttackSelectionChangedHandler(Card selectedCard)
        {
            // Actualizar el estado visual: seleccionada solo si somos la carta elegida
            SetSelected(selectedCard != null && selectedCard == CardData);
        }

        #region Drag & Drop

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isDraggable || isEnemyCard) return;
            if (CardData == null) return;

            // Solo se puede arrastrar si está en la mano
            if (CardData.Location != CardLocation.Hand) return;

            // Re-evaluar canvas si es nulo (puede ocurrir si la carta fue instanciada dinámicamente)
            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            originalPosition = rectTransform.position;
            originalParent = transform.parent;
            originalSiblingIndex = transform.GetSiblingIndex();

            // Mover al frente
            transform.SetParent(canvas.transform);
            transform.SetAsLastSibling();

            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDraggable || isEnemyCard) return;
            if (CardData == null) return;
            if (CardData.Location != CardLocation.Hand) return;

            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDraggable || isEnemyCard) return;
            if (CardData == null) return;

            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;

            // Verificar si se soltó en una zona válida (campo de batalla)
            if (IsOverBattleField(eventData))
            {
                TryPlayCard();
            }
            else
            {
                // Volver a la posición original
                ReturnToOriginalPosition();
            }
        }

        #endregion

        #region Click Handling

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isClickable) return;
            if (CardData == null) return;

            // Si la carta está en el campo, intentar atacar
            if (CardData.Location == CardLocation.Field && !isEnemyCard)
            {
                OnCardClicked();
            }
            // Si es una carta enemiga y está resaltada, es un objetivo válido
            else if (isEnemyCard && isHighlighted)
            {
                OnTargetSelected();
            }
        }

        #endregion

        /// <summary>
        /// Callback cuando se hace click en la carta
        /// </summary>
        private void OnCardClicked()
        {
            var manager = CardBattleManager.Instance;
            if (manager == null) return;

            // Si ya estaba seleccionada, deseleccionar
            if (isSelected)
            {
                manager.ClearAttackSelection();
                return;
            }

            if (CardData.CanAttackThisTurn && !CardData.HasAttackedThisTurn)
            {
                // Limpiar selección anterior y seleccionar esta carta
                manager.SelectAttackingCard(CardData);
            }
            else
            {
                // Carta no puede atacar, limpiar selección si hubiera
                manager.ClearAttackSelection();
            }
        }

        /// <summary>
        /// Callback cuando esta carta es seleccionada como objetivo
        /// </summary>
        private void OnTargetSelected()
        {
            var manager = CardBattleManager.Instance;
            if (manager == null) return;

            Card attacker = manager.SelectedAttackingCard;
            if (attacker == null)
            {
                Debug.LogWarning("No hay carta atacante seleccionada");
                return;
            }

            Debug.Log($"{attacker.Data.cardName} ataca a {CardData.Data.cardName}");
            manager.PlayerAttackWithCard(attacker, CardData);
            manager.ClearAttackSelection();
        }

        /// <summary>
        /// Intenta jugar la carta en el campo
        /// </summary>
        private void TryPlayCard()
        {
            if (CardBattleManager.Instance != null)
            {
                if (CardBattleManager.Instance.PlayerPlayCard(CardData))
                {
                    // La carta fue jugada exitosamente
                    Debug.Log($"Carta {CardData.Data.cardName} jugada");
                }
                else
                {
                    // No se pudo jugar, volver a la mano
                    ReturnToOriginalPosition();
                }
            }
            else
            {
                ReturnToOriginalPosition();
            }
        }

        /// <summary>
        /// Verifica si el puntero está sobre el campo de batalla
        /// </summary>
        private bool IsOverBattleField(PointerEventData eventData)
        {
            // Raycast para detectar la zona del campo de batalla
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (RaycastResult result in results)
            {
                // Comprobar por tag
                if (result.gameObject.CompareTag("BattleField"))
                    return true;

                // Fallback: comprobar si alguno de sus padres tiene un componente BattleField
                if (result.gameObject.GetComponentInParent<CardGame.Battle.BattleField>() != null)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Vuelve a la posición original
        /// </summary>
        private void ReturnToOriginalPosition()
        {
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalSiblingIndex);
            rectTransform.position = originalPosition;
        }

        /// <summary>
        /// Animación de flash rojo cuando recibe daño
        /// </summary>
        private IEnumerator FlashRed()
        {
            if (cardFrameImage == null) yield break;

            Color original = cardFrameImage.color;
            cardFrameImage.color = Color.red;

            yield return new WaitForSeconds(0.2f);

            cardFrameImage.color = original;
        }

        /// <summary>
        /// Animación de destrucción
        /// </summary>
        private IEnumerator DestroyAnimation()
        {
            float duration = 0.5f;
            float elapsed = 0f;
            Vector3 originalScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
                canvasGroup.alpha = 1f - t;

                yield return null;
            }

            Destroy(gameObject);
        }

        /// <summary>
        /// Crea elementos UI básicos si no existen
        /// </summary>
        private void CreateUIElementsIfMissing()
        {
            // Esta función crearía automáticamente los elementos UI básicos
            // Por ahora, asumimos que se configurarán en el prefab
        }

        private void OnDestroy()
        {
            // Desuscribirse de eventos
            if (CardData != null)
            {
                CardData.OnHealthChanged -= OnHealthChanged;
                CardData.OnDamageChanged -= OnDamageChanged;
                CardData.OnCardDestroyed -= OnCardDestroyed;
            }

            // Desuscribirse del sistema de selección de ataque
            if (!isEnemyCard && CardBattleManager.Instance != null)
            {
                CardBattleManager.Instance.OnAttackSelectionChanged -= OnAttackSelectionChangedHandler;
            }
        }
    }
}
