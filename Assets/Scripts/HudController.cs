using System.Collections.Generic;
using UnityEngine;
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
    private const string DetailsHint = "Hover an item to inspect it. Click to put it on or take it off.";
    // The worn slots, in the order the inventory panel shows them.
    private static readonly EquipSlot[] WornSlots = { EquipSlot.Weapon, EquipSlot.Helm, EquipSlot.Armor, EquipSlot.Boots, EquipSlot.Ring };

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
    private VisualElement bossBar, bossFill;
    private float toastHideAt;
    private VisualElement inventoryPanel;
    private readonly Dictionary<EquipSlot, VisualElement> equipSlots = new Dictionary<EquipSlot, VisualElement>();
    private VisualElement[] bagSlots;
    private Label itemDetails, statDamage, statGear;
    private readonly List<AbilitySlot> abilitySlots = new List<AbilitySlot>();

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

        // Contextual hints ("Space: Magic!") share the prompt's spot but are their own label,
        // so they never get mixed up with "E: Open chest".
        contextHint = new Label { name = "context-hint", pickingMode = PickingMode.Ignore };
        contextHint.AddToClassList("panel");
        contextHint.AddToClassList("interact-prompt");
        prompt.parent.Add(contextHint);

        interactor.Interacted += ShowToast;
        playerHealth.Damaged += OnPlayerHurt;
        spell.Cast += OnCast;
        progress = GameSession.Progress;
        progress.LeveledUp += OnLevelUp;
        progress.SkillLearned += OnSkillLearned;

        SetUpInventory();
        SetUpAbilitySlots();

        // Fill in who we're playing and where.
        root.Q("portrait").style.backgroundImage = new StyleBackground(character.Portrait);
        root.Q<Label>("player-name").text = character.DisplayName;
        spellSlot.Q(className: "slot-icon").style.backgroundImage = new StyleBackground(spell.Icon);
        root.Q<Label>("spell-cost").text = spell.ManaCost.ToString("0");
        root.Q<Label>("objective-title").text = LevelBootstrap.Current.Title;

        bossBar = root.Q("boss-bar");
        bossFill = root.Q("boss-fill");
        boss = FindAnyObjectByType<BossAbilities>();
        if (boss != null)
        {
            root.Q<Label>("boss-name").text = boss.BossName;
            boss.Announced += ShowToast;
        }

        exit = FindAnyObjectByType<ExitZone>();
        exitWasOpen = exit == null || exit.IsOpen;
        lastAlive = EnemyAI.AliveCount;
        startPosition = playerHealth.transform.position;
        startTime = Time.time;
    }

    private void Update()
    {
        HandleKeys();
        PointerOverUi = IsPointerOverUi();
        statDamage.text = $"{spell.SpellName} damage: {spell.Damage}";

        UpdateHearts();
        manaFill.style.width = Length.Percent(100f * playerMana.Current / playerMana.Max);
        UpdateSpellSlot();
        UpdateAbilitySlots();
        UpdateObjective();
        UpdateSecondary();
        UpdateHints();

        // Boss health bar: shown once the boss has noticed you, until it falls.
        bool showBoss = boss != null && boss.IsEngaged && !boss.Health.IsDead;
        bossBar.EnableInClassList("visible", showBoss);
        if (showBoss) bossFill.style.width = Length.Percent(100f * boss.Health.Current / boss.Health.Max);

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
    private void OnPlayerHurt(Health _) => Pop(hurtFlash, "visible", 140);

    // The spell went off: the slot dips and a four-point star flashes over it, briefly.
    private void OnCast()
    {
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
        if (alive < lastAlive) defeated += lastAlive - alive;
        lastAlive = alive;
        int total = defeated + alive;

        bool showCount = LevelBootstrap.Current.ShowEnemyCount;
        bool exitOpen = exit == null || exit.IsOpen;

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
            ShowToast("The stairs are open!");
            Celebrate();
        }
        exitWasOpen = exitOpen;
    }

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

        string hint = null;
        if (usable == null && !DialogueController.IsOpen && GameManager.Instance != null && !GameManager.Instance.IsGameOver)
        {
            if (!hasWalked)
                hint = GameInput.UsingGamepad ? "Stick: Walk" : "W A S D: Walk";
            else if (Time.time - spell.LastCastTime > 6f && spell.CanAfford && spell.FindTarget() != null)
                hint = $"{SpellKey}: Magic!";
        }
        contextHint.text = hint ?? "";
        contextHint.EnableInClassList("visible", hint != null);
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
        if (spell != null) spell.Cast -= OnCast;
    }

    private void OnLevelUp(int level) => ShowToast($"Level up! You're level {level}!");

    public void SetInventoryOpen(bool open) => inventoryPanel.EnableInClassList("open", open);
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

        // UI Toolkit events work like DOM events: register a callback on an element.
        for (int i = 0; i < bagSlots.Length; i++)
        {
            int index = i; // capture a copy for the lambdas below
            bagSlots[i] = root.Q($"bag-{i}");
            bagSlots[i].RegisterCallback<ClickEvent>(_ => OnBagSlotClicked(index));
            bagSlots[i].RegisterCallback<PointerEnterEvent>(_ => ShowDetails(BagItem(index), equipped: false));
            bagSlots[i].RegisterCallback<PointerLeaveEvent>(_ => itemDetails.text = DetailsHint);
        }
        foreach (var slot in WornSlots)
        {
            var element = root.Q($"equip-{slot.ToString().ToLowerInvariant()}");
            equipSlots[slot] = element;
            element.RegisterCallback<ClickEvent>(_ =>
            {
                if (inventory.Unequip(slot)) AudioManager.Play(clickSound);
            });
            element.RegisterCallback<PointerEnterEvent>(_ => ShowWornDetails(slot));
            element.RegisterCallback<PointerLeaveEvent>(_ => itemDetails.text = DetailsHint);
        }

        inventory.Changed += RefreshInventory;
        RefreshInventory();
    }

    private ItemDefinition BagItem(int index) => index < inventory.Bag.Count ? inventory.Bag[index] : null;

    private void OnBagSlotClicked(int index)
    {
        var item = BagItem(index);
        if (item != null && inventory.Equip(item))
        {
            ShowDetails(item, equipped: true);
            AudioManager.Play(clickSound);
        }
    }

    private void RefreshInventory()
    {
        for (int i = 0; i < bagSlots.Length; i++)
            SetSlotItem(bagSlots[i], BagItem(i));
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

    private void ShowDetails(ItemDefinition item, bool equipped)
    {
        if (item == null) { itemDetails.text = DetailsHint; return; }
        string action = equipped ? "Click to take it off."
                      : item.IsEquippable ? $"Click to wear it ({item.Slot.ToString().ToLowerInvariant()})." : "";
        string bonus = item.BonusText.Length > 0 ? $" ({item.BonusText})" : "";
        itemDetails.text = $"{item.DisplayName}: {item.Description}{bonus} {action}";
    }

    private void ShowWornDetails(EquipSlot slot)
    {
        var item = inventory.Equipped(slot);
        if (item != null) ShowDetails(item, equipped: true);
        else itemDetails.text = $"{slot}: nothing yet. Maybe there's one out there somewhere...";
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

    // ---------- Toast ----------

    private void ShowToast(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        toast.text = message;
        toastHideAt = Time.time + toastSeconds;
    }
}
