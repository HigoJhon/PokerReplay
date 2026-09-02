namespace Replay.Models;

public class HandHistoryFile
{
    public TournamentContext TournamentContext { get; set; } = new();
    public List<NotableHand> NotableHands { get; set; } = new();
}

public class TournamentContext
{
    public string TournamentId { get; set; } = "";
    public string Game { get; set; } = "";
    public decimal BuyInPrize { get; set; }
    public decimal BuyInRake { get; set; }
    public int TotalHandsProcessedInFile { get; set; }
    public int NotableHandsCount { get; set; }
}

public class NotableHand
{
    public int HandIndexInFile { get; set; }
    public string HandId { get; set; } = "";
    public string LearningTheme { get; set; } = "";
    public string CoachSummary { get; set; } = "";
    public Level Level { get; set; } = new();
    public InitialTableState InitialTableState { get; set; } = new();
    public List<TimelineEvent> TimelineEvents { get; set; } = new();
}

public class Level
{
    public string Name { get; set; } = "";
    public int SmallBlind { get; set; }
    public int BigBlind { get; set; }
    public int Ante { get; set; }
}

public class InitialTableState
{
    public int MaxSeats { get; set; }
    public int ButtonSeat { get; set; }
    public List<SeatInfo> Seats { get; set; } = new();
}

public class SeatInfo
{
    public int SeatNumber { get; set; }
    public string PlayerName { get; set; } = "";
    public int InitialStack { get; set; }
    public bool IsHero { get; set; }
}

public class TimelineEvent
{
    public int EventId { get; set; }
    public string EventType { get; set; } = "";
    public string? Street { get; set; }
    public string? PlayerName { get; set; }
    public bool? IsHero { get; set; }
    public string? Action { get; set; }
    public decimal? Amount { get; set; }
    public List<Card>? Cards { get; set; }
    public HeroAnalysis? HeroAnalysis { get; set; }
}

public class Card
{
    public string Rank { get; set; } = "";
    public string Suit { get; set; } = "";
}

public class HeroAnalysis
{
    public bool IsKeyDecision { get; set; }
    public string Evaluation { get; set; } = "";
    public string Commentary { get; set; } = "";
    public string SuggestedAction { get; set; } = "";
    public decimal SuggestedAmount { get; set; }
}
