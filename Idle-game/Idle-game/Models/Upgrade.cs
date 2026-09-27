using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Idle_game.Models
{
    public class Upgrade
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public double BaseCost {  get; set; }
        public double CostMultiplier { get; set; } 
        public double IncomePerSecond { get; set; }
        public int AmountOwned { get; set; }

        public double CurrentCost => BaseCost * Math.Pow(CostMultiplier, AmountOwned);
    }
}
