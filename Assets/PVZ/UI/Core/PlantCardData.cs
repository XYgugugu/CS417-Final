using UnityEngine;

namespace PVZ3D.UI
{
    /// <summary>
    /// Per-plant config asset consumed by the HUD's plant tray and the gameplay
    /// layer. Lives entirely in the Inspector — no code changes needed to add,
    /// rename, or rebalance a plant.
    ///
    /// <para>=== HOW TO ADD A NEW PLANT ===</para>
    /// <list type="number">
    ///   <item>Drop a portrait sprite (square PNG) under
    ///         <c>Assets/PVZ/UI/Sprites/PlantCards/</c>. Confirm the importer is set
    ///         to <c>Sprite (2D and UI)</c> + Single mode (it usually is).</item>
    ///   <item>In the Project window: right-click → <c>Create → PVZ → Plant Card Data</c>
    ///         and name the asset <c>PlantCard_&lt;Name&gt;</c>. Convention: the
    ///         file name matches the in-game name, the <see cref="plantId"/> is a
    ///         lowercase snake_case stable identifier.</item>
    ///   <item>Fill <see cref="plantId"/>, <see cref="displayName"/>, <see cref="icon"/>,
    ///         <see cref="sunCost"/>, <see cref="cooldownSeconds"/>,
    ///         <see cref="unlockedByDefault"/>.</item>
    ///   <item>Drag the new asset into <c>GameSettings_Default → Default Unlocked
    ///         Plants</c> if it should be available from the start.</item>
    ///   <item>Add a HUD slot (see <see cref="PlantCardSlotUI"/> top-of-class doc
    ///         for the duplicate-and-wire workflow).</item>
    /// </list>
    ///
    /// <para>=== HOW TO REPLACE AN EXISTING PLANT ===</para>
    /// Just edit this asset's fields. Every <see cref="PlantCardSlotUI"/>
    /// referencing it picks up the changes automatically next Play. To refresh
    /// the editor preview without entering Play, run the
    /// <b>Sync Icon From Card Data</b> context menu on the card slot.
    ///
    /// <para>=== ⚠ WARNING ===</para>
    /// Once a build has shipped to players, do <b>not</b> change <see cref="plantId"/> —
    /// it's the key under which save files store unlocks and cooldowns. Changing
    /// it will invalidate existing saves for that plant. <see cref="displayName"/>,
    /// <see cref="icon"/>, <see cref="sunCost"/>, and <see cref="cooldownSeconds"/>
    /// are safe to tweak at any time.
    /// </summary>
    [CreateAssetMenu(fileName = "PlantCard_", menuName = "PVZ/Plant Card Data", order = 1)]
    public class PlantCardData : ScriptableObject
    {
        [Tooltip("Stable string ID — used as a key in save files. " +
                 "DON'T change once shipped (would orphan existing player saves).")]
        public string plantId;

        [Tooltip("Display name on the card (e.g. \"Pea Shooter\").")]
        public string displayName;

        [Tooltip("Card portrait. Should be roughly square so it fits the slot. " +
                 "Set Texture Type to Sprite (2D and UI) in the importer.")]
        public Sprite icon;

        [Tooltip("Sun cost to plant.")]
        public int sunCost = 100;

        [Tooltip("Cooldown after planting, in seconds, before this card is usable again.")]
        public float cooldownSeconds = 7.5f;

        [Tooltip("If true, this plant is unlocked from the start of a fresh save. " +
                 "Locked plants can be unlocked later by gameplay code calling " +
                 "GameState.UnlockPlant(plantId).")]
        public bool unlockedByDefault = true;
    }
}
