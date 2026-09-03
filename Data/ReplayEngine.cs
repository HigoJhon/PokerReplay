using Replay.Models;

namespace Replay.Data;

public static class ReplayEngine
{
    public static ReplayState BuildState(NotableHand hand, int uptoEventIndex)
    {
        var state = new ReplayState();
        var bb = hand.Level.BigBlind;
        var heroName = hand.InitialTableState.Seats.FirstOrDefault(s => s.IsHero)?.PlayerName;

        foreach (var seat in hand.InitialTableState.Seats)
            state.Stacks[seat.PlayerName] = seat.InitialStack;

        var events = hand.TimelineEvents.OrderBy(e => e.EventId).ToList();

        for (int i = 0; i <= uptoEventIndex && i < events.Count; i++)
        {
            state.CurrentEvent = events[i];
            Apply(state, events[i], bb, heroName);
        }

        return state;
    }

    private static void Apply(ReplayState state, TimelineEvent ev, int bb, string? heroName)
    {
        switch (ev.EventType)
        {
            case "PostAnte":
                var anteAmt = (int)(ev.Amount ?? 0);
                state.Stacks[ev.PlayerName!] -= anteAmt;
                state.Pot += anteAmt;
                state.ActionLog.Add($"{ev.PlayerName} posta ante {anteAmt}");
                break;

            case "PostSmallBlind":
            case "PostBigBlind":
                var blindAmt = (int)(ev.Amount ?? 0);
                state.Stacks[ev.PlayerName!] -= blindAmt;
                state.Pot += blindAmt;
                AddToStreetBet(state, ev.PlayerName!, blindAmt);
                state.ActionLog.Add($"{ev.PlayerName} posta {blindAmt} ({FormatBB(blindAmt, bb)})");
                break;

            case "DealHoleCards":
                if (ev.PlayerName == heroName)
                    state.HeroCards = ev.Cards;
                break;

            case "DealBoard":
                state.Board.AddRange(ev.Cards ?? new());
                state.CurrentStreet = ev.Street ?? state.CurrentStreet;
                state.StreetBets.Clear();
                state.LastAction.Clear();
                state.ActionLog.Add($"— {ev.Street} —");
                break;

            case "PlayerAction":
                HandleAction(state, ev, bb);
                break;

            case "HandSummary":
                state.HandWinner = ev.Winner;
                if (ev.Pot.HasValue) state.Pot = (int)ev.Pot.Value;
                state.ActionLog.Add($"🏆 {ev.Winner} vence a mão ({FormatBB(state.Pot, bb)})");
                break;
        }
    }

    private static void HandleAction(ReplayState state, TimelineEvent ev, int bb)
    {
        var player = ev.PlayerName!;

        switch (ev.Action)
        {
            case "Fold":
                state.FoldedPlayers.Add(player);
                state.LastAction[player] = "FOLD";
                state.ActionLog.Add($"{player} desiste");
                break;

            case "Check":
                state.LastAction[player] = "CHECK";
                state.ActionLog.Add($"{player} passa");
                break;

            default: // Raise, Call, Bet, AllIn
                var amt = (int)(ev.Amount ?? 0);
                state.Stacks[player] -= amt;
                state.Pot += amt;
                AddToStreetBet(state, player, amt);
                state.LastAction[player] = $"{Traduz(ev.Action!).ToUpper()} {amt} ({FormatBB(amt, bb)})";
                state.ActionLog.Add($"{player} {Traduz(ev.Action!)} {amt} ({FormatBB(amt, bb)})");
                break;
        }
    }

    private static void AddToStreetBet(ReplayState state, string player, int amount)
        => state.StreetBets[player] = state.StreetBets.GetValueOrDefault(player) + amount;

    private static string Traduz(string action) => action switch
    {
        "Raise" => "raise",
        "Call" => "call",
        "Bet" => "bet",
        "AllIn" => "all-in",
        _ => action
    };

    public static string FormatBB(int amount, int bigBlind)
    {
        if (bigBlind <= 0) return "";
        var bb = amount / (decimal)bigBlind;
        return bb % 1 == 0 ? $"{bb:0}bb" : $"{bb:0.#}bb";
    }
}