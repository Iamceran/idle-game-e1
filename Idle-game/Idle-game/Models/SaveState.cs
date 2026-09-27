using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Idle_game.Models
{
    public class SaveState
    {
        public double Currency { get; set; }
        public DateTime LastSaved { get; set; } = DateTime.Now;
        public List<UpgradeSaveData> Upgrades { get; set; } = new();
    }
}
