using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// Keeps the HUD (Assets/UI/Hud.uxml + Hud.uss) in sync with the game.
// The pattern: find elements by name once (like getElementById), then each frame
// only change values: text, widths, and CSS-style classes that Hud.uss reacts to.
//
// It's built for a young player: hearts instead of numbers, "6 monsters left" with
// progress markers, one big spell slot, and one short hint at a time. Getting hurt is
// easy to read: the lost heart jumps, the screen's edges flash red, and the last heart
// left beats.
[RequireComponent(typeof(UIDocument))]
public class HudController : MonoBehaviour
{
    [SerializeField] private float toastSeconds = 3f;
    [SerializeField] private AudioClip clickSound; // inventory open/close and equip
    [SerializeField] private AudioClip eatSound;   // eating food from the bag

    // The player's parts, handed over by LevelBootstrap via Bind() when it spawns them.
    private Health playerHealth;
    private Mana playerMana;
    private SpellAbility spell;
    private HeroAbility[] abilities; // hotbar slots 2 and 3, once learned
    private PlayerInteractor interactor;
    private Inventory inventory;
    private CharacterDefinition character;
    private ExitZone exit;
    private bool exitWasOpen;

    private const float CooldownHeight = 48f; // matches .spell-slot .slot-icon in Hud.uss
    private const int MaxPips = 16;
    private const int FullSizeHearts = 8;  // more than this and they shrink to fit one row...
    private const int MaxHeartsShown = 12; // ...and past this, one heart and a count ("7 / 14")
    private const string DetailsHint = "Hover an item to inspect it. Click to put it on, take it off, or eat it.";
    // The worn slots, in the order the inventory panel shows them.
    private static readonly EquipSlot[] WornSlots =
        { EquipSlot.Weapon, EquipSlot.Helm, EquipSlot.Armor, EquipSlot.Boots, EquipSlot.Ring, EquipSlot.Hat, EquipSlot.Charm };
    private const int KeySlotCount = 10; // the treasures tab's grid
    // What the details box says on the treasures tab: your collection so far, then how to read about each treasure.
    private static string TreasureHint =>
        $"Heart pieces {Collectible.HeartPieces % Collectible.PiecesPerHeart}/{Collectible.PiecesPerHeart} · Star shards {Collectible.StarShards}. " +
        "Hover a treasure to read about it.";

    // True while the mouse is over a clickable part of the HUD, so gameplay can ignore
    // that click (SpellAbility doesn't cast when you click an inventory slot).
    public static bool PointerOverUi { get; private set; }

    public bool IsInventoryOpen => inventoryPanel.ClassListContains("open");
    public bool IsHelpOpen => helpPanel.ClassListContains("open");
    public string SpellKey => GameInput.UsingGamepad ? "X" : usedMouseLast ? "Click" : "Space";

    private VisualElement root, manaFill, spellSlot, spellCooldown, banner, objectiveCard, monstersRow, sleepFade;
    private Label enemiesLeft, objectiveHint, bannerTitle, bannerSubtitle, prompt, contextHint, toast, spellKey, helpPill;
    private VisualElement heartsRow, pipsRow, xpFill, helpPanel, hurtFlash, goldIcon, spellFlash;
    private Label levelBadge, goldText, pointsHint, heartsCount;
    private readonly List<VisualElement> hearts = new List<VisualElement>();
    private readonly List<VisualElement> pips = new List<VisualElement>();
    private Progression progress;
    private BossAbilities boss;
    private VisualElement bossBar, bossFill, targetCard, targetFill;
    private Label targetName, targetKey;
    private float toastHideAt;
    private VisualElement failBadge, failIcon, abilitiesGroup, itemsGroup;
    private Label failText;
    private float failHideAt;
    private QuestLogView questLog;
    private VisualElement tracker, trackerPortrait, trackerPips, trackerNext;
    private Label trackerTitle, trackerStep, trackerWhere;
    private string trackerShown = "";
    private SkillTreeView skillTree;
    private VisualElement inventoryPanel;
    private readonly Dictionary<EquipSlot, VisualElement> equipSlots = new Dictionary<EquipSlot, VisualElement>();
    private VisualElement[] bagSlots, keySlots;
    private VisualElement bagGrid, treasureGrid, bagTab, treasureTab;
    private bool treasuresShown;
    private int hoveredBag = -1; // the bag slot under the mouse (so 2-5 can put it on a quick slot)
    private readonly List<QuickSlot> quickSlots = new List<QuickSlot>();
    private Label itemDetails, statDamage, statGear;
    private VisualElement tooltip;
    private Label tipName, tipKind, tipDesc, tipStats, tipAction;
    private readonly List<AbilitySlot> abilitySlots = new List<AbilitySlot>();

    // One consumable slot in the hotbar (keys 2-5): its column (slot + key), and its parts.
    private class QuickSlot
    {
        public VisualElement Item, Slot, Icon;
        public Label Key, Count;
    }

    // One learned-ability slot in the hotbar: its column (slot + key), the slot, and its parts.
    private class AbilitySlot
    {
        public HeroAbility Ability;
        public VisualElement Item, Slot, Cooldown;
        public Label Key;
        public System.Action OnUsed;
    }

    // Remembered between frames, to spot changes worth reacting to.
    private int lastHealth = -1, lastAlive, defeated, lastGold = -1;
    private bool lastHalf;
    private float nextHeartbeat;
    private bool usedMouseLast, hasWalked;

    // First-session coach (see FirstSteps): when the player last made progress or got hurt, the idle
    // help that is showing, and the last failed interaction (the same message three times is "stuck").
    public static float IdleSeconds = 40f; // (tests shorten it)
    private const float HelpSeconds = 12f, HelpEvery = 120f, CombatQuietSeconds = 8f;
    private float lastProgressAt, lastHurtAt = -999f, helpUntil, nextHelpAt;
    private string helpText = "", lastFailMessage = "";
    private float lastFailAt;
    private int failCount;
    private Label objectiveGoal;
    private Npc[] friends;
    private Chest[] chests;
    private Bramble[] brambles;
    private Vector3 startPosition;
    private float startTime;

    public void Bind(GameObject player, CharacterDefinition who)
    {
        playerHealth = player.GetComponent<Health>();
        playerMana = player.GetComponent<Mana>();
        spell = player.GetComponent<SpellAbility>();
        abilities = player.GetComponents<HeroAbility>();
        interactor = player.GetComponent<PlayerInteractor>();
        inventory = player.GetComponent<Inventory>();
        character = who;
    }

    // Start, not OnEnable: UIDocument builds its element tree in its own OnEnable,
    // and Unity doesn't guarantee which component's OnEnable runs first.
    private void Start()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        root.pickingMode = PickingMode.Ignore; // the full-screen root itself shouldn't count as "UI"

        heartsRow = root.Q("hearts");
        heartsCount = root.Q<Label>("hearts-count");
        spellFlash = root.Q("spell-flash");
        manaFill = root.Q("mana-fill");
        spellSlot = root.Q("slot-spell");
        spellCooldown = root.Q("spell-cooldown");
        spellKey = root.Q<Label>("spell-key");
        objectiveCard = root.Q("objective");
        monstersRow = root.Q("objective-monsters");
        enemiesLeft = root.Q<Label>("enemies-left");
        pipsRow = root.Q("objective-pips");
        objectiveHint = root.Q<Label>("objective-hint");
        objectiveGoal = root.Q<Label>("objective-goal");
        banner = root.Q("banner");
        sleepFade = root.Q("sleep-fade");
        bannerTitle = root.Q<Label>("banner-title");
        bannerSubtitle = root.Q<Label>("banner-subtitle");
        helpPill = root.Q<Label>("help-pill");
        prompt = root.Q<Label>("interact-prompt");
        toast = root.Q<Label>("toast");
        helpPanel = root.Q("help");
        xpFill = root.Q("xp-fill");
        levelBadge = root.Q<Label>("level-badge");
        goldText = root.Q<Label>("gold-text");
        pointsHint = root.Q<Label>("points-hint");
        goldIcon = root.Q(className: "gold-icon");
        hurtFlash = root.Q("hurt-flash");
        failBadge = root.Q("fail-badge");
        failIcon = root.Q("fail-icon");
        failText = root.Q<Label>("fail-text");
        root.Q("fail-no").style.backgroundImage = new StyleBackground(FeedbackArt.NoBadge());
        abilitiesGroup = root.Q("abilities-group");
        itemsGroup = root.Q("items-group");
        questLog = GetComponent<QuestLogView>();
        tracker = root.Q("tracker");
        trackerPortrait = root.Q("tracker-portrait");
        trackerPips = root.Q("tracker-pips");
        trackerNext = root.Q("tracker-next");
        trackerTitle = root.Q<Label>("tracker-title");
        trackerStep = root.Q<Label>("tracker-step");
        trackerWhere = root.Q<Label>("tracker-where");
        tracker.RegisterCallback<ClickEvent>(_ => { if (questLog != null) questLog.SetOpen(true); }); // click it for the whole log
        skillTree = GetComponent<SkillTreeView>();

        // Contextual hints ("Space: Magic!") share the prompt's spot but are their own label,
        // so they never get mixed up with "E: Open chest".
        contextHint = new Label { name = "context-hint", pickingMode = PickingMode.Ignore };
        contextHint.AddToClassList("panel");
        contextHint.AddToClassList("interact-prompt");
        prompt.parent.Add(contextHint);

        interactor.Interacted += ShowToast;
        ActionFeedback.Failed += OnActionFailed;
        SaveSystem.SaveFailed += OnSaveFailed;
        interactor.Interacted += OnInteracted;
        playerHealth.Damaged += OnPlayerHurt;
        spell.Cast += OnCast;
        progress = GameSession.Progress;
        progress.LeveledUp += OnLevelUp;
        progress.SkillLearned += OnSkillLearned;

        SetUpInventory();
        SetUpAbilitySlots();
        SetUpQuickSlots();

        // Fill in who we're playing and where.
        root.Q("portrait").style.backgroundImage = new StyleBackground(character.Portrait);
        root.Q<Label>("player-name").text = character.DisplayName;
        spellSlot.Q(className: "slot-icon").style.backgroundImage = new StyleBackground(spell.Icon);
        root.Q<Label>("spell-cost").text = spell.ManaCost.ToString("0");
        root.Q<Label>("objective-title").text = LevelBootstrap.Current.Title;

        bossBar = root.Q("boss-bar");
        bossFill = root.Q("boss-fill");
        targetCard = root.Q("target-card");
        targetFill = root.Q("target-fill");
        targetName = root.Q<Label>("target-name");
        targetKey = root.Q<Label>("target-key");
        boss = FindAnyObjectByType<BossAbilities>();
        if (boss != null)
        {
            root.Q<Label>("boss-name").text = boss.BossName;
            boss.Announced += ShowToast;
        }

        exit = FindAnyObjectByType<ExitZone>();
        exitWasOpen = ObjectiveDone;
        lastAlive = EnemyAI.AliveCount;
        startPosition = playerHealth.transform.position;
        startTime = Time.time;
        lastProgressAt = Time.time;
        friends = FindObjectsByType<Npc>();
        chests = FindObjectsByType<Chest>();
        brambles = FindObjectsByType<Bramble>();
        FirstSteps.SkipIfExperienced();
    }

    private void Update()
    {
        HandleKeys();
        PointerOverUi = IsPointerOverUi();
        // Floating panels aren't for fighting: GameInput.ActionsBlocked keeps clicks and buttons out of the spells.
        GameInput.OverlayOpen = IsInventoryOpen || IsHelpOpen || (questLog != null && questLog.IsOpen) || (skillTree != null && skillTree.IsOpen);
        failBadge.EnableInClassList("visible", Time.time < failHideAt);
        statDamage.text = $"{spell.SpellName} damage: {spell.Damage}";

        UpdateHearts();
        manaFill.style.width = Length.Percent(100f * playerMana.Current / playerMana.Max);
        UpdateSpellSlot();
        UpdateAbilitySlots();
        UpdateQuickSlots();
        UpdateObjective();
        UpdateGoal();
        UpdateTracker();
        UpdateSecondary();
        UpdateHints();

        // Boss health bar: shown once the boss has noticed you, until it falls.
        bool showBoss = boss != null && boss.IsEngaged && !boss.Health.IsDead;
        bossBar.EnableInClassList("visible", showBoss);
        if (showBoss) bossFill.style.width = Length.Percent(100f * boss.Health.Current / boss.Health.Max);

        UpdateTarget(showBoss);

        toast.EnableInClassList("visible", Time.time < toastHideAt);

        var game = GameManager.Instance;
        sleepFade.style.opacity = game != null ? game.SleepFade : 0f;
        bool over = game != null && game.IsGameOver;
        banner.EnableInClassList("visible", over);
        if (over)
        {
            banner.EnableInClassList("won", game.PlayerWon);
            banner.EnableInClassList("lost", !game.PlayerWon);
            bannerTitle.text = game.PlayerWon ? "You did it!" : "Oh no! Try again?";
            bannerSubtitle.text = GameInput.UsingGamepad ? "A: try again      Start: menu" : "R: try again      Esc: menu";
        }
        helpPill.text = $"{GameInput.HelpKey}: Help";
    }

    // The monster the spell is aimed at: its name and health, with the key that picks another. (A boss that's
    // the target already has its own big bar just above, so the card sits that one out.)
    private void UpdateTarget(bool bossBarShown)
    {
        var target = spell.Target;
        bool show = target != null && !(bossBarShown && target.Health == boss.Health);
        targetCard.EnableInClassList("visible", show);
        if (!show) return;
        targetName.text = target.DisplayName;
        targetKey.text = GameInput.TargetKey;
        targetFill.style.width = Length.Percent(100f * target.Health.Current / target.Health.Max);
    }

    private void HandleKeys()
    {
        if (GameInput.GameplayBlocked) return;
        if (GameInput.InventoryPressed)
        {
            SetInventoryOpen(!IsInventoryOpen);
            AudioManager.Play(clickSound, 0.6f);
        }
        if (GameInput.HelpPressed)
        {
            SetHelpOpen(!IsHelpOpen);
            AudioManager.Play(clickSound, 0.6f);
        }
        if (GameInput.TipsPressed) ToggleTips();
        // 2-5 on the bag (hovering an item) assigns it; with another panel open they're not for eating.
        if (!GameInput.OverlayOpen || IsInventoryOpen) HandleQuickKeys();
        // Which button does this player cast with? Show that one on the spell slot.
        if (GameInput.ClickPressed && !PointerOverUi) usedMouseLast = true;
        if (GameInput.CastPressed) usedMouseLast = false;
    }

    // ---------- Hearts ----------

    private void UpdateHearts()
    {
        int max = playerHealth.Max, current = playerHealth.Current;
        // Gentle Mode: a half hit shows as the last full heart broken in half.
        bool half = current > 0 && GameManager.Instance != null && GameManager.Instance.HalfHeartLost;
        if (hearts.Count != max) // first frame, or a level-up added a heart
        {
            heartsRow.Clear();
            hearts.Clear();
            for (int i = 0; i < max; i++)
            {
                var heart = new VisualElement { pickingMode = PickingMode.Ignore };
                heart.AddToClassList("heart");
                heartsRow.Add(heart);
                hearts.Add(heart);
            }
            // Always one row: smaller hearts past 8, and past 12 just one heart with a count.
            heartsRow.EnableInClassList("compact", max > FullSizeHearts);
            heartsCount.EnableInClassList("visible", max > MaxHeartsShown);
            for (int i = 0; i < max; i++)
                hearts[i].style.display = max > MaxHeartsShown && i > 0 ? DisplayStyle.None : DisplayStyle.Flex;
        }
        bool counted = max > MaxHeartsShown;
        for (int i = 0; i < hearts.Count; i++)
        {
            // Counted: the one heart shown stands for the hero's state (full, half, or empty at 0).
            hearts[i].EnableInClassList("empty", counted ? current == 0 : i >= current);
            hearts[i].EnableInClassList("half", half && i == (counted ? 0 : current - 1));
        }
        if (counted) heartsCount.text = $"{current} / {max}";

        if (lastHealth >= 0)
        {
            // Healing: the hearts that just refilled pop for a moment.
            for (int i = lastHealth; i < current && i < hearts.Count; i++)
                Pop(hearts[counted ? 0 : i]);
            // Hurt: the hearts just lost (or just broken in half) jump and tilt.
            for (int i = current; i < lastHealth && i < hearts.Count; i++)
                Pop(hearts[counted ? 0 : i], "hurt", 220);
            if (half && !lastHalf && current == lastHealth)
                Pop(hearts[counted ? 0 : current - 1], "hurt", 220);
        }
        lastHealth = current;
        lastHalf = half;

        // Down to the last heart: it beats, to say "careful!"
        if (current == 1 && max > 1 && Time.time >= nextHeartbeat)
        {
            nextHeartbeat = Time.time + 0.9f;
            Pop(hearts[0], "beat", 150);
        }
    }

    // Any hit, even a Gentle Mode bump that costs no heart: the screen's edges flash red.
    private void OnPlayerHurt(Health _)
    {
        lastHurtAt = Time.time;
        Pop(hurtFlash, "visible", 140);
    }

    // The spell went off: the slot dips and a four-point star flashes over it, briefly.
    private void OnCast()
    {
        lastProgressAt = Time.time;
        FirstSteps.Complete(FirstStep.Spell);
        Pop(spellSlot, "cast", 90);
        Pop(spellFlash, "visible", 90);
    }

    // Add a class, then remove it a moment later: USS transitions animate both ways.
    // (element.schedule is UI Toolkit's own timer, like setTimeout in a browser.)
    private static void Pop(VisualElement element, string className = "pop", long ms = 180)
    {
        element.AddToClassList(className);
        element.schedule.Execute(() => element.RemoveFromClassList(className)).StartingIn(ms);
    }

    // ---------- Spell ----------

    private void UpdateSpellSlot()
    {
        // The shade covers the icon fully right after casting and shrinks to nothing when ready.
        spellCooldown.style.height = CooldownHeight * (1f - spell.CooldownProgress);
        spellSlot.EnableInClassList("no-mana", !spell.CanAfford);
        spellSlot.EnableInClassList("ready", spell.CooldownProgress >= 1f && spell.CanAfford);
        spellKey.text = SpellKey;
    }

    // ---------- Abilities (hotbar slots 2 and 3) ----------

    private void SetUpAbilitySlots()
    {
        foreach (var ability in abilities)
        {
            var item = root.Q($"ability-{ability.Slot}");
            if (item == null) continue;
            var entry = new AbilitySlot
            {
                Ability = ability,
                Item = item,
                Slot = item.Q(className: "slot"),
                Cooldown = item.Q(className: "slot-cooldown"),
                Key = item.Q<Label>(className: "spell-key"),
            };
            entry.Slot.Q(className: "slot-icon").style.backgroundImage = new StyleBackground(ability.Icon);
            entry.Slot.Q<Label>(className: "slot-cost").text = ability.ManaCost.ToString("0");
            entry.OnUsed = () => Pop(entry.Slot, "cast", 90);
            ability.Used += entry.OnUsed;
            abilitySlots.Add(entry);
        }
    }

    private void UpdateAbilitySlots()
    {
        foreach (var s in abilitySlots)
        {
            var a = s.Ability;
            s.Item.EnableInClassList("unlocked", a.Unlocked); // the slot appears once its skill is learned
            s.Cooldown.style.height = CooldownHeight * (1f - a.CooldownProgress);
            s.Slot.EnableInClassList("no-mana", !a.CanAfford);
            s.Key.text = GameInput.AbilityKey(a.Slot);
        }
        abilitiesGroup.EnableInClassList("unlocked", abilitySlots.Exists(s => s.Ability.Unlocked));
    }

    // ---------- Quick slots (hotbar slots 2-5: consumables) ----------

    private void SetUpQuickSlots()
    {
        for (int i = 0; i < InventoryState.QuickSlotCount; i++)
        {
            var item = root.Q($"quick-{i}");
            if (item == null) continue;
            var slot = item.Q(className: "slot");
            int index = i;
            slot.RegisterCallback<ClickEvent>(_ => UseQuickSlot(index)); // clicking works too
            quickSlots.Add(new QuickSlot
            {
                Item = item,
                Slot = slot,
                Icon = slot.Q(className: "slot-icon"),
                Key = item.Q<Label>(className: "spell-key"),
                Count = slot.Q<Label>(className: "slot-cost"),
            });
        }
    }

    // A slot appears once it holds something, shows how many are left, and fades when you've run out.
    private void UpdateQuickSlots()
    {
        for (int i = 0; i < quickSlots.Count; i++)
        {
            var q = quickSlots[i];
            var item = inventory.Quick(i);
            q.Item.EnableInClassList("unlocked", item != null);
            if (item == null) continue;
            int count = inventory.Count(item);
            q.Icon.style.backgroundImage = new StyleBackground(item.Icon);
            q.Count.text = count.ToString();
            q.Slot.EnableInClassList("no-mana", count == 0);
            q.Key.text = GameInput.QuickKey(i);
        }
        itemsGroup.EnableInClassList("unlocked", quickSlots.Exists(q => q.Item.ClassListContains("unlocked")));
    }

    // 2-5 eats what's on that slot. With the bag open and the mouse over a consumable, 2-5 puts it
    // on that slot instead (and a slot's item can be swapped the same way).
    private void HandleQuickKeys()
    {
        for (int i = 0; i < InventoryState.QuickSlotCount; i++)
        {
            if (!GameInput.QuickPressed(i)) continue;
            var hovered = IsInventoryOpen ? BagItem(hoveredBag) : null;
            if (hovered != null && hovered.IsConsumable)
            {
                if (inventory.AssignQuick(i, hovered))
                {
                    ShowToast($"{hovered.DisplayName} is on slot {GameInput.QuickKey(i)}.");
                    AudioManager.Play(clickSound, 0.6f);
                }
            }
            else UseQuickSlot(i);
        }
    }

    private void UseQuickSlot(int slot)
    {
        if (GameManager.Instance != null && !GameManager.Instance.PlayerCanAct) return;
        var item = inventory.Quick(slot);
        if (item == null) return;
        if (!EatFromBag(item)) ShowToast($"No {item.DisplayName} left!");
    }

    // Eats one (from the bag or a quick slot), with the munch and the toast.
    private bool EatFromBag(ItemDefinition item)
    {
        if (!inventory.Eat(item)) return false;
        FirstSteps.Complete(FirstStep.Treasure);
        itemDetails.text = $"Yum! You eat the {item.DisplayName}. {item.FoodText}!";
        ShowToast($"Yum! {item.DisplayName}! {item.FoodText}");
        AudioManager.Play(eatSound != null ? eatSound : clickSound);
        return true;
    }

    private void OnSkillLearned(SkillDefinition skill)
    {
        if (!skill.IsAbility) return;
        foreach (var s in abilitySlots)
            if (s.Ability.SkillId == skill.Id)
                ShowToast($"New ability: {skill.Name}! Press {GameInput.AbilityKey(s.Ability.Slot)}");
    }

    // ---------- Objective ----------

    private void UpdateObjective()
    {
        // Count defeats as the number alive drops (a boss's slimelings add to the total).
        int alive = EnemyAI.AliveCount;
        if (alive < lastAlive) { defeated += lastAlive - alive; lastProgressAt = Time.time; }
        lastAlive = alive;
        int total = defeated + alive;

        bool showCount = LevelBootstrap.Current.ShowEnemyCount;
        bool exitOpen = ObjectiveDone;

        enemiesLeft.text = alive == 1 ? "1 monster left" : $"{alive} monsters left";
        monstersRow.style.display = showCount && !exitOpen ? DisplayStyle.Flex : DisplayStyle.None;

        if (pips.Count != Mathf.Min(total, MaxPips))
        {
            pipsRow.Clear();
            pips.Clear();
            for (int i = 0; i < Mathf.Min(total, MaxPips); i++)
            {
                var pip = new VisualElement { pickingMode = PickingMode.Ignore };
                pip.AddToClassList("pip");
                pipsRow.Add(pip);
                pips.Add(pip);
            }
        }
        for (int i = 0; i < pips.Count; i++)
            pips[i].EnableInClassList("defeated", i < defeated);
        pipsRow.style.display = showCount && total > 0 ? DisplayStyle.Flex : DisplayStyle.None;

        string hint = LevelBootstrap.Current.Hint(exitOpen);
        objectiveHint.text = hint;
        objectiveHint.style.display = string.IsNullOrEmpty(hint) ? DisplayStyle.None : DisplayStyle.Flex;
        objectiveHint.EnableInClassList("open", exitOpen && exit != null);

        if (exitOpen && !exitWasOpen)
        {
            if (exit != null) ShowToast("The stairs are open!"); // (a boss on a level with no exit announces its own defeat)
            Celebrate();
        }
        exitWasOpen = exitOpen;
    }

    // The level's goal is met: its exit is open or, with no exit crystal but a boss (Hollow Farm's Pumpkin
    // King), the boss is beaten. A destroyed boss compares equal to null (Unity overloads ==).
    private bool ObjectiveDone => exit != null ? exit.IsOpen : boss == null || boss.Health.IsDead;

    // A few quick bounces of the objective card.
    private void Celebrate()
    {
        for (int i = 0; i < 3; i++)
        {
            objectiveCard.schedule.Execute(() => objectiveCard.AddToClassList("celebrate")).StartingIn(i * 320);
            objectiveCard.schedule.Execute(() => objectiveCard.RemoveFromClassList("celebrate")).StartingIn(i * 320 + 160);
        }
    }

    // ---------- Level, experience, coins (deliberately quieter) ----------

    private void UpdateSecondary()
    {
        levelBadge.text = progress.Level.ToString();
        bool maxed = progress.Level >= Progression.MaxLevel;
        xpFill.style.width = Length.Percent(maxed ? 100f : 100f * progress.Xp / progress.XpForNextLevel);
        goldText.text = progress.Gold.ToString();
        if (lastGold >= 0 && progress.Gold > lastGold) Pop(goldIcon, "pop", 140); // a coin came in
        lastGold = progress.Gold;
        pointsHint.text = progress.SkillPoints > 0
            ? $"{progress.SkillPoints} skill point{(progress.SkillPoints > 1 ? "s" : "")} · K"
            : "";
        pointsHint.EnableInClassList("visible", progress.SkillPoints > 0);
    }

    // ---------- Hints: one short line at a time ----------

    private void UpdateHints()
    {
        var usable = interactor.Current;
        prompt.EnableInClassList("visible", usable != null);
        if (usable != null) prompt.text = $"{GameInput.InteractKey}: {usable.Prompt}";

        if (!hasWalked)
            hasWalked = Vector3.Distance(playerHealth.transform.position, startPosition) > 2f
                        || Time.time - startTime > 10f;
        if (hasWalked) FirstSteps.Complete(FirstStep.Walk);

        string hint = null;
        if (usable == null && !DialogueController.IsOpen && GameManager.Instance != null && !GameManager.Instance.IsGameOver)
        {
            if (!hasWalked)
                hint = GameInput.UsingGamepad ? "Stick: Walk" : "W A S D: Walk";
            else if (Time.time - spell.LastCastTime > 6f && spell.CanAfford && spell.FindTarget() != null)
                hint = $"{SpellKey}: Magic!";
            else
                hint = CoachHint();
        }
        contextHint.text = hint ?? "";
        contextHint.EnableInClassList("visible", hint != null);
    }

    // ---------- First-session coach (see FirstSteps) ----------

    private bool InCastle => SceneManager.GetActiveScene().name == "Level0";

    // Any monster in reach of the spell, or a recent hit: the player is fighting, so the coach stays quiet.
    private bool InCombat => Time.time - lastHurtAt < CombatQuietSeconds || spell.Target != null;

    // G hides the tips for good (it's saved); pressing it again brings them back from the first step.
    public void ToggleTips()
    {
        bool turnOn = !FirstSteps.Enabled;
        FirstSteps.SetEnabled(turnOn);
        helpUntil = 0f;
        lastProgressAt = Time.time;
        ShowToast(turnOn ? "Tips are back on." : $"Tips hidden. {GameInput.TipsKey} brings them back.");
    }

    // A failed interaction (a locked door says the same thing every time) three times in 20 seconds.
    private void OnInteracted(string message)
    {
        if (string.IsNullOrEmpty(message)) { lastProgressAt = Time.time; failCount = 0; return; }
        failCount = message == lastFailMessage && Time.time - lastFailAt < 20f ? failCount + 1 : 1;
        lastFailMessage = message;
        lastFailAt = Time.time;
        if (failCount >= 3) StartHelp("Can't open it yet? Look for a picture bubble, or try somewhere else first.");
        else lastProgressAt = Time.time;
    }

    private void StartHelp(string text)
    {
        if (!FirstSteps.Enabled || Time.time < nextHelpAt) return;
        helpText = $"{text}   ({GameInput.TipsKey}: hide tips)";
        helpUntil = Time.time + HelpSeconds;
        nextHelpAt = helpUntil + HelpEvery;
    }

    // The one line the coach has for right now, or null: idle help first, then a nudge for the step the
    // player is on once they're near the thing to do it with. Never while fighting.
    private string CoachHint()
    {
        if (!FirstSteps.Enabled || InCombat) return null;
        if (Time.time >= helpUntil && Time.time - lastProgressAt > IdleSeconds)
            StartHelp(InCastle && FirstSteps.Current != FirstStep.Done
                ? "Not sure what to do? " + FirstSteps.Goal(FirstSteps.Current, NearestUnmetFriend())
                : $"Stuck? {GameInput.QuestLogKeyName}: your quests   {GameInput.MapKey}: the map");
        if (Time.time < helpUntil) return helpText;
        if (!InCastle) return null;

        var here = playerHealth.transform.position;
        switch (FirstSteps.Current)
        {
            case FirstStep.Talk:
                return NearestUnmetFriend() != "" ? $"{GameInput.InteractKey}: Say hello to a friend" : null;
            case FirstStep.Treasure:
                return chests.Any(c => c != null && !c.IsOpen && Near(c.transform.position, here, 8f))
                    ? $"{GameInput.InteractKey}: Open the chest" : null;
            case FirstStep.Obstacle:
                return brambles.Any(b => b != null && !b.IsCleared && Near(b.transform.position, here, 6f))
                    ? $"{SpellKey}: Zap the thorns!" : null;
        }
        return null;
    }

    private static bool Near(Vector3 a, Vector3 b, float range)
    {
        a.y = b.y;
        return (a - b).sqrMagnitude <= range * range;
    }

    // The closest friend the hero hasn't met yet ("" if there's nobody left to meet in this scene).
    private string NearestUnmetFriend()
    {
        var here = playerHealth.transform.position;
        Npc best = null;
        float bestDistance = float.MaxValue;
        foreach (var n in friends)
        {
            if (n == null || n.HasMet) continue;
            float d = (n.transform.position - here).sqrMagnitude;
            if (d < bestDistance) { best = n; bestDistance = d; }
        }
        return best != null ? best.Name : "";
    }

    // The objective card's first line: what to do right now. Only in the castle grounds, while the tips are on.
    private void UpdateGoal()
    {
        string goal = FirstSteps.Enabled && InCastle ? FirstSteps.Goal(FirstSteps.Current, NearestUnmetFriend()) : "";
        objectiveGoal.text = goal;
        objectiveGoal.style.display = goal.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // Progression outlives this scene's HUD, so stop listening when the HUD goes away.
    private void OnDestroy()
    {
        if (progress != null)
        {
            progress.LeveledUp -= OnLevelUp;
            progress.SkillLearned -= OnSkillLearned;
        }
        foreach (var s in abilitySlots)
            if (s.Ability != null) s.Ability.Used -= s.OnUsed;
        if (playerHealth != null) playerHealth.Damaged -= OnPlayerHurt;
        ActionFeedback.Failed -= OnActionFailed;
        SaveSystem.SaveFailed -= OnSaveFailed;
        GameInput.OverlayOpen = false;
        if (spell != null) spell.Cast -= OnCast;
    }

    private void OnLevelUp(int level) => ShowToast($"Level up! You're level {level}!");

    public void SetInventoryOpen(bool open)
    {
        inventoryPanel.EnableInClassList("open", open);
        if (!open) HideTip();
    }
    public void SetHelpOpen(bool open) => helpPanel.EnableInClassList("open", open);

    // ---------- Inventory panel ----------

    private void SetUpInventory()
    {
        inventoryPanel = root.Q("inventory");
        itemDetails = root.Q<Label>("item-details");
        statDamage = root.Q<Label>("stat-damage");
        statGear = root.Q<Label>("stat-gear");
        bagSlots = new VisualElement[inventory.Capacity];
        itemDetails.text = DetailsHint;
        tooltip = root.Q("item-tooltip");
        tipName = root.Q<Label>("tip-name");
        tipKind = root.Q<Label>("tip-kind");
        tipDesc = root.Q<Label>("tip-desc");
        tipStats = root.Q<Label>("tip-stats");
        tipAction = root.Q<Label>("tip-action");
        SetUpDoll();

        // UI Toolkit events work like DOM events: register a callback on an element.
        for (int i = 0; i < bagSlots.Length; i++)
        {
            int index = i; // capture a copy for the lambdas below
            bagSlots[i] = root.Q($"bag-{i}");
            bagSlots[i].RegisterCallback<ClickEvent>(_ => OnBagSlotClicked(index));
            bagSlots[i].RegisterCallback<PointerEnterEvent>(e =>
            {
                hoveredBag = index;
                ShowDetails(BagItem(index), equipped: false, e.position);
            });
            bagSlots[i].RegisterCallback<PointerMoveEvent>(e => MoveTip(e.position));
            bagSlots[i].RegisterCallback<PointerLeaveEvent>(_ =>
            {
                hoveredBag = -1;
                HideTip();
            });
        }

        // The treasures tab: a grid of key items (abilities, lanterns, gems...), look but don't touch.
        bagGrid = root.Q("bag");
        treasureGrid = root.Q("treasures");
        bagTab = root.Q("tab-bag");
        treasureTab = root.Q("tab-treasures");
        keySlots = new VisualElement[KeySlotCount];
        for (int i = 0; i < KeySlotCount; i++)
        {
            int index = i;
            keySlots[i] = root.Q($"key-{i}");
            keySlots[i].RegisterCallback<PointerEnterEvent>(e => ShowDetails(KeyItem(index), equipped: false, e.position));
            keySlots[i].RegisterCallback<PointerMoveEvent>(e => MoveTip(e.position));
            keySlots[i].RegisterCallback<PointerLeaveEvent>(_ => HideTip());
        }
        bagTab.RegisterCallback<ClickEvent>(_ => ShowTreasures(false));
        treasureTab.RegisterCallback<ClickEvent>(_ => ShowTreasures(true));
        ShowTreasures(false);
        foreach (var slot in WornSlots)
        {
            var element = root.Q($"equip-{slot.ToString().ToLowerInvariant()}");
            equipSlots[slot] = element;
            element.RegisterCallback<ClickEvent>(_ =>
            {
                if (inventory.Unequip(slot)) AudioManager.Play(clickSound);
            });
            element.RegisterCallback<PointerEnterEvent>(e => ShowWornDetails(slot, e.position));
            element.RegisterCallback<PointerMoveEvent>(e => MoveTip(e.position));
            element.RegisterCallback<PointerLeaveEvent>(_ => HideTip());
        }

        inventory.Changed += RefreshInventory;
        RefreshInventory();
    }

    private ItemDefinition BagItem(int index) => index >= 0 && index < inventory.Bag.Count ? inventory.Bag[index] : null;
    private ItemDefinition KeyItem(int index) => index >= 0 && index < inventory.KeyItems.Count ? inventory.KeyItems[index] : null;

    public bool TreasuresShown => treasuresShown;

    // The Bag / Treasures tabs.
    public void ShowTreasures(bool show)
    {
        treasuresShown = show;
        bagGrid.style.display = show ? DisplayStyle.None : DisplayStyle.Flex;
        treasureGrid.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        bagTab.EnableInClassList("active", !show);
        treasureTab.EnableInClassList("active", show);
        itemDetails.text = show ? TreasureHint : DetailsHint;
        HideTip();
    }

    private void OnBagSlotClicked(int index)
    {
        var item = BagItem(index);
        if (item != null && item.IsFood && EatFromBag(item)) return;
        if (item != null && inventory.Equip(item))
        {
            HideTip(); // it has moved to the doll
            AudioManager.Play(clickSound);
        }
    }

    private void RefreshInventory()
    {
        for (int i = 0; i < bagSlots.Length; i++)
            SetSlotItem(bagSlots[i], BagItem(i));
        for (int i = 0; i < keySlots.Length; i++)
            SetSlotItem(keySlots[i], KeyItem(i));
        foreach (var pair in equipSlots)
            SetSlotItem(pair.Value, inventory.Equipped(pair.Key));
        statGear.text = GearSummary();
    }

    private static void SetSlotItem(VisualElement slot, ItemDefinition item)
    {
        var icon = slot.Q(className: "slot-icon");
        // StyleKeyword.Null means "no inline value": fall back to the USS (an empty slot).
        icon.style.backgroundImage = item != null ? new StyleBackground(item.Icon) : new StyleBackground(StyleKeyword.Null);
        slot.EnableInClassList("has-item", item != null);
    }

    // ---------- Tooltip ----------

    // The tooltip: the item's name and kind, what it is, each thing it does (in green), and what a click does.
    // `at` is where the pointer is, in panel coordinates; the tooltip sits beside it and follows it (MoveTip).
    public bool TooltipShown => tooltip != null && tooltip.ClassListContains("visible");
    public string TooltipName => tipName.text;
    public string TooltipText => $"{tipName.text}\n{tipKind.text}\n{tipDesc.text}\n{tipStats.text}\n{tipAction.text}";

    private void ShowDetails(ItemDefinition item, bool equipped, Vector2 at)
    {
        if (item == null) { HideTip(); return; }
        string action = equipped ? "Click to take it off."
                      : item.IsEquippable ? $"Click to wear it ({item.Slot.ToString().ToLowerInvariant()})."
                      : item.IsFood ? "Click to eat it. Hover and press 2-5 to put it on the hotbar."
                      : item.IsKeyItem ? "A treasure: it stays with you." : "";
        ShowTip(item.DisplayName, equipped ? $"{item.KindText} · worn" : item.KindText, item.Description,
                string.Join("\n", item.BonusLines.Select(line => "• " + line)), action, at);
    }

    private void ShowWornDetails(EquipSlot slot, Vector2 at)
    {
        var item = inventory.Equipped(slot);
        if (item != null) ShowDetails(item, equipped: true, at);
        else ShowTip(slot.ToString(), "Empty slot", "Nothing yet. Maybe there's one out there somewhere...", "", "", at);
    }

    private void ShowTip(string name, string kind, string description, string stats, string action, Vector2 at)
    {
        tipName.text = name;
        tipKind.text = kind;
        tipDesc.text = description;
        tipStats.text = stats;
        tipAction.text = action;
        tipStats.style.display = stats.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        tipAction.style.display = action.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        tooltip.EnableInClassList("visible", true);
        MoveTip(at);
    }

    private void HideTip()
    {
        if (tooltip != null) tooltip.EnableInClassList("visible", false);
    }

    // Beside the pointer, flipped to its other side (or lifted) when it would run off the screen.
    private void MoveTip(Vector2 at)
    {
        if (!TooltipShown) return;
        const float gap = 18f;
        float width = tooltip.resolvedStyle.width > 1f ? tooltip.resolvedStyle.width : 270f;
        float height = tooltip.resolvedStyle.height > 1f ? tooltip.resolvedStyle.height : 120f;
        float x = at.x + gap, y = at.y + gap;
        if (x + width > root.layout.width) x = at.x - gap - width;
        if (y + height > root.layout.height) y = Mathf.Max(0f, root.layout.height - height);
        tooltip.style.left = Mathf.Max(0f, x);
        tooltip.style.top = y;
    }

    // The paper doll's body: the hero's own picture (the first frame of their Idle, front-on).
    private void SetUpDoll()
    {
        var figure = root.Q("doll-figure");
        var visuals = inventory != null ? inventory.GetComponent<CharacterAnimator>() : null;
        var sprite = visuals != null && visuals.SpriteRenderer != null ? visuals.SpriteRenderer.sprite : null;
        if (figure != null && sprite != null) figure.style.backgroundImage = new StyleBackground(sprite);
    }

    // Everything the worn equipment adds besides spell damage (that's on the line above).
    private string GearSummary()
    {
        var parts = new List<string>();
        if (inventory.MaxHealthBonus != 0) parts.Add($"+{inventory.MaxHealthBonus} {(inventory.MaxHealthBonus == 1 ? "heart" : "hearts")}");
        if (inventory.MaxManaBonus != 0) parts.Add($"+{inventory.MaxManaBonus} magic");
        int walk = Mathf.RoundToInt((inventory.MoveSpeedFactor - 1f) * 100f);
        if (walk != 0) parts.Add($"walk {walk}% faster");
        int recharge = Mathf.RoundToInt((1f - inventory.SpellCooldownFactor) * 100f);
        if (recharge != 0) parts.Add($"spells recharge {recharge}% faster");
        return parts.Count > 0 ? "Gear: " + string.Join(", ", parts) : "";
    }

    private bool IsPointerOverUi()
    {
        var panel = root.panel;
        if (panel == null) return false;
        // The mouse position has y=0 at the bottom; UI Toolkit has y=0 at the top.
        var mouse = GameInput.MousePosition;
        var screen = new Vector2(mouse.x, Screen.height - mouse.y);
        var picked = panel.Pick(RuntimePanelUtils.ScreenToPanel(panel, screen));
        return picked != null; // decorative elements use picking-mode="Ignore", so they don't count
    }

    // ---------- The quest tracker ----------

    // The followed quest, always on screen: who gave it (their portrait), what to do next with its progress, how far
    // along (a dot per step), where to go (the place, or "You're here!"), and a picture of the thing to find or the
    // person to see. It stays hidden until some quest has started.
    public bool TrackerShown => tracker != null && tracker.ClassListContains("visible");

    private void UpdateTracker()
    {
        var quest = QuestCatalog.Tracked();
        var step = quest != null ? QuestCatalog.CurrentStep(quest) : null;
        tracker.EnableInClassList("visible", quest != null && step != null);
        if (quest == null || step == null) { trackerShown = ""; return; }

        string here = SceneManager.GetActiveScene().name;
        string where = step.Where.Length == 0 ? "" : step.Where == here ? "You're here!" : $"Go to: {SaveSystem.PlaceName(step.Where)}";
        string signature = $"{quest.Id}|{QuestCatalog.StepText(step)}|{where}|{QuestCatalog.StepsDone(quest)}";
        if (signature == trackerShown) return;
        trackerShown = signature;

        trackerTitle.text = quest.Title;
        trackerStep.text = QuestCatalog.StepText(step);
        trackerWhere.text = where;
        trackerWhere.style.display = where.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        var giver = questLog != null ? questLog.PictureFor(quest.Giver.Length > 0 ? "npc:" + quest.Giver : quest.Steps[0].Icon) : null;
        trackerPortrait.style.backgroundImage = giver != null ? new StyleBackground(giver) : new StyleBackground();
        var next = questLog != null ? questLog.PictureFor(step.Icon) : null;
        trackerNext.style.backgroundImage = next != null ? new StyleBackground(next) : new StyleBackground();
        trackerPips.Clear();
        for (int i = 0; i < quest.Steps.Length; i++)
        {
            var pip = new VisualElement { pickingMode = PickingMode.Ignore };
            pip.AddToClassList("tracker-pip");
            pip.EnableInClassList("done", Condition.Met(quest.Steps[i].Done));
            trackerPips.Add(pip);
        }
    }

    // ---------- Failed actions ----------

    // A save that didn't go through says so (and that the last one is safe): never a silent "success".
    private void OnSaveFailed(string reason) => ShowToast("Couldn't save your adventure just now. Your last save is safe.");

    // "That didn't work, and here's why": the reason's picture with a red "no" on it, and a few words.
    private void OnActionFailed(FailReason reason, string message, Sprite icon)
    {
        failIcon.style.backgroundImage = new StyleBackground(icon);
        failText.text = message;
        failHideAt = Time.time + 1.8f;
        failBadge.EnableInClassList("visible", true);
        Pop(failBadge, "shake", 70);
    }

    // ---------- Toast ----------

    public void ShowToast(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        toast.text = message;
        toastHideAt = Time.time + toastSeconds;
    }
}
