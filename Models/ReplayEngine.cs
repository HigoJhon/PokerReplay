using Replay.Models;

namespace Replay.Data;

public static class ReplayEngine
{
    public static ReplayState BuildState(NotableHand hand, int uptoEventIndex)
    {
        var state = new ReplayState();
        var bb = hand.Level.BigBlind;

        foreach (var seat in hand.InitialTableState.Seats)
            state.Stacks[seat.PlayerName] = seat.InitialStack;

        var events = hand.TimelineEvents.OrderBy(e => e.EventId).ToList();

        for (int i = 0; i <= uptoEventIndex && i < events.Count; i++)
        {
            state.CurrentEvent = events[i];
            Apply(state, events[i], bb);
        }

        return state;
    }

    private static void Apply(ReplayState state, TimelineEvent ev, int bb)
    {
        switch (ev.EventType)
        {
            case "PostAnte":
            case "PostSmallBlind":
            case "PostBigBlind":
                var postAmt = (int)(ev.Amount ?? 0);
                state.Stacks[ev.PlayerName!] -= postAmt;
                state.Pot += postAmt;
                state.ActionLog.Add($"{ev.PlayerName} posta {postAmt} ({FormatBB(postAmt, bb)})");
                break;

            case "DealHoleCards":
                if (ev.IsHero == true) state.HeroCards = ev.Cards;
                break;

            case "DealBoard":
                state.Board.AddRange(ev.Cards ?? new());
                state.CurrentStreet = ev.Street ?? state.CurrentStreet;
                state.ActionLog.Add($"— {ev.Street} —");
                break;

            case "PlayerAction":
                HandleAction(state, ev, bb);
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
                state.ActionLog.Add($"{player} desiste");
                break;
            case "Check":
                state.ActionLog.Add($"{player} passa");
                break;
            default: // Raise, Call, Bet, AllIn
                var amt = (int)(ev.Amount ?? 0);
                state.Stacks[player] -= amt;
                state.Pot += amt;
                state.ActionLog.Add($"{player} {Traduz(ev.Action!)} {amt} ({FormatBB(amt, bb)})");
                break;
        }
    }

    private static string Traduz(string action) => action switch
    {
        "Raise" => "aumenta",
        "Call" => "paga",
        "Bet" => "aposta",
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