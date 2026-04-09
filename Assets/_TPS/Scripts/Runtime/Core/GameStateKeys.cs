namespace TPS.Runtime.Core
{
    /// <summary>
    /// Strict central repository for all string keys used in GameStateManager.
    /// Prevents typos ("Tyle") and unifies key usage across the project.
    /// </summary>
    public static class GameStateKeys
    {
        // Example keys (to be expanded as project grows)
        public const string Tutorial_Completed = "tutorial_completed";
        public const string Demo_QuestA_Done = "demo_quest_a_done";
        public const string Encounter_Boss_Dead = "encounter_boss_dead";
        public const string Dialog_Met_Mayor = "dialog_met_mayor";
        
        // Example int counters
        public const string Stat_Enemies_Killed = "stat_enemies_killed";
        public const string Stat_Doors_Opened = "stat_doors_opened";
        
        // Weather persistent tracking if needed (though WeatherSystem handles current)
        public const string Flag_Rain_Experienced = "flag_rain_experienced";
    }
}
