using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod;

namespace TowerFall.Patching;

[MonoModPatch("TowerFall.ArchivesStatsPage")]
public class ArchivesStatsPage : TowerFall.ArchivesStatsPage
{
    // For whatever reason, this need to be repatch for v1.3.3.1
    private float atY;
    private List<PieChart> charts;

    [MonoModLinkTo("TowerFall.ArchivesPage", "System.Void .ctor(System.String)")]
    [MonoModIgnore]
    public void base_ctor(string title) {}   

    [MonoModConstructor]
    [MonoModIfFlag("NoLauncher")]
    [MonoModReplace]
    [FixGameStatsArrowsShot]
    [FixGameStatsArrowsCollected]
    [FixGameStatsArrowsCaught]
    [FixGameStatsTimesLaunched]
    [FixGameStatsMatchesPlayed]
    [FixGameStatsRoundsPlayed]
    [FixGameStatsTotalVersusKills]
    [FixGameStatsJumps]
    [FixGameStatsDodges]
    public void ctor() 
    {
        base_ctor("STATS");

        AddStat("TIMES LAUNCHED:", ((long)SaveData.Instance.Stats.TimesLaunched).ToString());
        atY += 3f;
        AddStat("BATTLE TIME:", GetTimeString(SaveData.Instance.Stats.TimePlayed));
        AddStat("MATCHES PLAYED:", SaveData.Instance.Stats.MatchesPlayed.ToString());
        AddStat("ROUNDS PLAYED:", SaveData.Instance.Stats.RoundsPlayed.ToString());
        AddStat("VERSUS KILLS:", SaveData.Instance.Stats.TotalVersusKills.ToString());
        atY += 3f;
        AddStat("ARROWS SHOT:", SaveData.Instance.Stats.ArrowsShot.ToString());
        AddStat("ARROWS COLLECTED:", SaveData.Instance.Stats.ArrowsCollected.ToString());
        AddStat("ARROWS CAUGHT:", SaveData.Instance.Stats.ArrowsCaught.ToString());
        AddStat("JUMPS:", SaveData.Instance.Stats.Jumps.ToString());
        AddStat("DODGES:", SaveData.Instance.Stats.Dodges.ToString());
        charts = [];

        if (SaveData.Instance.Stats.Kills.Total > 0UL)
        {
            Tuple<Color, float>[] colorKills = new Tuple<Color, float>[ArcherData.Amount];
            for (int i = 0; i < ArcherData.Amount; i++)
            {
                colorKills[i] = new Tuple<Color, float>(ArcherData.Archers[i].ColorA, SaveData.Instance.Stats.Kills[i]);
            }
            PieChart pieChart = new PieChart(new Vector2(-50f, 132f), 26, colorKills);
            Add(pieChart);
            charts.Add(pieChart);
            OutlineText outlineText = new OutlineText(TFGame.Font, "KILLS BY ARCHER", new Vector2(-50f, 157f), Text.HorizontalAlign.Center, Text.VerticalAlign.Center);
            Add(outlineText);
        }

        bool hasWon = false;
        ulong[] wins = SaveData.Instance.Stats.Wins;
        for (int j = 0; j < wins.Length; j++)
        {
            if (wins[j] > 0UL)
            {
                hasWon = true;
                break;
            }
        }

        if (hasWon)
        {
            var colorWins = new Tuple<Color, float>[ArcherData.Amount];
            for (int k = 0; k < ArcherData.Amount; k++)
            {
                colorWins[k] = new Tuple<Color, float>(ArcherData.Archers[k].ColorA, SaveData.Instance.Stats.Wins[k]);
            }
            PieChart pieChart2 = new PieChart(new Vector2(50f, 132f), 26, colorWins);
            Add(pieChart2);
            charts.Add(pieChart2);

            OutlineText outlineText2 = new OutlineText(TFGame.Font, "WINS BY ARCHER", new Vector2(50f, 157f), Text.HorizontalAlign.Center, Text.VerticalAlign.Center);
            Add(outlineText2);
        }
    }

    [MonoModIgnore]
    private extern void AddStat(string title, string value);

    [MonoModIgnore]
    private static extern string GetTimeString(long ticks);
}

