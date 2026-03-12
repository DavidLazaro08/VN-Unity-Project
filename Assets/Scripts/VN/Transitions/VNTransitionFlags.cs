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
    // Si es true, la música actual no hace fade out durante esta escena
    public static bool SkipMusicFadeOnce = false;

    // Si es true, la música se mantiene VIVA permanentemente entre escenas (no va al MusicTailFader)
    public static bool KeepMusicAliveOnce = false;
    /// <summary>
    /// Si > 0, la próxima transición hará fade de música durante este tiempo (segundos)
    /// en lugar de los 0.8s por defecto. Se resetea a 0 después de usarse.
    /// </summary>
    public static float MusicFadeDuration = 0f;

}
