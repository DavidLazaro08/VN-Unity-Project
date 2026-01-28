using System.Collections.Generic;
using UnityEngine;

// Una línea del diálogo ya “parseada” del CSV.
public class DialogueLine
{
    public string speaker;
    public string text;
    public string cmd;

    public DialogueLine(string speaker, string text, string cmd)
    {
        this.speaker = speaker;
        this.text = text;
        this.cmd = cmd;
    }
}

public static class VNSceneLoader
{
    // Carga un CSV desde Resources/Dialogue/<fileName>.csv
    // Espera cabecera: speaker,text,cmd
    public static List<DialogueLine> LoadFromResources(string fileName)
    {
        var result = new List<DialogueLine>();

        TextAsset csv = Resources.Load<TextAsset>("Dialogue/" + fileName);

        if (csv == null)
        {
            Debug.LogError("No se encontró el CSV: Resources/Dialogue/" + fileName + ".csv");
            return result;
        }

        string[] rows = csv.text.Split('\n');

        // Saltamos cabecera (fila 0)
        for (int i = 1; i < rows.Length; i++)
        {
            string row = rows[i].Trim();
            if (string.IsNullOrWhiteSpace(row)) continue;

            var parts = SplitCsvRow(row);
            if (parts.Count < 2) continue;

            string speaker = parts[0].Trim();
            string text = parts[1].Trim();
            string cmd = parts.Count >= 3 ? parts[2].Trim() : "";

            result.Add(new DialogueLine(speaker, Unquote(text), cmd));
        }

        if (result.Count == 0)
            Debug.LogWarning("CSV cargado pero sin líneas: " + fileName);

        return result;
    }

    // -------------------------
    // Helpers CSV (simple, pero funciona para lo nuestro)
    // -------------------------

    private static string Unquote(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        s = s.Trim();
        if (s.StartsWith("\"") && s.EndsWith("\"") && s.Length >= 2)
            return s.Substring(1, s.Length - 2);
        return s;
    }

    // Split CSV respetando comillas.
    private static List<string> SplitCsvRow(string row)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var cur = "";

        for (int i = 0; i < row.Length; i++)
        {
            char ch = row[i];

            if (ch == '\"')
            {
                inQuotes = !inQuotes;
                cur += ch;
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                result.Add(cur);
                cur = "";
            }
            else
            {
                cur += ch;
            }
        }

        result.Add(cur);
        return result;
    }
}
