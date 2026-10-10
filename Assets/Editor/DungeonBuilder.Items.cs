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
        public bool KeyItem;                    // a treasure: lives in the treasures tab
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
        // The Whispering Woods and the easter eggs (art: Tools/make_woods_sprites.py).
        new ItemSpec
        {
            Id = "frog_hat", Asset = "FrogHat", Name = "Frog Hat", Slot = EquipSlot.Hat, FloorSheet = "WoodsItems",
            Description = "A little green frog hat with big round eyes. A frog gave it to you for flushing so much. Ribbit!",
            Glow = new Color(0.5f, 0.9f, 0.4f),
            Hearts = 1,
        },
        new ItemSpec
        {
            Id = "healing_apple", Asset = "HealingApple", Name = "Healing Apple", FloorSheet = "WoodsItems",
            Description = "A crunchy red apple from the Whispering Woods.",
            Glow = new Color(1f, 0.4f, 0.4f),
            RestoreHearts = 2,
        },
        new ItemSpec
        {
            Id = "mana_berry", Asset = "ManaBerry", Name = "Mana Berry", FloorSheet = "WoodsItems",
            Description = "Sparkly blue berries that taste like magic.",
            Glow = new Color(0.45f, 0.55f, 1f),
            RestoreMagic = 40,
        },
        new ItemSpec
        {
            Id = "fairy_lantern", Asset = "FairyLantern", Name = "Fairy Lantern", Slot = EquipSlot.None, FloorSheet = "WoodsItems",
            Description = "A lantern with a tiny fairy light inside. It follows you and glows in the dark.",
            Glow = new Color(1f, 0.9f, 0.5f), KeyItem = true,
        },
        // The first metroidvania abilities and gems (art: Tools/make_gate_sprites.py). Key items: the treasures tab.
        new ItemSpec
        {
            Id = Abilities.BouncyBoots, Asset = "BouncyBoots", Name = "Bouncy Boots", Slot = EquipSlot.None, FloorSheet = "GateItems",
            Description = "Boots made from the Slime King's jelly! Walk into a gap and you'll hop right over it.",
            Glow = new Color(0.5f, 1f, 0.7f), KeyItem = true,
        },
        new ItemSpec
        {
            Id = "amethyst", Asset = "Amethyst", Name = "The Amethyst", Slot = EquipSlot.None, FloorSheet = "GateItems",
            Description = "Amethyra's own gem, the heart of the crown. Take it to her, and the crystals will crumble away.",
            Glow = new Color(0.75f, 0.45f, 1f), KeyItem = true,
        },
        new ItemSpec
        {
            Id = "dragon_egg_castle", Asset = "DragonEgg", Name = "Amethyst Dragon Egg", Slot = EquipSlot.None, FloorSheet = "GateItems",
            Description = "One of Amethyra's five lost eggs. It's warm, and every so often it wiggles.",
            Glow = new Color(0.8f, 0.55f, 1f), KeyItem = true,
        },
        // The Glimmer Mines' treasures: the Mole Mitts (an ability), the Topaz (the region's gem) and its egg.
        new ItemSpec
        {
            Id = Abilities.MoleMitts, Asset = "MoleMitts", Name = "Mole Mitts", Slot = EquipSlot.None, FloorSheet = "GateItems",
            Description = "Big, strong digging paws! Lean on a stone block to shove it along, and press E at loose soil to dig.",
            Glow = new Color(1f, 0.65f, 0.7f), KeyItem = true,
        },
        new ItemSpec
        {
            Id = "topaz", Asset = "Topaz", Name = "The Topaz", Slot = EquipSlot.None, FloorSheet = "GateItems",
            Description = "A golden gem from the heart of the Mines. It was keeping the island's lamps lit.",
            Glow = new Color(1f, 0.75f, 0.3f), KeyItem = true,
        },
        new ItemSpec
        {
            Id = "dragon_egg_mines", Asset = "DragonEggMines", Name = "Topaz Dragon Egg", Slot = EquipSlot.None, FloorSheet = "GateItems",
            Description = "One of Amethyra's five lost eggs, golden as honey. It hums when you hold it.",
            Glow = new Color(1f, 0.75f, 0.3f), KeyItem = true,
        },
        // Puddlebrook Lake's treasures: the Bubble Charm (an ability), the Aquamarine, its egg and the Fishing Rod.
        new ItemSpec
        {
            Id = Abilities.BubbleCharm, Asset = "BubbleCharm", Name = "Bubble Charm", Slot = EquipSlot.None, FloorSheet = "GateItems",
            Description = "A shimmering bubble on a silver chain. Wear it and you can swim: walk right into deep water.",
            Glow = new Color(0.7f, 0.85f, 1f), KeyItem = true,
        },
        new ItemSpec
        {
            Id = "aquamarine", Asset = "Aquamarine", Name = "The Aquamarine", Slot = EquipSlot.None, FloorSheet = "GateItems",
            Description = "A sea-green gem from the Lake's heart. The merfolk say it keeps the water clear.",
            Glow = new Color(0.45f, 0.95f, 0.85f), KeyItem = true,
        },
        new ItemSpec
        {
            Id = "dragon_egg_lake", Asset = "DragonEggLake", Name = "Aquamarine Dragon Egg", Slot = EquipSlot.None, FloorSheet = "GateItems",
            Description = "One of Amethyra's five lost eggs, sea-green and cool. It sloshes a little.",
            Glow = new Color(0.45f, 0.95f, 0.85f), KeyItem = true,
        },
        new ItemSpec
        {
            Id = "fishing_rod", Asset = "FishingRod", Name = "Fishing Rod", Slot = EquipSlot.None, FloorSheet = "GateItems",
            Description = "Captain Clamshell's old rod. You get longer to catch a bite, and the big fish like it better.",
            Glow = new Color(1f, 0.85f, 0.5f), KeyItem = true,
        },
        // Frostpeak's treasures: the Rainbow Chalk (an ability), the Sapphire, its egg, and Mr. Frost's scarf chain.
        new ItemSpec
        {
            Id = Abilities.RainbowChalk, Asset = "RainbowChalk", Name = "Rainbow Chalk", Slot = EquipSlot.None, FloorSheet = "GateItems",
            Description = "A fat stick of rainbow chalk. Press E at a rainbow post and it draws a bridge across the chasm.",
            Glow = new Color(1f, 0.8f, 0.9f), KeyItem = true,
        },
        new ItemSpec
        {
            Id = "sapphire", Asset = "Sapphire", Name = "The Sapphire", Slot = EquipSlot.None, FloorSheet = "GateItems",
            Description = "A deep-blue gem from the mountain's heart. It's been keeping the snow from falling for three hundred years.",
            Glow = new Color(0.5f, 0.65f, 1f), KeyItem = true,
        },
        new ItemSpec
        {
            Id = "dragon_egg_frost", Asset = "DragonEggFrost", Name = "Sapphire Dragon Egg", Slot = EquipSlot.None, FloorSheet = "GateItems",
            Description = "One of Amethyra's five lost eggs, deep blue and frosty. It's cold on the outside and warm on the inside.",
            Glow = new Color(0.5f, 0.65f, 1f), KeyItem = true,
        },
        new ItemSpec
        {
            Id = "ball_of_yarn", Asset = "BallOfYarn", Name = "Ball of Yarn", Slot = EquipSlot.None, FloorSheet = "GateItems",
            Description = "A ball of soft red wool from Barnaby's stall. Granny Purl, on Frostpeak, will want this.",
            Glow = new Color(1f, 0.4f, 0.5f), KeyItem = true,
        },
        new ItemSpec
        {
            Id = "warm_scarf", Asset = "WarmScarf", Name = "Warm Scarf", Slot = EquipSlot.None, FloorSheet = "GateItems",
            Description = "A long, soft red-and-cream scarf, knitted by Granny Purl. Mr. Frost will be thrilled.",
            Glow = new Color(1f, 0.5f, 0.5f), KeyItem = true,
        },
        new ItemSpec
        {
            Id = "snow_hat", Asset = "SnowHat", Name = "Snow Hat", Slot = EquipSlot.Hat, FloorSheet = "GateItems",
            Description = "A woolly white bobble hat from Mr. Frost. Toasty warm, with a big pom-pom.",
            Glow = new Color(0.8f, 0.9f, 1f),
            Hearts = 1, Magic = 10,
        },
        new ItemSpec
        {
            Id = "clover_charm", Asset = "CloverCharm", Name = "Lucky Clover Charm", Slot = EquipSlot.Charm, FloorSheet = "WoodsItems",
            Description = "A four-leaf clover on a gold ring. Your spells recharge a little faster.",
            Glow = new Color(0.5f, 1f, 0.5f),
            RechargePercent = 10, WalkPercent = 5,
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
        so.FindProperty("keyItem").boolValue = spec.KeyItem;
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
