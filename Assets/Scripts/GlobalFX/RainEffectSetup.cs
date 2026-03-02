using UnityEngine;

/// <summary>
/// Script que utilizamos para configurar la lluvia en esta escena.
/// 
/// Crea la cámara FX, ajusta capas y coloca el prefab de lluvia.
/// Es solo para dejar todo montado; después se puede eliminar.
/// </summary>
public class RainEffectSetup : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Cámara principal de la escena (arrastra aquí)")]
    public Camera mainCamera;

    [Header("Parámetros del Efecto")]
    [Tooltip("Posición del efecto de lluvia")]
    public Vector3 rainPosition = new Vector3(0f, 12f, 5f);

    [Tooltip("Rotación del efecto de lluvia")]
    public Vector3 rainRotation = Vector3.zero;

    private bool _setupComplete = false;

    void Start()
    {
        if (_setupComplete) return;

        Debug.Log("[RainEffectSetup] Iniciando configuración automática...");

        // Si no me asignan cámara, intento usar la principal del tag MainCamera.
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("[RainEffectSetup] No se encontró la cámara principal. Asigna 'mainCamera' en el Inspector.");
                return;
            }
        }

        // 1) Capa FX (si no existe, Unity no deja crearla por script, así que avisamos)
        SetupFXLayer();

        // 2) Ajustes en cámara principal (que NO dibuje la capa FX)
        SetupMainCamera();

        // 3) Cámara secundaria para FX (solo dibuja FX y va por encima)
        SetupFXCamera();

        // 4) Instanciar el prefab de lluvia si aún no está en escena
        SetupRainPrefab();

        _setupComplete = true;
        Debug.Log("[RainEffectSetup] ✅ Configuración completada. Puedes eliminar este script del GameObject.");
    }

    void SetupFXLayer()
    {
        // Unity no permite crear capas por script, pero sí podemos comprobar si existe.
        int fxLayer = LayerMask.NameToLayer("FX");

        if (fxLayer == -1)
        {
            Debug.LogWarning("[RainEffectSetup] ⚠️ La capa 'FX' no existe. Créala manualmente:");
            Debug.LogWarning("   1. Edit → Project Settings → Tags and Layers");
            Debug.LogWarning("   2. En 'Layers', encuentra un slot vacío (ej: User Layer 8)");
            Debug.LogWarning("   3. Escribe 'FX'");
            Debug.LogWarning("   4. Vuelve a ejecutar Play");
        }
        else
        {
            Debug.Log("[RainEffectSetup] ✅ Capa 'FX' encontrada.");
        }
    }

    void SetupMainCamera()
    {
        // La cámara principal NO debería renderizar la capa FX (para que no duplique).
        int fxLayer = LayerMask.NameToLayer("FX");

        if (fxLayer != -1)
        {
            mainCamera.cullingMask &= ~(1 << fxLayer); // Quitar capa FX
            Debug.Log("[RainEffectSetup] ✅ Cámara principal configurada (sin capa FX).");
        }

        // Depth base: 0
        mainCamera.depth = 0;
    }

    void SetupFXCamera()
    {
        // Si ya existe, no creamos otra (para no duplicar cámaras).
        Camera fxCamera = GameObject.Find("FX_Camera")?.GetComponent<Camera>();

        if (fxCamera == null)
        {
            GameObject fxCamObj = new GameObject("FX_Camera");
            fxCamera = fxCamObj.AddComponent<Camera>();

            // Solo pinta “por encima” (sin limpiar color)
            fxCamera.clearFlags = CameraClearFlags.Depth;
            fxCamera.depth = 1;

            int fxLayer = LayerMask.NameToLayer("FX");
            if (fxLayer != -1)
            {
                fxCamera.cullingMask = 1 << fxLayer; // Solo la capa FX
            }

            // Copiamos posición/rotación de la principal para que todo encaje.
            fxCamObj.transform.position = mainCamera.transform.position;
            fxCamObj.transform.rotation = mainCamera.transform.rotation;

            Debug.Log("[RainEffectSetup] ✅ FX_Camera creada y configurada.");
        }
        else
        {
            Debug.Log("[RainEffectSetup] ℹ️ FX_Camera ya existe.");
        }
    }

    void SetupRainPrefab()
    {
        // Si ya existe en escena, no lo instanciamos otra vez.
        GameObject existingRain = GameObject.Find("lluviaFx");

        if (existingRain == null)
        {
            // Ojo: tiene que estar en Resources/Efectoscine/lluviaFx
            GameObject rainPrefab = Resources.Load<GameObject>("Efectoscine/lluviaFx");

            if (rainPrefab == null)
            {
                Debug.LogError("[RainEffectSetup] ❌ No se encontró el prefab 'lluviaFx' en Resources/Efectoscine/");
                return;
            }

            GameObject rainInstance = Instantiate(rainPrefab);
            rainInstance.name = "lluviaFx"; // Quitamos el (Clone)

            rainInstance.transform.position = rainPosition;
            rainInstance.transform.rotation = Quaternion.Euler(rainRotation);

            // La lluvia debe ir en la capa FX para que solo la pinte la FX_Camera.
            int fxLayer = LayerMask.NameToLayer("FX");
            if (fxLayer != -1)
            {
                SetLayerRecursively(rainInstance, fxLayer);
                Debug.Log("[RainEffectSetup] ✅ Prefab lluviaFx instanciado y configurado.");
            }
            else
            {
                Debug.LogWarning("[RainEffectSetup] ⚠️ Prefab instanciado, pero la capa 'FX' no existe.");
            }
        }
        else
        {
            Debug.Log("[RainEffectSetup] ℹ️ lluviaFx ya existe en la escena.");
        }
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Solo para editor: ver el punto donde cae la lluvia.
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(rainPosition, 0.5f);
        Gizmos.DrawLine(rainPosition, rainPosition + Vector3.down * 2f);
    }
#endif
}