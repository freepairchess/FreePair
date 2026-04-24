using System;
using System.IO;
using System.Linq;
using FreePair.Core.Formatting;
using FreePair.Core.Tournaments;
using FreePair.Core.Tournaments.Standings;
using FreePair.Core.Tournaments.Tiebreaks;
using FreePair.Core.Tournaments.WallCharts;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FreePair.Core.Reports;

/// <summary>
/// Builds TD-facing PDF reports of section state using
/// <see href="https://www.questpdf.com/">QuestPDF</see>. One static
/// method per tab (Players, Pairings, Standings, Wall chart, Prizes);
/// each writes a complete, self-contained PDF document into the
/// supplied <see cref="Stream"/>.
/// </summary>
/// <remarks>
/// QuestPDF's Community licence covers OSS projects and small
/// organisations; <see cref="RegisterLicense"/> sets it once per
/// process the first time any generator runs.
/// </remarks>
public static class PdfReportBuilder
{
    private static bool _licenseRegistered;

    /// <summary>
    /// Registers the QuestPDF Community licence once per process.
    /// Idempotent. Called automatically by the other entry points,
    /// but exposed so host apps can pre-warm if they want to.
    /// </summary>
    public static void RegisterLicense()
    {
        if (_licenseRegistered) return;
        QuestPDF.Settings.License = LicenseType.Community;
        _licenseRegistered = true;
    }

    // =========================================================
    //  Public entry points
    // =========================================================

    public static void WritePlayersReport(
        Stream output, Tournament tournament, Section section)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(tournament);
        ArgumentNullException.ThrowIfNull(section);
        RegisterLicense();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                ApplyPageDefaults(page);
                ApplyHeader(page, tournament, section, subtitle: "Player roster");
                ApplyFooter(page);
                page.Content().Element(c => RenderPlayersTable(c, section));
            });
        }).GeneratePdf(output);
    }

    public static void WritePairingsReport(
        Stream output, Tournament tournament, Section section, int round)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(tournament);
        ArgumentNullException.ThrowIfNull(section);
        RegisterLicense();

        var r = section.Rounds.FirstOrDefault(x => x.Number == round)
            ?? throw new InvalidOperationException(
                $"Round {round} does not exist in section '{section.Name}'.");

        Document.Create(container =>
        {
            container.Page(page =>
            {
                ApplyPageDefaults(page);
                ApplyHeader(page, tournament, section, subtitle: $"Round {round} pairings");
                ApplyFooter(page);
                page.Content().Element(c => RenderPairingsTable(c, section, r));
            });
        }).GeneratePdf(output);
    }

    public static void WriteStandingsReport(
        Stream output,
        Tournament tournament,
        Section section,
        IScoreFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(tournament);
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(formatter);
        RegisterLicense();

        var rows = StandingsBuilder.Build(section);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                ApplyPageDefaults(page);
                ApplyHeader(page, tournament, section,
                    subtitle: $"Standings after round {section.RoundsPlayed}");
                ApplyFooter(page);
                page.Content().Element(c => RenderStandingsTable(c, rows, formatter));
            });
        }).GeneratePdf(output);
    }

    public static void WriteWallChartReport(
        Stream output,
        Tournament tournament,
        Section section,
        IScoreFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(tournament);
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(formatter);
        RegisterLicense();

        var rows = WallChartBuilder.Build(section);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                ApplyPageDefaults(page, landscape: true);
                ApplyHeader(page, tournament, section,
                    subtitle: $"Wall chart (rounds 1–{section.RoundsPlayed})");
                ApplyFooter(page);
                page.Content().Element(c =>
                    RenderWallChartTable(c, rows, section.RoundsPlayed, formatter));
            });
        }).GeneratePdf(output);
    }

    public static void WritePrizesReport(
        Stream output, Tournament tournament, Section section)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(tournament);
        ArgumentNullException.ThrowIfNull(section);
        RegisterLicense();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                ApplyPageDefaults(page);
                ApplyHeader(page, tournament, section, subtitle: "Prize fund");
                ApplyFooter(page);
                page.Content().Element(c => RenderPrizesSection(c, section));
            });
        }).GeneratePdf(output);
    }

    // =========================================================
    //  Page chrome
    // =========================================================

    private static void ApplyPageDefaults(QuestPDF.Fluent.PageDescriptor page, bool landscape = true)
    {
        // Default is landscape — TD-facing reports (players list, pairings
        // sheet, standings, wall chart) all benefit from the extra
        // horizontal room. Callers can override by passing landscape:false.
        page.Size(landscape ? PageSizes.Letter.Landscape() : PageSizes.Letter);
        page.Margin(36);
        page.DefaultTextStyle(t => t.FontSize(10).FontFamily(Fonts.Calibri));
    }

    private static void ApplyHeader(
        QuestPDF.Fluent.PageDescriptor page,
        Tournament t,
        Section s,
        string subtitle)
    {
        page.Header().Column(col =>
        {
            col.Spacing(2);
            col.Item().Text(t.Title ?? "Untitled tournament")
                      .FontSize(16).SemiBold();
            col.Item().Text(txt =>
            {
                txt.Span($"Section: ").Bold();
                txt.Span(s.Name);
                if (!string.IsNullOrWhiteSpace(s.TimeControl))
                {
                    txt.Span("   •   ").Light();
                    txt.Span($"Time control: ").Bold();
                    txt.Span(s.TimeControl!);
                }
            });
            if (t.StartDate is not null || t.EndDate is not null || t.LocationSummary.Length > 0)
            {
                col.Item().Text(txt =>
                {
                    if (t.LocationSummary.Length > 0)
                    {
                        txt.Span("Location: ").Bold();
                        txt.Span(t.LocationSummary);
                        txt.Span("   •   ").Light();
                    }
                    if (t.StartDate is not null)
                    {
                        txt.Span("Dates: ").Bold();
                        txt.Span(t.StartDate.Value.ToString("yyyy-MM-dd"));
                        if (t.EndDate is not null && t.EndDate != t.StartDate)
                        {
                            txt.Span(" to ");
                            txt.Span(t.EndDate.Value.ToString("yyyy-MM-dd"));
                        }
                    }
                });
            }
            col.Item().PaddingTop(6).Text(subtitle).FontSize(13).SemiBold().FontColor(Colors.Blue.Darken2);
            col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
        });
    }

    private static void ApplyFooter(QuestPDF.Fluent.PageDescriptor page)
    {
        page.Footer().AlignCenter().Text(txt =>
        {
            txt.Span("Generated by FreePair on ").FontSize(8).FontColor(Colors.Grey.Medium);
            txt.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm")).FontSize(8).FontColor(Colors.Grey.Medium);
            txt.Span("   •   Page ").FontSize(8).FontColor(Colors.Grey.Medium);
            txt.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
            txt.Span(" of ").FontSize(8).FontColor(Colors.Grey.Medium);
            txt.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
        });
    }

    // =========================================================
    //  Table renderers
    // =========================================================

    private static void RenderPlayersTable(IContainer container, Section s)
    {
        container.Table(tbl =>
        {
            tbl.ColumnsDefinition(c =>
            {
                c.ConstantColumn(35);   // Pair
                c.ConstantColumn(70);   // USCF
                c.RelativeColumn(2.5f); // Name
                c.ConstantColumn(45);   // Rating
                c.RelativeColumn(1f);   // Club
                c.ConstantColumn(35);   // State
                c.RelativeColumn(1f);   // Team
                c.RelativeColumn(2f);   // Email
                c.RelativeColumn(1.2f); // Phone
                c.ConstantColumn(40);   // Score
                c.ConstantColumn(55);   // ½-pt byes
                c.ConstantColumn(55);   // 0-pt byes
            });

            tbl.Header(h =>
            {
                HeaderCell(h, "#");
                HeaderCell(h, "USCF");
                HeaderCell(h, "Name");
                HeaderCell(h, "Rtg");
                HeaderCell(h, "Club");
                HeaderCell(h, "St");
                HeaderCell(h, "Team");
                HeaderCell(h, "Email");
                HeaderCell(h, "Phone");
                HeaderCell(h, "Score");
                HeaderCell(h, "½-pt byes");
                HeaderCell(h, "0-pt byes");
            });

            foreach (var p in s.Players.OrderBy(x => x.PairNumber))
            {
                BodyCell(tbl, p.PairNumber.ToString());
                BodyCell(tbl, p.UscfId);
                BodyCell(tbl, p.Name);
                BodyCell(tbl, p.Rating.ToString());
                BodyCell(tbl, p.Club);
                BodyCell(tbl, p.State);
                BodyCell(tbl, p.Team);
                BodyCell(tbl, p.Email);
                BodyCell(tbl, p.Phone);
                BodyCell(tbl, p.Score.ToString("0.##"));
                BodyCell(tbl, p.RequestedByeRounds.Count == 0
                    ? null
                    : string.Join(", ", p.RequestedByeRounds.OrderBy(r => r)));
                BodyCell(tbl, p.ZeroPointByeRoundsOrEmpty.Count == 0
                    ? null
                    : string.Join(", ", p.ZeroPointByeRoundsOrEmpty.OrderBy(r => r)));
            }
        });
    }

    private static void RenderPairingsTable(IContainer container, Section s, Round r)
    {
        var byPair = s.Players.ToDictionary(p => p.PairNumber);

        container.Column(col =>
        {
            col.Spacing(8);

            col.Item().Table(tbl =>
            {
                tbl.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(40);     // Board
                    c.RelativeColumn(2.2f);   // White name
                    c.ConstantColumn(45);     // White rtg
                    c.ConstantColumn(55);     // Result
                    c.RelativeColumn(2.2f);   // Black name
                    c.ConstantColumn(45);     // Black rtg
                });

                tbl.Header(h =>
                {
                    HeaderCell(h, "Bd");
                    HeaderCell(h, "White");
                    HeaderCell(h, "Rtg");
                    HeaderCell(h, "Result", alignCenter: true);
                    HeaderCell(h, "Black");
                    HeaderCell(h, "Rtg");
                });

                foreach (var p in r.Pairings.OrderBy(x => x.Board))
                {
                    byPair.TryGetValue(p.WhitePair, out var w);
                    byPair.TryGetValue(p.BlackPair, out var b);

                    BodyCell(tbl, p.Board.ToString());
                    BodyCell(tbl, $"#{p.WhitePair} {w?.Name ?? string.Empty}");
                    BodyCell(tbl, (w?.Rating ?? 0).ToString());
                    BodyCell(tbl, FormatResult(p.Result), alignCenter: true);
                    BodyCell(tbl, $"#{p.BlackPair} {b?.Name ?? string.Empty}");
                    BodyCell(tbl, (b?.Rating ?? 0).ToString());
                }
            });

            if (r.Byes.Count > 0)
            {
                col.Item().PaddingTop(6).Text("Byes").SemiBold().FontSize(11);
                col.Item().Table(tbl =>
                {
                    tbl.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(40);
                        c.RelativeColumn(3f);
                        c.ConstantColumn(80);
                    });

                    tbl.Header(h =>
                    {
                        HeaderCell(h, "#");
                        HeaderCell(h, "Player");
                        HeaderCell(h, "Kind");
                    });

                    foreach (var b in r.Byes)
                    {
                        byPair.TryGetValue(b.PlayerPair, out var pl);
                        BodyCell(tbl, b.PlayerPair.ToString());
                        BodyCell(tbl, pl?.Name ?? string.Empty);
                        BodyCell(tbl, b.Kind switch
                        {
                            ByeKind.Full     => "Full (1)",
                            ByeKind.Half     => "Half (½)",
                            ByeKind.Unpaired => "Zero (0)",
                            _                => b.Kind.ToString(),
                        });
                    }
                });
            }
        });
    }

    private static void RenderStandingsTable(
        IContainer container,
        System.Collections.Generic.IReadOnlyList<StandingsRow> rows,
        IScoreFormatter formatter)
    {
        container.Table(tbl =>
        {
            tbl.ColumnsDefinition(c =>
            {
                c.ConstantColumn(40);   // Rank
                c.ConstantColumn(35);   // Pair
                c.RelativeColumn(3f);   // Name
                c.ConstantColumn(45);   // Rating
                c.ConstantColumn(45);   // Score
                c.ConstantColumn(55);   // ModMed
                c.ConstantColumn(55);   // Solkoff
                c.ConstantColumn(55);   // Cum
                c.ConstantColumn(55);   // OppCum
            });

            tbl.Header(h =>
            {
                HeaderCell(h, "Rank");
                HeaderCell(h, "#");
                HeaderCell(h, "Name");
                HeaderCell(h, "Rtg");
                HeaderCell(h, "Score", alignRight: true);
                HeaderCell(h, "Mod-Med", alignRight: true);
                HeaderCell(h, "Solkoff", alignRight: true);
                HeaderCell(h, "Cum", alignRight: true);
                HeaderCell(h, "Opp-Cum", alignRight: true);
            });

            foreach (var row in rows)
            {
                BodyCell(tbl, row.Rank.ToString());
                BodyCell(tbl, row.PairNumber.ToString());
                BodyCell(tbl, row.Name);
                BodyCell(tbl, row.Rating.ToString());
                BodyCell(tbl, formatter.Score(row.Score), alignRight: true);
                BodyCell(tbl, formatter.Score(row.Tiebreaks.ModifiedMedian), alignRight: true);
                BodyCell(tbl, formatter.Score(row.Tiebreaks.Solkoff), alignRight: true);
                BodyCell(tbl, formatter.Score(row.Tiebreaks.Cumulative), alignRight: true);
                BodyCell(tbl, formatter.Score(row.Tiebreaks.OpponentCumulative), alignRight: true);
            }
        });
    }

    private static void RenderWallChartTable(
        IContainer container,
        System.Collections.Generic.IReadOnlyList<WallChartRow> rows,
        int roundsPlayed,
        IScoreFormatter formatter)
    {
        container.Table(tbl =>
        {
            tbl.ColumnsDefinition(c =>
            {
                c.ConstantColumn(35);  // #
                c.ConstantColumn(30);  // Pair
                c.RelativeColumn(2.4f); // Name
                c.ConstantColumn(40);  // Rating
                for (var i = 0; i < roundsPlayed; i++) c.ConstantColumn(55);
                c.ConstantColumn(40);  // Score
            });

            tbl.Header(h =>
            {
                HeaderCell(h, "#");
                HeaderCell(h, "Pair");
                HeaderCell(h, "Name");
                HeaderCell(h, "Rtg");
                for (var i = 1; i <= roundsPlayed; i++) HeaderCell(h, $"R{i}", alignCenter: true);
                HeaderCell(h, "Score", alignRight: true);
            });

            var rank = 1;
            foreach (var row in rows)
            {
                BodyCell(tbl, rank.ToString());
                BodyCell(tbl, row.PairNumber.ToString());
                BodyCell(tbl, row.Name);
                BodyCell(tbl, row.Rating.ToString());
                for (var i = 0; i < roundsPlayed; i++)
                {
                    var cell = i < row.Cells.Count ? row.Cells[i] : null;
                    BodyCell(tbl, cell?.Code ?? string.Empty, alignCenter: true);
                }
                BodyCell(tbl, formatter.Score(row.Score), alignRight: true);
                rank++;
            }
        });
    }

    private static void RenderPrizesSection(IContainer container, Section s)
    {
        container.Column(col =>
        {
            col.Spacing(10);

            col.Item().Text("Place prizes").FontSize(12).SemiBold();
            if (s.Prizes.Place.Count == 0)
            {
                col.Item().Text("(none configured)").Italic().FontColor(Colors.Grey.Medium);
            }
            else
            {
                col.Item().Table(tbl =>
                {
                    tbl.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(50);
                        c.RelativeColumn(3f);
                        c.ConstantColumn(80);
                    });
                    tbl.Header(h =>
                    {
                        HeaderCell(h, "Place");
                        HeaderCell(h, "Description");
                        HeaderCell(h, "Amount", alignRight: true);
                    });
                    for (var i = 0; i < s.Prizes.Place.Count; i++)
                    {
                        var p = s.Prizes.Place[i];
                        BodyCell(tbl, (i + 1).ToString());
                        BodyCell(tbl, p.Description ?? string.Empty);
                        BodyCell(tbl, $"${p.Value:0.##}", alignRight: true);
                    }
                });
            }

            col.Item().PaddingTop(4).Text("Class prizes").FontSize(12).SemiBold();
            if (s.Prizes.Class.Count == 0)
            {
                col.Item().Text("(none configured)").Italic().FontColor(Colors.Grey.Medium);
            }
            else
            {
                col.Item().Table(tbl =>
                {
                    tbl.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3f);
                        c.ConstantColumn(80);
                    });
                    tbl.Header(h =>
                    {
                        HeaderCell(h, "Description");
                        HeaderCell(h, "Amount", alignRight: true);
                    });
                    foreach (var p in s.Prizes.Class)
                    {
                        BodyCell(tbl, p.Description ?? string.Empty);
                        BodyCell(tbl, $"${p.Value:0.##}", alignRight: true);
                    }
                });
            }
        });
    }

    // =========================================================
    //  Shared cell helpers
    // =========================================================

    private static void HeaderCell(
        QuestPDF.Fluent.TableCellDescriptor tbl, string label,
        bool alignCenter = false, bool alignRight = false)
    {
        var cell = tbl.Cell().Background(Colors.Grey.Lighten3)
            .BorderBottom(0.8f).BorderColor(Colors.Grey.Medium)
            .PaddingVertical(4).PaddingHorizontal(4);
        var textElement =
            alignRight ? cell.AlignRight() :
            alignCenter ? cell.AlignCenter() :
            cell;
        textElement.Text(label).SemiBold().FontSize(9);
    }

    private static void BodyCell(
        QuestPDF.Fluent.TableDescriptor tbl, string? value,
        bool alignCenter = false, bool alignRight = false)
    {
        var cell = tbl.Cell().BorderBottom(0.25f).BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(3).PaddingHorizontal(4);
        var textElement =
            alignRight ? cell.AlignRight() :
            alignCenter ? cell.AlignCenter() :
            cell;
        textElement.Text(value ?? string.Empty).FontSize(9);
    }

    private static string FormatResult(PairingResult result) => result switch
    {
        PairingResult.WhiteWins => "1 – 0",
        PairingResult.BlackWins => "0 – 1",
        PairingResult.Draw      => "½ – ½",
        _ => string.Empty,
    };
}
