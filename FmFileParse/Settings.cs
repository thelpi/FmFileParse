using System.Data;
using System.Text;

namespace FmFileParse;

internal static class Settings
{
    public const string ConnString = "Server=localhost;Database=cm_save_explorer;Uid=root;Pwd=;";

    public const string SaveFilesPath = "S:\\Share_VM\\saves\\test";

    public const string SaveFileExtension = "sav";

    public const decimal MinValueOccurenceRate = 2 / 3M;

    public const decimal MinPlayerOccurencesRate = 1 / 3M;

    public const int MinAttributeValue = 1;

    public const int MaxAttributeValue = 20;

    public const int MinAttributeForPosition = 15;

    public static readonly string[] StringColumns =
    [
        "first_name", "last_name", "common_name", "transfer_status", "squad_status"
    ];

    public static readonly string[] DateColumns =
    [
        "date_of_birth", "contract_expiration"
    ];

    public static DbType GetDbType(string column)
    {
        return StringColumns.Contains(column)
            ? DbType.String
            : (DateColumns.Contains(column)
                ? DbType.Date
                : DbType.Int32);
    }

    public static readonly string[] CommonSqlColumns =
    [
        // intrinsic
        "first_name", "last_name", "common_name", "date_of_birth", "right_foot", "left_foot",
        // nation related
        "nation_id", "secondary_nation_id", "caps", "international_goals",
        // potential & reputation
        "ability", "potential_ability", "home_reputation", "current_reputation", "world_reputation",
        // club related
        "club_id", "value", "contract_expiration", "wage", "transfer_status", "squad_status",
        // release fee
        "manager_job_rel", "min_fee_rel", "non_play_rel", "non_promotion_rel", "relegation_rel",
        // positions
        "pos_goalkeeper", "pos_sweeper", "pos_defender", "pos_defensive_midfielder", "pos_midfielder",
        "pos_attacking_midfielder", "pos_forward", "pos_wingback", "pos_free_role",
        // sides
        "side_left", "side_right", "side_center",
        // attributes
        "acceleration", "adaptability", "aggression", "agility", "ambition", "anticipation", "balance", "bravery",
        "consistency", "corners", "creativity", "crossing", "decisions", "determination", "dirtiness", "dribbling",
        "finishing", "flair", "handling", "heading", "important_matches", "influence", "injury_proneness", "jumping",
        "long_shots", "loyalty", "marking", "natural_fitness", "off_the_ball", "one_on_ones", "pace", "passing",
        "penalties", "positioning", "pressure", "professionalism", "reflexes", "set_pieces", "sportsmanship", "stamina",
        "strength", "tackling", "teamwork", "technique", "temperament", "throw_ins", "versatility", "work_rate",
        // preferences
        "liked_staff_1", "liked_staff_2", "liked_staff_3", "disliked_staff_1", "disliked_staff_2", "disliked_staff_3",
        "liked_club_1", "liked_club_2", "liked_club_3", "disliked_club_1", "disliked_club_2", "disliked_club_3"
    ];

    public static readonly string[] UnmergedOnlyColumns =
    [
        "id", "file_id"
    ];

    public static readonly string[] ForeignKeyColumns =
    [
        "club_id", "nation_id", "secondary_nation_id",
        "liked_staff_1", "liked_staff_2", "liked_staff_3",
        "disliked_staff_1", "disliked_staff_2", "disliked_staff_3",
        "liked_club_1", "liked_club_2", "liked_club_3",
        "disliked_club_1", "disliked_club_2", "disliked_club_3"
    ];

    public static readonly Encoding DefaultEncoding = Encoding.Latin1;

    public static readonly string[] SaveFilesReferencesColumns =
    [
        // order is important
        "data_type", "data_id", "file_id", "save_id"
    ];
}
