﻿namespace FCS_ProductionSolutions.DeepDriller.Models.Upgrades
{
    internal class UpgradeClass
    {
        internal string FunctionName { get; set; }
        internal string FriendlyName { get; set; }



        public override string ToString()
        {
            return FriendlyName;
        }
    }
}