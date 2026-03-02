using System;
using UnityEngine;

[Serializable]
public class CharacterPoseEntry
{
    // Identificador del personaje (ej: "LOGAN", "DAMIAO")
    public string id;

    // Nombre de la pose tal como se usará en el CSV (ej: "serio", "sonrisa", "normal")
    public string pose;

    // Sprite que corresponde a ese personaje + pose
    public Sprite sprite;

    // Escala opcional por pose (por si algún sprite necesita ajustarse un poco)
    public float scale = 1f;
}