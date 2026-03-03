#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using CardGame.Core;

/// <summary>
/// Herramienta de editor para generar todos los CardData desde las imágenes disponibles.
/// Menú: Card Game → Generate All Card Assets
/// </summary>
public class CardDataGenerator
{
    [MenuItem("Card Game/Generar todos los CardData")]
    public static void GenerateAllCards()
    {
        // Definición de todas las cartas disponibles
        // Formato: (nombre, color, manaCost, baseDamage, baseHealth, artFileName)
        var cardDefinitions = new (string name, CardColor color, int mana, int damage, int health, string art)[]
        {
            // Cartas ROJAS
            ("Virus Rojo",     CardColor.Red,   1, 2, 2, "virus_rojo"),
            ("Firewall Rojo",  CardColor.Red,   2, 3, 3, "firewall_rojo"),
            ("Hacker Rojo",    CardColor.Red,   3, 4, 3, "hacker_rojo"),
            ("Programa Rojo",  CardColor.Red,   2, 2, 4, "programa_rojo"),
            // Cartas VERDES
            ("Drone Verde",    CardColor.Green, 1, 2, 2, "drone_verde"),
            ("Antivirus Verde",CardColor.Green, 2, 1, 5, "antivirus_verde"),
            ("IA Verde",       CardColor.Green, 3, 3, 4, "ia_verde"),
            ("Servidor Verde", CardColor.Green, 2, 2, 4, "servidor_verde"),
            // Cartas AZULES
            ("NetRunner Azul", CardColor.Blue,  1, 2, 3, "netrunner_azul"),
            ("Bot Azul",       CardColor.Blue,  2, 3, 2, "bot_azul"),
            ("Cortafuegos Azul",CardColor.Blue, 3, 2, 5, "cortafuegos_azul"),
            ("Matrix Azul",    CardColor.Blue,  3, 4, 3, "matrix_azul"),
        };

        string savePath = "Assets/Resources/CardGame/Cards";
        System.IO.Directory.CreateDirectory(savePath);

        int created = 0;
        int updated = 0;

        foreach (var def in cardDefinitions)
        {
            string assetPath = $"{savePath}/{def.name.Replace(" ", "")}.asset";
            CardData existing = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);

            CardData card = existing != null ? existing : ScriptableObject.CreateInstance<CardData>();

            card.cardName   = def.name;
            card.cardColor  = def.color;
            card.manaCost   = def.mana;
            card.baseDamage = def.damage;
            card.baseHealth = def.health;
            card.description = $"Carta {def.color} con {def.damage} de ataque y {def.health} de vida.";

            // Cargar el arte desde Resources
            Sprite art = Resources.Load<Sprite>($"CardGame/CardArt/{def.art}");
            if (art != null)
                card.cardArt = art;
            else
                Debug.LogWarning($"[CardDataGenerator] Arte no encontrado: CardGame/CardArt/{def.art}");

            if (existing == null)
            {
                AssetDatabase.CreateAsset(card, assetPath);
                created++;
            }
            else
            {
                EditorUtility.SetDirty(card);
                updated++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[CardDataGenerator] ✅ Generación completa: {created} creadas, {updated} actualizadas.");
        EditorUtility.DisplayDialog(
            "CardData generados",
            $"Se han creado {created} cartas nuevas y actualizado {updated} existentes.\n\n" +
            $"Puedes encontrarlas en:\n{savePath}",
            "OK"
        );
    }
}
#endif
