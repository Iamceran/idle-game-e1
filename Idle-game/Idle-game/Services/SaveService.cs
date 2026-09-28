using Idle_game.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Idle_game.Services
{
    public static class SaveService
    {
        // Save file location: AppData/Local/IdleGame/savegame.json
        private static readonly string FolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IdleGame"
        );

        private static readonly string FilePath = Path.Combine(FolderPath, "savegame.json");

        // 1. Save state to disk
        public static bool Save(SaveState state)
        {
            try
            {
                if (!Directory.Exists(FolderPath))
                {
                    Directory.CreateDirectory(FolderPath);
                }

                // Format JSON with indentation for readability
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(state, options);

                File.WriteAllText(FilePath, json);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save failed: {ex.Message}");
                return false;
            }
        }

        // 2. Load state from disk
        // Returns a tuple: (SaveState? State, bool IsCorrupted)
        public static (SaveState? State, bool IsCorrupted) Load()
        {
            if (!File.Exists(FilePath))
            {
                return (null, false); // File simply doesn't exist yet
            }

            try
            {
                string json = File.ReadAllText(FilePath);
                SaveState? state = JsonSerializer.Deserialize<SaveState>(json);

                if (state == null)
                {
                    throw new JsonException("Deserialized state returned null.");
                }

                return (state, false); // Success!
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load failed due to corruption: {ex.Message}");

                // Quarantine the corrupted file so it isn't overwritten immediately
                try
                {
                    string corruptPath = Path.Combine(FolderPath, $"savegame_corrupted_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                    if (File.Exists(FilePath))
                    {
                        File.Move(FilePath, corruptPath, overwrite: true);
                    }
                }
                catch { /* Ignore quarantine failure if file is locked */ }

                return (null, true); // Corrupted!
            }
        }

        // 3. Check if a save file exists
        public static bool SaveExists()
        {
            return File.Exists(FilePath);
        }
    }
}
