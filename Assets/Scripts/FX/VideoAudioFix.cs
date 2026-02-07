using UnityEngine;
using UnityEngine.Video;

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

        // Desactiva audio del vídeo (la música va por AudioSource aparte)
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

        // Por si el vídeo trae pistas de audio y Unity intenta tocarlas igualmente
        for (ushort i = 0; i < videoPlayer.audioTrackCount; i++)
            videoPlayer.EnableAudioTrack(i, false);
    }
}
