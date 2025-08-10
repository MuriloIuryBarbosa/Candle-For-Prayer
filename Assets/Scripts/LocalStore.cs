using UnityEngine;
using System.IO;

public static class LocalStore
{
    static string Path => System.IO.Path.Combine(Application.persistentDataPath, "appdata.json");

    public static void Save(SaveData data)
    {
        var json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(Path, json);
    }

    public static SaveData Load()
    {
        if (!File.Exists(Path)) return new SaveData();
        try {
            var json = File.ReadAllText(Path);
            return JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
        } catch {
            return new SaveData();
        }
    }
}
