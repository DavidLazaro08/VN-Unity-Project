using UnityEngine;

/// <summary>
/// Flags estáticos para controlar comportamiento one-shot de transiciones.
/// Se auto-resetean después de usarse una vez.
/// </summary>
public static class VNTransitionFlags
{
    /// <summary>
    /// Si es true, la próxima transición NO hará fade de música.
    /// Se resetea automáticamente a false después de usarse.
    /// </summary>
    public static bool SkipMusicFadeOnce = false;
}
