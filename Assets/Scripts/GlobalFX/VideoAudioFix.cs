using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Desactiva el audio del VideoPlayer para que no interfiera con
/// la música gestionada por AudioSource.
/// 
/// Lo usamos cuando el vídeo tiene pista de audio pero queremos que
/// el control del sonido sea externo (música ambiente, sistema propio, etc.).
/// </summary>
public class VideoAudioFix : MonoBehaviour
{
    [Header("Si se deja vacío, busca VideoPlayer en este mismo objeto")]
    public VideoPlayer videoPlayer;

    private void Awake()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer == null)
            return;

        // Desactivar salida de audio del vídeo
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

        // Por si el clip trae pistas de audio activas
        for (ushort i = 0; i < videoPlayer.audioTrackCount; i++)
            videoPlayer.EnableAudioTrack(i, false);
    }
}