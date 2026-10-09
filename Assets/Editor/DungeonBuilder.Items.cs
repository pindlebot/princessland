using UnityEditor;
using UnityEngine;

// Items: every item's stats, and the pickup that lies on the floor for each one.
// To add an item: draw it (Tools/make_item_sprites.py), add an ItemSpec below, and put it on a
// map with a legend line such as "5 = item plumed_helm" (MapFile.cs). Rebuild All Scenes.
public static partial class DungeonBuilder
{
    private const string EmberRingId = "ember_ring"; // the built-in 'I' tile

    private class ItemSpec
    {
        public string Id, Asset, Name, Description;
        public EquipSlot Slot;
        public string FloorSheet = "Items", FloorAnim; // its floor sprite (FloorAnim defaults to Asset)
        public string IconPath;                        // defaults to Assets/Art/UI/Icon<Asset>.png
        public Color Glow;
        public int SpellDamage, Hearts, Magic, WalkPercent, RechargePercent;
        public int RestoreHearts, RestoreMagic; // food: what eating it gives back
    }

    // Where each one lies: the Ember Ring in the dungeon's first room ('I'), the helm in the
    // bedroom at home, the boots by the camp on the castle grounds, the mail in Mermaid Cove's
    // sea cave, and the wand in the dungeon's locked treasure room. The bubble bath isn't lying
    // anywhere: Barnaby Badger sells it in the village (DungeonBuilder.Town.cs). Nor are the
    // cooking ingredients: the hens' coop, the pantry and the fruit bowl hand them out, and the
    // stove cooks them into pancakes (DungeonBuilder.Cooking.cs). An ingredient's description
    // says where to find it: the recipe card shows it until you have one.
    private static readonly ItemSpec[] ItemSpecs =
    {
        new ItemSpec
        {
            Id = EmberRingId, Asset = "EmberRing", Name = "Ember Ring", Slot = EquipSlot.Ring,
            Description = "A gold band set with a smouldering gem.",
            FloorSheet = "Props", FloorAnim = "Ring", Glow = new Color(1f, 0.45f, 0.25f),
            SpellDamage = 1,
        },
        new ItemSpec
        {
            Id = "plumed_helm", Asset = "PlumedHelm", Name = "Plumed Helm", Slot = EquipSlot.Helm,
            Description = "A little knight's helm with a coral plume. It fits just right.",
            Glow = new Color(1f, 0.6f, 0.65f),
            Hearts = 1, Magic = 10,
        },
        new ItemSpec
        {
            Id = "seashell_mail", Asset = "SeashellMail", Name = "Seashell Mail", Slot = EquipSlot.Armor,
            Description = "Shimmering shell scales the pirates hoarded, with a pearl at the heart.",
            Glow = new Color(0.4f, 0.95f, 0.9f),
            Hearts = 2,
        },
        new ItemSpec
        {
            Id = "starlight_wand", Asset = "StarlightWand", Name = "Starlight Wand", Slot = EquipSlot.Weapon,
            Description = "A wand with a fallen star on its tip. It hums when you cast.",
            Glow = new Color(1f, 0.9f, 0.5f),
            SpellDamage = 1, RechargePercent = 15,
        },
        new ItemSpec
        {
            Id = "trailblazer_boots", Asset = "TrailblazerBoots", Name = "Trailblazer Boots", Slot = EquipSlot.Boots,
            Description = "Comfy walking boots with warm cuffs, left by a traveller at the camp.",
            Glow = new Color(1f, 0.75f, 0.45f),
            WalkPercent = 20,
        },
        // Not equipment (Slot None): pour it into the bath at home for a mountain of bubbles (HouseFixture).
        new ItemSpec
        {
            Id = "bubble_bath", Asset = "BubbleBath", Name = "Bubble Bath",
            Description = "Lavender and honey, from Barnaby's stall in Hollyhock. Pour it into the bath at home for a mountain of bubbles!",
            Glow = new Color(0.8f, 0.65f, 1f),
        },
        // Cooking ingredients (Slot None): DungeonBuilder.Cooking.cs says where each one comes from.
        new ItemSpec
        {
            Id = "egg", Asset = "Egg", Name = "Egg",
            Description = "Find one in the hens' coop in Hollyhock.",
            Glow = new Color(1f, 0.95f, 0.8f),
        },
        new ItemSpec
        {
            Id = "flour", Asset = "Flour", Name = "Flour",
            Description = "There's a sack in the pantry, here in the kitchen.",
            Glow = new Color(1f, 0.95f, 0.85f),
        },
        new ItemSpec
        {
            Id = "strawberry", Asset = "Strawberry", Name = "Strawberry",
            Description = "Pick one from the fruit bowl on the kitchen island.",
            Glow = new Color(1f, 0.5f, 0.55f),
        },
        // Food: click it in the bag to eat it.
        new ItemSpec
        {
            Id = "pancakes", Asset = "Pancakes", Name = "Strawberry Pancakes",
            Description = "A fluffy stack with butter, syrup and a strawberry on top. Cooked on the stove at home.",
            Glow = new Color(1f, 0.8f, 0.45f),
            RestoreHearts = 3, RestoreMagic = 50,
        },
        // Hollow Farm's autumn festival: Pippin sells cider (DungeonBuilder.Farm.cs), and the
        // corn maze hides a hat at its centre (Levels/Farm.txt).
        new ItemSpec
        {
            Id = "apple_cider", Asset = "AppleCider", Name = "Hot Apple Cider",
            Description = "Warm, spiced and sweet, from Pippin's stand at the autumn festival on Hollow Farm.",
            Glow = new Color(1f, 0.6f, 0.3f),
            RestoreHearts = 2, RestoreMagic = 25,
        },
        new ItemSpec
        {
            Id = "pumpkin_hat", Asset = "PumpkinHat", Name = "Jack-o'-Lantern Hat", Slot = EquipSlot.Helm,
            Description = "A carved pumpkin to wear, its grin glowing. Found at the heart of Hollow Farm's corn maze.",
            Glow = new Color(1f, 0.55f, 0.2f),
            SpellDamage = 1, Magic = 15,
        },
    };

    // Items are ScriptableObject assets: pure data, shared by everything that refers to them.
    // You can also make new ones by hand: Project window > Create > Dungeon > Item.
    private static ItemDefinition CreateItem(ItemSpec spec)
    {
        var item = LoadOrCreateAsset<ItemDefinition>($"Assets/Items/{spec.Asset}.asset");
        var so = new SerializedObject(item);
        so.FindProperty("id").stringValue = spec.Id;
        so.FindProperty("displayName").stringValue = spec.Name;
        so.FindProperty("description").stringValue = spec.Description;
        so.FindProperty("icon").objectReferenceValue =
            SpriteSheetImporter.ImportSingle(spec.IconPath ?? $"Assets/Art/UI/Icon{spec.Asset}.png", 16);
        so.FindProperty("slot").enumValueIndex = (int)spec.Slot;
        so.FindProperty("spellDamageBonus").intValue = spec.SpellDamage;
        so.FindProperty("maxHealthBonus").intValue = spec.Hearts;
        so.FindProperty("maxManaBonus").intValue = spec.Magic;
        so.FindProperty("moveSpeedPercent").intValue = spec.WalkPercent;
        so.FindProperty("spellRechargePercent").intValue = spec.RechargePercent;
        so.FindProperty("healthRestore").intValue = spec.RestoreHearts;
        so.FindProperty("manaRestore").intValue = spec.RestoreMagic;
        so.ApplyModifiedPropertiesWithoutUndo();
        return item;
    }

    // Every item, so ids from the inventory and save files can be turned back into items.
    private static ItemDatabase CreateItemDatabase(params ItemDefinition[] items)
    {
        var database = LoadOrCreateAsset<ItemDatabase>("Assets/Items/ItemDatabase.asset");
        SetRefs(database, "items", items);
        return database;
    }

    // An item on the floor: a bobbing, glinting sprite with a faint glow so it's easy to spot.
    private static GameObject CreateItemPickupPrefab(SpriteSheetImporter.SpriteSheet sheet, ItemSpec spec, ItemDefinition item, Sprite shadowSprite)
    {
        var go = new GameObject($"{spec.Asset}Pickup");

        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        visual.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        AddLoopingSprite(visual, sheet, spec.FloorAnim ?? spec.Asset);

        AddShadow(go, shadowSprite, 0.6f);

        var glow = new GameObject("Glow").AddComponent<Light>();
        glow.transform.SetParent(go.transform, false);
        glow.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        glow.type = LightType.Point;
        glow.color = spec.Glow;
        glow.range = 2.5f;
        glow.intensity = 1.5f;

        var pickup = go.AddComponent<ItemPickup>();
        SetRef(pickup, "item", item);
        SetRef(pickup, "visual", visual.transform);
        SetRef(pickup, "pickupSound", Sound("pickup"));
        return SavePrefab(go, go.name);
    }
}
