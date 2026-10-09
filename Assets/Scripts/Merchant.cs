using UnityEngine;

// A shopkeeper: an Npc who sells one thing (Barnaby Badger and his bubble bath, in the village).
// The first time you press E they introduce themselves, like any Npc (their first conversation
// sets "met:<name>"). After that, E buys their ware: it costs gold, goes into your bag, and
// they say thank you in the dialogue box. Inheritance at work: Merchant only overrides the
// prompt and what talking does, and reuses everything else Npc has (portrait, voice, mouth).
public class Merchant : Npc
{
    [Header("Shop")]
    [SerializeField] private ItemDefinition ware;
    [SerializeField] private int price = 10;
    [Tooltip("What they say after a sale. Several answers separated by '|' take turns.")]
    [TextArea] [SerializeField] private string thanks = "Thank you kindly!";
    [Tooltip("What they say when you can't afford it. \"{price}\" becomes the price.")]
    [TextArea] [SerializeField] private string tooPoor = "That's {price} coins, I'm afraid.";
    [TextArea] [SerializeField] private string bagFull = "Your bag is full!";
    [SerializeField] private AudioClip saleSound;

    public ItemDefinition Ware => ware;
    public int Price => price;
    public int Sold => GameSession.GetCounter("bought:" + ware.Id); // saved with the other counters

    public override string Prompt => HasMet ? $"Buy {ware.DisplayName} ({price} coins)" : base.Prompt;

    public override string Interact(GameObject player)
    {
        if (!HasMet) return base.Interact(player); // the introduction first
        Say(Buy(player.GetComponent<Inventory>()));
        return null;
    }

    // Sells one if you have room and enough coins. Returns what the shopkeeper says.
    public string Buy(Inventory inventory)
    {
        if (inventory.Bag.Count >= inventory.Capacity) return bagFull;
        if (!GameSession.Progress.SpendGold(price)) return tooPoor.Replace("{price}", price.ToString());

        inventory.Add(ware);
        AudioManager.Play(saleSound);
        int sold = GameSession.AddToCounter("bought:" + ware.Id);
        var answers = thanks.Split('|');
        return answers[(sold - 1) % answers.Length].Trim();
    }
}
