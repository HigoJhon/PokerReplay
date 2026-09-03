namespace Replay.Models;

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
    public List<string> ActionLog { get; set; } = new();
    public TimelineEvent? CurrentEvent { get; set; }
    public string? HandWinner { get; set; }
}