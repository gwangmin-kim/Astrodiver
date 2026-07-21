using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class GameDataFileStore
{
    public static bool TryLoad(string path, out GameSaveData data, out string error)
    {
        if (TryLoadFile(path, out data, out error))
        {
            return true;
        }

        string backupPath = GetBackupPath(path);
        if (!File.Exists(backupPath))
        {
            return false;
        }

        string primaryError = error;
        if (TryLoadFile(backupPath, out data, out error))
        {
            Debug.LogWarning($"Recovered game data from backup because the primary save failed: {primaryError}");
            return true;
        }

        error = $"Primary save: {primaryError} Backup save: {error}";
        return false;
    }

    public static bool TrySave(string path, GameSaveData data, out string error)
    {
        string temporaryPath = path + ".tmp";
        string backupPath = GetBackupPath(path);

        try
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            data.Normalize();
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));

            if (File.Exists(path))
            {
                ReplaceExistingFile(temporaryPath, path, backupPath);
            }
            else
            {
                File.Move(temporaryPath, path);
            }

            error = null;
            return true;
        }
        catch (Exception exception)
        {
            error = exception.ToString();
            TryDelete(temporaryPath);
            return false;
        }
    }

    private static bool TryLoadFile(string path, out GameSaveData data, out string error)
    {
        data = null;

        try
        {
            if (!File.Exists(path))
            {
                error = "Save file does not exist.";
                return false;
            }

            string json = File.ReadAllText(path, Encoding.UTF8);
            data = JsonUtility.FromJson<GameSaveData>(json);
            if (data == null)
            {
                error = "Save file did not contain valid game data.";
                return false;
            }

            data.Normalize();
            error = null;
            return true;
        }
        catch (Exception exception)
        {
            error = exception.ToString();
            return false;
        }
    }

    private static void ReplaceExistingFile(
        string temporaryPath,
        string targetPath,
        string backupPath)
    {
        try
        {
            File.Replace(temporaryPath, targetPath, backupPath);
        }
        catch (PlatformNotSupportedException)
        {
            CopyReplace(temporaryPath, targetPath, backupPath);
        }
        catch (IOException)
        {
            CopyReplace(temporaryPath, targetPath, backupPath);
        }
    }

    private static void CopyReplace(
        string temporaryPath,
        string targetPath,
        string backupPath)
    {
        File.Copy(targetPath, backupPath, true);
        File.Copy(temporaryPath, targetPath, true);
        File.Delete(temporaryPath);
    }

    private static string GetBackupPath(string path)
    {
        return path + ".bak";
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Preserve the original save error.
        }
    }
}
