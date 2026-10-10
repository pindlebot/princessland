using System;

// A tiny language for "has this happened yet?", shared by NPC conversations and quests so that
// one rule covers intros, "later" chats, quest steps and easter eggs. A condition is one or more
// terms separated by commas, and ALL of them must hold ("" always holds):
//
//   met:Pearl            a flag (GameSession.Flags) is set
//   !found:frog          "!" in front of any term means "not": the flag is NOT set
//   talks:Amethyra>=9    a counter (GameSession.Counters) compared with a number: >=  <=  >  <  ==
//   has:fairy_lantern    the item is in the bag, in the treasures tab or being worn
//
// Because progress is derived from flags and counters, nothing else needs saving.
public static class Condition
{
    public static bool Met(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return true;
        foreach (var raw in expression.Split(','))
        {
            string term = raw.Trim();
            if (term.Length == 0) continue;
            bool negate = term[0] == '!';
            if (negate) term = term.Substring(1).Trim();
            if (Holds(term) == negate) return false;
        }
        return true;
    }

    private static bool Holds(string term)
    {
        if (term.StartsWith("has:", StringComparison.Ordinal))
            return GameSession.Inventory.Has(term.Substring(4).Trim());

        foreach (var op in new[] { ">=", "<=", "==", ">", "<" })
        {
            int at = term.IndexOf(op, StringComparison.Ordinal);
            if (at <= 0) continue;
            if (!int.TryParse(term.Substring(at + op.Length).Trim(), out int number)) break; // not a comparison after all
            int value = GameSession.GetCounter(term.Substring(0, at).Trim());
            switch (op)
            {
                case ">=": return value >= number;
                case "<=": return value <= number;
                case "==": return value == number;
                case ">": return value > number;
                default: return value < number;
            }
        }
        return GameSession.Flags.Contains(term);
    }
}
