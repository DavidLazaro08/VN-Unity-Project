using System.Collections;
using UnityEngine;

public partial class VNDialogue
{
    /*
     * VNDialogue.Fades
     * ----------------
     * Gestiona pequeñas transiciones visuales del panel de diálogo.
     * Se utiliza para hacer aparecer el cuadro de texto suavemente.
     */

    /// <summary>
    /// Realiza un fade-in del panel de diálogo usando CanvasGroup.
    /// </summary>
    private IEnumerator FadeInDialogue(float duration)
    {
        if (dialoguePanel == null) yield break;

        CanvasGroup cg = dialoguePanel.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = dialoguePanel.AddComponent<CanvasGroup>();
        }

        cg.alpha = 0f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            cg.alpha = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        cg.alpha = 1f;
    }
}