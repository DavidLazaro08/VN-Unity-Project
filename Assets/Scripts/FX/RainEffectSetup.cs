using UnityEngine;

/// <summary>
/// Script de configuración automática del efecto de lluvia para Scene_Game2.
/// Configura la cámara FX y el prefab lluviaFx sin afectar Scene_Menu.
/// 
/// INSTRUCCIONES:
/// 1. Adjuntar este script a cualquier GameObject en Scene_Game2
/// 2. Asignar la cámara principal en el Inspector
/// 3. Play → El script configurará todo automáticamente
/// 4. Guardar la escena cuando estés satisfecho
/// 5. ELIMINAR este script del GameObject (ya no es necesario)
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

        // Validar cámara principal
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("[RainEffectSetup] No se encontró la cámara principal. Asigna 'mainCamera' en el Inspector.");
                return;
            }
        }

        // 1. Crear capa FX si no existe
        SetupFXLayer();

        // 2. Configurar cámara principal
        SetupMainCamera();

        // 3. Crear cámara FX
        SetupFXCamera();

        // 4. Instanciar prefab de lluvia
        SetupRainPrefab();

        _setupComplete = true;
        Debug.Log("[RainEffectSetup] ✅ Configuración completada. Puedes eliminar este script del GameObject.");
    }

    void SetupFXLayer()
    {
        // Unity no permite crear capas por script, pero podemos verificar
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
        // Asegurar que la cámara principal NO renderice la capa FX
        int fxLayer = LayerMask.NameToLayer("FX");
        
        if (fxLayer != -1)
        {
            mainCamera.cullingMask &= ~(1 << fxLayer); // Quitar capa FX
            Debug.Log("[RainEffectSetup] ✅ Cámara principal configurada (sin capa FX).");
        }

        // Asegurar depth = 0
        mainCamera.depth = 0;
    }

    void SetupFXCamera()
    {
        // Buscar si ya existe FX_Camera
        Camera fxCamera = GameObject.Find("FX_Camera")?.GetComponent<Camera>();

        if (fxCamera == null)
        {
            // Crear nueva cámara FX
            GameObject fxCamObj = new GameObject("FX_Camera");
            fxCamera = fxCamObj.AddComponent<Camera>();

            // Configurar
            fxCamera.clearFlags = CameraClearFlags.Depth;
            fxCamera.depth = 1; // Renderiza después de la cámara principal
            
            int fxLayer = LayerMask.NameToLayer("FX");
            if (fxLayer != -1)
            {
                fxCamera.cullingMask = 1 << fxLayer; // Solo renderiza capa FX
            }

            // Copiar posición de la cámara principal
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
        // Buscar si ya existe lluviaFx en la escena
        GameObject existingRain = GameObject.Find("lluviaFx");

        if (existingRain == null)
        {
            // Cargar prefab desde Resources
            GameObject rainPrefab = Resources.Load<GameObject>("Efectoscine/lluviaFx");

            if (rainPrefab == null)
            {
                Debug.LogError("[RainEffectSetup] ❌ No se encontró el prefab 'lluviaFx' en Resources/Efectoscine/");
                return;
            }

            // Instanciar
            GameObject rainInstance = Instantiate(rainPrefab);
            rainInstance.name = "lluviaFx"; // Quitar "(Clone)"

            // Configurar posición y rotación
            rainInstance.transform.position = rainPosition;
            rainInstance.transform.rotation = Quaternion.Euler(rainRotation);

            // Asignar a capa FX
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
        // Visualizar posición del efecto de lluvia en el editor
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(rainPosition, 0.5f);
        Gizmos.DrawLine(rainPosition, rainPosition + Vector3.down * 2f);
    }
#endif
}
