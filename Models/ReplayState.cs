namespace Replay.Models;

public class PotResult
{
    public string PlayerName { get; set; } = "";
    public int Amount { get; set; }
}

public class ReplayState
{
    public Dictionary<string, int> Stacks { get; set; } = new();
    public Dictionary<string, int> StreetBets { get; set; } = new();
    public Dictionary<string, string> LastAction { get; set; } = new();
    public HashSet<string> FoldedPlayers { get; set; } = new();
    public List<Card> Board { get; set; } = new();
    public int Pot { get; set; }
    public string CurrentStreet { get; set; } = "Preflop";
    public List<Card>? HeroCards { get; set; }
    public Dictionary<string, List<Card>> ShowdownCards { get; set; } = new();
    public List<string> ActionLog { get; set; } = new();
    public TimelineEvent? CurrentEvent { get; set; }
    public List<PotResult> PotsAwarded { get; set; } = new();
    public bool HoleCardsDealt { get; set; }
}