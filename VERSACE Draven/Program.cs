namespace VERSACEDraven
{
    using System;

    using LeagueSharp;
    using LeagueSharp.Common;

    internal class Program
    {

        private static void GameOnOnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName == "Draven")
            {
                new VERSACEDraven().Load();
            }
        }

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += GameOnOnGameLoad;
        }

    }
}