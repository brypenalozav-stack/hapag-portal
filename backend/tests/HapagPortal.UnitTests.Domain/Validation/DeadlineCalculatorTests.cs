namespace HapagPortal.UnitTests.Domain.Validation;

using FluentAssertions;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Validation;

public sealed class DeadlineCalculatorTests
{
    private static readonly DateTime Arrival = new(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ComputeDueAt_NegativeOffset_IsBeforeEvent()
    {
        DeadlineCalculator.ComputeDueAt(Arrival, -48).Should().Be(Arrival.AddHours(-48));
    }

    [Fact]
    public void ComputeDueAt_PositiveOffset_IsAfterEvent()
    {
        DeadlineCalculator.ComputeDueAt(Arrival, 24).Should().Be(Arrival.AddHours(24));
    }

    [Fact]
    public void ComputeStatus_WhenCompleted_IsMet()
    {
        var due = Arrival.AddHours(-48);
        DeadlineCalculator.ComputeStatus(due, now: Arrival, completedAt: due.AddHours(-1), atRiskWindowHours: 12)
            .Should().Be(DeadlineStatus.Met);
    }

    [Fact]
    public void ComputeStatus_WhenNowPastDue_IsOverdue()
    {
        var due = Arrival.AddHours(-48);
        DeadlineCalculator.ComputeStatus(due, now: due.AddMinutes(1), completedAt: null, atRiskWindowHours: 12)
            .Should().Be(DeadlineStatus.Overdue);
    }

    [Fact]
    public void ComputeStatus_ExactlyAtDue_IsOverdue()
    {
        var due = Arrival.AddHours(-48);
        DeadlineCalculator.ComputeStatus(due, now: due, completedAt: null, atRiskWindowHours: 12)
            .Should().Be(DeadlineStatus.Overdue);
    }

    [Fact]
    public void ComputeStatus_WithinRiskWindow_IsAtRisk()
    {
        var due = Arrival.AddHours(-48);
        // faltan 6 h para el vencimiento, ventana de riesgo 12 h -> AtRisk
        DeadlineCalculator.ComputeStatus(due, now: due.AddHours(-6), completedAt: null, atRiskWindowHours: 12)
            .Should().Be(DeadlineStatus.AtRisk);
    }

    [Fact]
    public void ComputeStatus_FarFromDue_IsOnTrack()
    {
        var due = Arrival.AddHours(-48);
        DeadlineCalculator.ComputeStatus(due, now: due.AddHours(-72), completedAt: null, atRiskWindowHours: 12)
            .Should().Be(DeadlineStatus.OnTrack);
    }
}
