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

    /// <summary>
    /// Si > 0, la próxima transición hará fade de música durante este tiempo (segundos)
    /// en lugar de los 0.8s por defecto. Se resetea a 0 después de usarse.
    /// </summary>
    public static float MusicFadeDuration = 0f;

}
