using System.Collections;
using UnityEngine;

public partial class VNDialogue
{
    private IEnumerator FadeInDialogue(float duration)
    {
        if (dialoguePanel == null) yield break;

        CanvasGroup cg = dialoguePanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = dialoguePanel.AddComponent<CanvasGroup>();

        cg.alpha = 0;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0, 1, elapsed / duration);
            yield return null;
        }
        cg.alpha = 1;
    }
}
