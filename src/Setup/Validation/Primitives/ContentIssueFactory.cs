namespace Setup.Validation.Primitives;

public static class ContentIssueFactory
{
    public const string RequiredFieldCode = "VAL_REQUIRED_FIELD";
    public const string NullCollectionCode = "VAL_NULL_COLLECTION";
    public const string NullItemCode = "VAL_NULL_ITEM";
    public const string DuplicateIdCode = "VAL_DUPLICATE_ID";
    public const string UnknownReferenceCode = "VAL_UNKNOWN_REFERENCE";
    public const string InvalidMapDimensionCode = "VAL_INVALID_MAP_DIMENSION";
    public const string InvalidTileDistributionCode = "VAL_INVALID_TILE_DISTRIBUTION";
    public const string TileDistributionNormalizationWarningCode = "VAL_TILE_DISTRIBUTION_NOT_NORMALIZED";
    public const string SpawnOutOfBoundsCode = "VAL_SPAWN_OUT_OF_BOUNDS";
    public const string InvalidTeamCode = "VAL_INVALID_TEAM";
    public const string InvalidTeamToActCode = "VAL_INVALID_TEAM_TO_ACT";
    public const string UnsupportedComponentTypeCode = "VAL_UNSUPPORTED_COMPONENT_TYPE";
    public const string MissingComponentFieldCode = "VAL_MISSING_COMPONENT_FIELD";
    public const string InvalidEnumValueCode = "VAL_INVALID_ENUM_VALUE";

    public static ContentIssue RequiredField(string path, string fieldName)
        => new(
            RequiredFieldCode,
            $"Required field '{fieldName}' is missing or blank.",
            path,
            ContentIssueSeverity.Error);

    public static ContentIssue NullCollection(string path)
        => new(
            NullCollectionCode,
            "Collection is null. Use an empty collection instead.",
            path,
            ContentIssueSeverity.Error);

    public static ContentIssue NullItem(string path)
        => new(
            NullItemCode,
            "Collection item is null.",
            path,
            ContentIssueSeverity.Error);

    public static ContentIssue DuplicateId(string path, string id)
        => new(
            DuplicateIdCode,
            $"Duplicate id '{id}'.",
            path,
            ContentIssueSeverity.Error);

    public static ContentIssue UnknownReference(string path, string referenceType, string id)
        => new(
            UnknownReferenceCode,
            $"Unknown {referenceType} reference '{id}'.",
            path,
            ContentIssueSeverity.Error);

    public static ContentIssue InvalidMapDimension(string path, int value)
        => new(
            InvalidMapDimensionCode,
            $"Map dimension must be greater than 0. Actual value: {value}.",
            path,
            ContentIssueSeverity.Error);

    public static ContentIssue InvalidTileDistribution(string path, string detail)
        => new(
            InvalidTileDistributionCode,
            $"Tile distribution is invalid: {detail}",
            path,
            ContentIssueSeverity.Error);

    public static ContentIssue TileDistributionNotNormalized(string path, double totalWeight)
        => new(
            TileDistributionNormalizationWarningCode,
            $"Tile distribution weights sum to {totalWeight:0.####}. Expected approximately 1.0.",
            path,
            ContentIssueSeverity.Warning);

    public static ContentIssue SpawnOutOfBounds(
        string path,
        int q,
        int r,
        int col,
        int row,
        int width,
        int height)
        => new(
            SpawnOutOfBoundsCode,
            $"Spawn coordinate (q={q}, r={r}) resolves to offset (col={col}, row={row}) outside map bounds {width}x{height}.",
            path,
            ContentIssueSeverity.Error);

    public static ContentIssue InvalidTeam(string path, int teamId)
        => new(
            InvalidTeamCode,
            $"Team id must be greater than 0. Actual value: {teamId}.",
            path,
            ContentIssueSeverity.Error);

    public static ContentIssue InvalidTeamToAct(string path, int teamToAct, int attackerTeamId, int defenderTeamId)
        => new(
            InvalidTeamToActCode,
            $"TeamToAct must match attacker ({attackerTeamId}) or defender ({defenderTeamId}). Actual value: {teamToAct}.",
            path,
            ContentIssueSeverity.Error);

    public static ContentIssue UnsupportedComponentType(string path, string? componentType)
        => new(
            UnsupportedComponentTypeCode,
            $"Unsupported effect component type '{componentType ?? "<null>"}'.",
            path,
            ContentIssueSeverity.Error);

    public static ContentIssue MissingComponentField(string path, string fieldName)
        => new(
            MissingComponentFieldCode,
            $"Component field '{fieldName}' is required but missing.",
            path,
            ContentIssueSeverity.Error);

    public static ContentIssue InvalidEnumValue(string path, string enumName, string? rawValue)
        => new(
            InvalidEnumValueCode,
            $"Value '{rawValue ?? "<null>"}' is not valid for enum '{enumName}'.",
            path,
            ContentIssueSeverity.Error);
}
