namespace Setup.Validation;

public static class ContentSchema
{
    public static class Collections
    {
        public const string UnitTemplates = "UnitTemplates";
        public const string Abilities = "Abilities";
        public const string EffectTemplates = "EffectTemplates";
        public const string EffectComponentTemplates = "EffectComponentTemplates";
        public const string GameStates = "GameStates";
        public const string Units = "Units";
    }

    public static class Fields
    {
        public const string Id = "Id";
        public const string Name = "Name";
        public const string PrimaryRole = "PrimaryRole";
        public const string SecondaryRole = "SecondaryRole";
        public const string Category = "Category";
        public const string EffectTemplateId = "EffectTemplateId";
        public const string AllowedTarget = "AllowedTarget";
        public const string AbilityIds = "AbilityIds";
        public const string ComponentTemplateIds = "ComponentTemplateIds";
        public const string Type = "Type";
        public const string MapGen = "MapGen";
        public const string TileDistribution = "TileDistribution";
        public const string TeamId = "TeamId";
        public const string TeamToAct = "TeamToAct";
        public const string AttackerTeamId = "AttackerTeamId";
        public const string DefenderTeamId = "DefenderTeamId";
        public const string Width = "Width";
        public const string Height = "Height";
        public const string Q = "Q";
        public const string R = "R";
        public const string GameStateId = "GameStateId";
    }

    public static string UnitTemplate(int index) => $"{Collections.UnitTemplates}[{index}]";
    public static string Ability(int index) => $"{Collections.Abilities}[{index}]";
    public static string EffectTemplate(int index) => $"{Collections.EffectTemplates}[{index}]";
    public static string EffectComponentTemplate(int index) => $"{Collections.EffectComponentTemplates}[{index}]";
    public static string GameState(int index) => $"{Collections.GameStates}[{index}]";
    public static string GameStateUnit(string gameStatePath, int unitIndex) => $"{gameStatePath}.{Collections.Units}[{unitIndex}]";

    public static string Property(string parentPath, string field) => $"{parentPath}.{field}";
    public static string IndexedProperty(string parentPath, string collectionField, int index) => $"{parentPath}.{collectionField}[{index}]";
}
