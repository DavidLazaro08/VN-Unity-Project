using System;
using UnityEngine;

[Serializable]
public class CharacterPoseEntry
{
    public string id;        // "LOGAN", "DAMIAO"
    public string pose;      // "serio", "sonrisa", "normal"
    public Sprite sprite;    // sprite correspondiente
    public float scale = 1f; // escala personalizada (def: 1)
}
