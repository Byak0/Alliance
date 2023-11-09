namespace Alliance.Common.GameModes.Story.Models
{
    public class SpawnLogic
    {
        public string[] DefaultCharacters { get; }
        public string[] DefaultSpawnTags { get; }
        public bool StoreAgentsInfo { get; }
        public bool UsePreviousActAgents { get; }
        public int[] MaxLives { get; }
        public bool KeepLivesFromPreviousAct { get; set; }
        public LocationStrategy[] LocationStrategies { get; }
        public RespawnStrategy[] RespawnStrategies { get; }

        public SpawnLogic(string[] defaultCharacters, string[] spawnTags, bool storeAgentsInfo, bool usePreviousActAgents, int[] maxLives, bool keepLivesFromPreviousAct, LocationStrategy[] locationStrategies, RespawnStrategy[] respawnStrategies)
        {
            DefaultCharacters = defaultCharacters;
            DefaultSpawnTags = spawnTags;
            StoreAgentsInfo = storeAgentsInfo;
            UsePreviousActAgents = usePreviousActAgents;
            MaxLives = maxLives;
            KeepLivesFromPreviousAct = keepLivesFromPreviousAct;
            LocationStrategies = locationStrategies;
            RespawnStrategies = respawnStrategies;
        }
    }

    public enum LocationStrategy
    {
        Invalid,
        OnlyTags,
        OnlyFlags,
        TagsThenFlags,
        PlayerChoice,
    }

    public enum RespawnStrategy
    {
        Invalid,
        NoRespawn,
        MaxLivesPerTeam,
        MaxLivesPerPlayer,
    }
}
