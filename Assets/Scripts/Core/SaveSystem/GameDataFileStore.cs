using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 실제 저장, 로드 API 제공
/// </summary>
public static class GameDataFileStore
{
    public static bool TryLoad(string path, out GameSaveData data, out string error)
    {
        // 파일 존재 시 그대로 사용
        if (TryLoadFile(path, out data, out error))
        {
            return true;
        }

        // 없다면 백업 파일 확인
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
        string temporaryPath = GetTempPath(path);
        string backupPath = GetBackupPath(path);

        try
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!data.TryValidate(out string validationError))
            {
                error = $"Save data validation failed: {validationError}";
                return false;
            }

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

    public static bool TrySaveNewGame(string path, GameSaveData data, out string error)
    {
        if (!TrySave(path, data, out error))
        {
            return false;
        }

        string backupPath = GetBackupPath(path);
        try
        {
            // A normal replace keeps the previous run in the backup file. A new game must
            // not recover that old run if the new primary save later becomes unreadable.
            File.Copy(path, backupPath, true);
        }
        catch (Exception copyException)
        {
            TryDelete(backupPath);

            Debug.LogWarning(
                $"The new game was saved, but its backup could not be synchronized. " +
                $"The stale backup was removed when possible. {copyException}");
        }

        error = null;
        return true;
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
            json = MigrateLegacyInventoryJson(json);
            data = JsonUtility.FromJson<GameSaveData>(json);
            if (data == null)
            {
                error = "Save file did not contain valid game data.";
                return false;
            }

            data.RepairAfterLoad();
            error = null;
            return true;
        }
        catch (Exception exception)
        {
            error = exception.ToString();
            return false;
        }
    }

    private static string MigrateLegacyInventoryJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return json;
        }

        return json
            .Replace("\"_creatureSlots\"", "\"_creatures\"")
            .Replace("\"creatureSlots\"", "\"_creatures\"")
            .Replace("\"resourceAmounts\"", "\"_resourceAmounts\"");
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

    private static string GetTempPath(string path)
    {
        return path + ".tmp";
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
