using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// The skill tree panel (press K): the hero's path of four skills, top to bottom. Unlike the
// rest of the HUD, its nodes aren't written in Hud.uxml: they're created here from
// SkillCatalog, so adding a skill to the catalog adds it to the screen. Building UI from
// code is just `new VisualElement()` + AddToClassList.
[RequireComponent(typeof(UIDocument))]
public class SkillTreeView : MonoBehaviour
{
    [SerializeField] private AudioClip learnSound;

    private const string DetailsHint = "Hover a skill to read about it. Click to learn it (1 point).";

    private VisualElement panel;
    private Label pointsLabel, details;
    private readonly Dictionary<SkillDefinition, VisualElement> nodes = new Dictionary<SkillDefinition, VisualElement>();
    private readonly Dictionary<SkillDefinition, VisualElement> links = new Dictionary<SkillDefinition, VisualElement>();
    private Progression progress;
    private SkillDefinition[] path;

    public bool IsOpen => panel.ClassListContains("open");
    public IReadOnlyList<SkillDefinition> Path => path;
    public VisualElement NodeFor(string skillId) => nodes[SkillCatalog.Find(skillId)];

    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        panel = root.Q("skills");
        pointsLabel = root.Q<Label>("skill-points");
        details = root.Q<Label>("skill-details");
        details.text = DetailsHint;
        // LevelBootstrap knows who's playing even when no hero was picked (Play in a level scene).
        string hero = LevelBootstrap.Current != null ? LevelBootstrap.Current.Character.name : SkillCatalog.CurrentHero;
        path = SkillCatalog.PathFor(hero);
        BuildTree(root.Q("skill-branches"));

        progress = GameSession.Progress;
        progress.Changed += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (progress != null) progress.Changed -= Refresh; // Progression outlives this scene
    }

    private void Update()
    {
        if (GameInput.SkillTreePressed && !GameInput.GameplayBlocked)
            SetOpen(!IsOpen);
    }

    public void SetOpen(bool open) => panel.EnableInClassList("open", open);

    // One column: each step, with a short link down to the next.
    private void BuildTree(VisualElement container)
    {
        var column = new VisualElement();
        column.AddToClassList("skill-branch");
        column.pickingMode = PickingMode.Ignore;
        foreach (var skill in path)
        {
            if (skill.Requires != null)
            {
                var link = new VisualElement();
                link.AddToClassList("skill-link");
                column.Add(link);
                links[skill] = link;
            }
            column.Add(CreateNode(skill));
        }
        container.Add(column);
    }

    private VisualElement CreateNode(SkillDefinition skill)
    {
        var node = new VisualElement();
        node.AddToClassList("skill-node");
        if (skill.IsAbility) node.AddToClassList("ability");

        var name = new Label(skill.Name);
        name.AddToClassList("skill-name");
        var tag = new Label(skill.IsAbility ? $"Step {skill.Step} · new ability" : $"Step {skill.Step} · enhancement");
        tag.AddToClassList("skill-tag");
        node.Add(name);
        node.Add(tag);

        node.RegisterCallback<ClickEvent>(_ => TryLearn(skill));
        node.RegisterCallback<PointerEnterEvent>(_ => details.text = Describe(skill));
        node.RegisterCallback<PointerLeaveEvent>(_ => details.text = DetailsHint);
        nodes[skill] = node;
        return node;
    }

    public bool TryLearn(SkillDefinition skill)
    {
        if (System.Array.IndexOf(path, skill) < 0) return false; // another hero's skill
        if (!progress.Learn(skill)) return false;
        AudioManager.Play(learnSound);
        SaveSystem.AutosaveSoon(); // a bought skill isn't lost if the game closes
        details.text = Describe(skill);
        return true;
    }

    private string Describe(SkillDefinition skill)
    {
        string state =
            progress.Has(skill.Id) ? "Learned."
            : !progress.MeetsRequirement(skill) ? $"Requires {SkillCatalog.Find(skill.Requires).Name}."
            : progress.SkillPoints == 0 ? "No skill points: gain a level to earn one."
            : "Click to learn (1 point).";
        string use = skill.IsAbility && progress.Has(skill.Id) ? $" Press {KeyFor(skill)} to use it." : "";
        return $"{skill.Name}: {skill.Description} {state}{use}";
    }

    // Abilities take hotbar slots 2 and 3 in path order (step 3 -> slot 2, step 4 -> slot 3).
    private static string KeyFor(SkillDefinition skill) => GameInput.AbilityKey(skill.Step - 1);

    private void Refresh()
    {
        pointsLabel.text = $"Points: {progress.SkillPoints}";
        foreach (var pair in nodes)
        {
            var skill = pair.Key;
            bool learned = progress.Has(skill.Id);
            pair.Value.EnableInClassList("learned", learned);
            pair.Value.EnableInClassList("available", !learned && progress.CanLearn(skill));
            if (links.TryGetValue(skill, out var link))
                link.EnableInClassList("learned", progress.Has(skill.Requires));
        }
    }
}
