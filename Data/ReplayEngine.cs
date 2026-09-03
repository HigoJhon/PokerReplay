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
                state.HoleCardsDealt = true;
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

            case "UncalledBetReturned":
                var returned = (int)(ev.Amount ?? 0);
                state.Stacks[ev.PlayerName!] += returned;
                state.Pot -= returned;
                if (state.StreetBets.ContainsKey(ev.PlayerName!))
                    state.StreetBets[ev.PlayerName!] -= returned;
                state.ActionLog.Add($"{ev.PlayerName} recebe de volta {returned} (aposta não paga)");
                break;

            case "Showdown":
                if (ev.PlayerName is not null && ev.Cards is not null)
                {
                    state.ShowdownCards[ev.PlayerName] = ev.Cards;
                    var desc = string.IsNullOrEmpty(ev.HandDescription) ? "" : $" ({ev.HandDescription})";
                    state.ActionLog.Add($"{ev.PlayerName} mostra a mão{desc}");
                }
                break;

            case "PotAwarded":
                var wonAmt = (int)(ev.Amount ?? 0);
                state.Stacks[ev.PlayerName!] += wonAmt;
                state.PotsAwarded.Add(new PotResult { PlayerName = ev.PlayerName!, Amount = wonAmt });
                state.ActionLog.Add($"🏆 {ev.PlayerName} ganha {wonAmt} ({FormatBB(wonAmt, bb)})");
                break;

            case "PlayerStatus":
                if (!string.IsNullOrEmpty(ev.Status))
                    state.ActionLog.Add($"{ev.PlayerName}: {ev.Status}");
                break;

            case "HandSummary": // formato antigo, mantido por compatibilidade
                if (ev.Winner is not null)
                {
                    if (ev.Pot.HasValue) state.Pot = (int)ev.Pot.Value;
                    state.PotsAwarded.Add(new PotResult { PlayerName = ev.Winner, Amount = state.Pot });
                    state.ActionLog.Add($"🏆 {ev.Winner} vence a mão ({FormatBB(state.Pot, bb)})");
                }
                break;
        }
    }
    
    public static Dictionary<string, string> CalculatePositions(NotableHand hand)
    {
        var seats = hand.InitialTableState.Seats
            .OrderBy(s => s.SeatNumber)
            .ToList();

        var buttonIndex = seats.FindIndex(s => s.SeatNumber == hand.InitialTableState.ButtonSeat);
        if (buttonIndex < 0) buttonIndex = 0;

        var total = seats.Count;
        var positions = new Dictionary<string, string>();

        // rótulos por número de jogadores, do BTN pro UTG (heads-up e 3-max são casos especiais)
        string[] labels = total switch
        {
            2 => new[] { "BTN/SB", "BB" },
            3 => new[] { "BTN", "SB", "BB" },
            4 => new[] { "BTN", "SB", "BB", "UTG" },
            5 => new[] { "BTN", "SB", "BB", "UTG", "CO" },
            6 => new[] { "BTN", "SB", "BB", "UTG", "HJ", "CO" },
            7 => new[] { "BTN", "SB", "BB", "UTG", "UTG+1", "HJ", "CO" },
            8 => new[] { "BTN", "SB", "BB", "UTG", "UTG+1", "UTG+2", "HJ", "CO" },
            9 => new[] { "BTN", "SB", "BB", "UTG", "UTG+1", "UTG+2", "UTG+3", "HJ", "CO" },
            _ => new[] { "BTN", "SB", "BB", "UTG", "UTG+1", "UTG+2", "UTG+3", "UTG+4", "HJ", "CO" }
        };

        for (int i = 0; i < total; i++)
        {
            var seatIndex = (buttonIndex + i) % total;
            var label = i < labels.Length ? labels[i] : $"UTG+{i - 2}";
            positions[seats[seatIndex].PlayerName] = label;
        }

        return positions;
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

                var isAllIn = ev.AllIn == true;
                state.LastAction[player] = isAllIn ? "ALL-IN" : Traduz(ev.Action!).ToUpper();
                state.ActionLog.Add($"{player} {Traduz(ev.Action!)} {amt}{(isAllIn ? " (all-in)" : "")} ({FormatBB(amt, bb)})");
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