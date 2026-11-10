using SimcProfileParser.Model.Generated;
using SimcProfileParser.Model.Profile;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimcProfileParser.Interfaces
{
    public interface ISimcGenerationService
    {
        /// <summary>
        /// Set the flag to use PTR data for data extraction
        /// </summary>
        bool UsePtrData { get; set; }
        /// <summary>
        /// Set the github branch name to use for data extraction
        /// </summary>
        string UseBranchName { get; set; }

        Task<SimcProfile> GenerateProfileAsync(List<string> profileString);
        /// <summary>
        /// Helper method to generate a SimcProfile based on the contents of an entire profile string
        /// </summary>
        /// <param name="profileString"></param>
        /// <returns></returns>
        Task<SimcProfile> GenerateProfileAsync(string profileString);

        Task<SimcItem> GenerateItemAsync(SimcItemOptions options);
        Task<SimcSpell> GenerateSpellAsync(SimcSpellOptions options);

        Task<string> GetGameDataVersionAsync();

        /// <summary>
        /// Clears all cached data used by the generator (both memory and disk cache).
        /// </summary>
        Task ClearCacheAsync();
        /// <summary>
        /// Gets all talents available to a class and specialization.
        /// Returns the raw talent tree structure; prerequisites, choice nodes and point requirements are not validated.
        /// </summary>
        /// <param name="classId">Class ID, e.g. 5 for Priest</param>
        /// <param name="specId">Specialization ID, e.g. 257 for Holy Priest</param>
        Task<List<SimcTalent>> GetAvailableTalentsAsync(int classId, int specId);
        /// <summary>
        /// Gets a class's base Strength, Agility, Stamina and Intellect at a player level, before racial modifiers.
        /// From dbc_t::attribute_base
        /// </summary>
        /// <param name="classId">Class ID, e.g. 5 for Priest</param>
        /// <param name="level">Player level</param>
        /// <returns>The base attributes, or null if simc has no data for the class at that level</returns>
        Task<SimcBaseStats> GetClassBaseStatsAsync(int classId, int level);
        /// <summary>
        /// Gets a race's Strength, Agility, Stamina and Intellect modifiers, e.g. +3 Intellect for a Gnome.
        /// From dbc_t::race_base
        /// </summary>
        /// <param name="raceId">simc's race ID, as returned by <see cref="SimcParsedProfile.RaceId"/>. 0 (no race) has no modifiers</param>
        /// <returns>The racial modifiers, or null if the race ID isn't known</returns>
        Task<SimcBaseStats> GetRaceBaseStatsAsync(int raceId);
        /// <summary>
        /// Gets a class's base mana at a player level. 0 for classes without mana.
        /// From dbc_t::resource_base
        /// </summary>
        /// <param name="classId">Class ID, e.g. 5 for Priest</param>
        /// <param name="level">Player level</param>
        /// <returns>The base mana, or null if simc has no data for the class or level</returns>
        Task<double?> GetBaseManaAsync(int classId, int level);
        /// <summary>
        /// Gets the combat rating needed for 1% of each stat at a player level, e.g. 46 crit rating for 1% crit at level 90.
        /// These are before any class or specialization multiplier, such as a specialization's mastery coefficient.
        /// From the __combat_ratings table used by dbc_t::combat_rating
        /// </summary>
        /// <param name="level">Player level</param>
        /// <returns>The combat ratings, or null if simc has no data for the level</returns>
        Task<SimcCombatRatings> GetCombatRatingsAsync(int level);
    }
}
