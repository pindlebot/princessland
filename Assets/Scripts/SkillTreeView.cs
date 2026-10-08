using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// The skill tree panel (press K). Unlike the rest of the HUD, its nodes aren't written in
// Hud.uxml: they're created here from SkillCatalog, so adding a skill to the catalog adds
// it to the screen. Building UI from code is just `new VisualElement()` + AddToClassList.
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

    public bool IsOpen => panel.ClassListContains("open");
    public VisualElement NodeFor(string skillId) => nodes[System.Array.Find(SkillCatalog.All, s => s.Id == skillId)];

    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        panel = root.Q("skills");
        pointsLabel = root.Q<Label>("skill-points");
        details = root.Q<Label>("skill-details");
        details.text = DetailsHint;
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
        if (GameInput.SkillTreePressed && !DialogueController.BlocksInput)
            SetOpen(!IsOpen);
    }

    public void SetOpen(bool open) => panel.EnableInClassList("open", open);

    private void BuildTree(VisualElement container)
    {
        foreach (var branch in SkillCatalog.Branches)
        {
            var column = new VisualElement();
            column.AddToClassList("skill-branch");
            column.pickingMode = PickingMode.Ignore;
            var title = new Label(branch);
            title.AddToClassList("skill-branch-title");
            column.Add(title);

            foreach (var skill in SkillCatalog.All)
            {
                if (skill.Branch != branch) continue;
                if (skill.Tier > 1)
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
    }

    private VisualElement CreateNode(SkillDefinition skill)
    {
        var node = new VisualElement();
        node.AddToClassList("skill-node");
        if (!skill.Implemented) node.AddToClassList("coming-soon");

        var name = new Label(skill.Name);
        name.AddToClassList("skill-name");
        var tag = new Label(skill.Implemented ? $"Tier {skill.Tier}" : $"Tier {skill.Tier} · coming soon");
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
        if (!progress.Learn(skill)) return false;
        AudioManager.Play(learnSound);
        details.text = Describe(skill);
        return true;
    }

    private string Describe(SkillDefinition skill)
    {
        string state =
            progress.Has(skill.Id) ? "Learned."
            : !progress.MeetsRequirement(skill) ? $"Requires {System.Array.Find(SkillCatalog.All, s => s.Id == skill.Requires).Name}."
            : progress.SkillPoints == 0 ? "No skill points: gain a level to earn one."
            : "Click to learn (1 point).";
        string soon = skill.Implemented ? "" : " (Coming soon: this ability isn't built yet.)";
        return $"{skill.Name}: {skill.Description}{soon} {state}";
    }

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
