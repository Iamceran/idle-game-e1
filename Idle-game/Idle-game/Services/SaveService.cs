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
        public static SaveState? Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return null;
                }

                string json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<SaveState>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load failed: {ex.Message}");
                return null;
            }
        }

        // 3. Check if a save file exists
        public static bool SaveExists()
        {
            return File.Exists(FilePath);
        }
    }
}
