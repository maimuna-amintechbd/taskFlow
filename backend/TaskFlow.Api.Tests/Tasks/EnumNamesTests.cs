using TaskFlow.Api.Models;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Tests.Tasks;

/// <summary>Unit tests: EnumNames is a plain static class, so no API is needed.</summary>
public class EnumNamesTests
{
    [Theory]
    [InlineData("Todo", TaskItemStatus.Todo)]
    [InlineData("inprogress", TaskItemStatus.InProgress)]
    [InlineData("INPROGRESS", TaskItemStatus.InProgress)]
    [InlineData("DONE", TaskItemStatus.Done)]
    public void TryParse_ExactNameIgnoringCase_ReturnsTrue(string input, TaskItemStatus expected)
    {
        Assert.True(EnumNames.TryParse<TaskItemStatus>(input, out var value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("0")]
    [InlineData("2")]
    [InlineData("7")]
    [InlineData("-1")]
    [InlineData(" Todo")]
    [InlineData("Todo ")]
    [InlineData("Todo,Done")]
    [InlineData("In Progress")]
    [InlineData("Hgh")]
    public void TryParse_AnythingElse_ReturnsFalse(string? input)
    {
        Assert.False(EnumNames.TryParse<TaskItemStatus>(input, out var value));
        Assert.Equal(default, value);
    }

    [Fact]
    public void TryParse_WorksForAnyEnum()
    {
        Assert.True(EnumNames.TryParse<TaskPriority>("high", out var priority));
        Assert.Equal(TaskPriority.High, priority);
        Assert.False(EnumNames.TryParse<TaskPriority>("Todo", out _));
    }

    [Fact]
    public void List_JoinsNamesInDeclarationOrder()
    {
        Assert.Equal("Todo, InProgress, Done", EnumNames.List<TaskItemStatus>());
        Assert.Equal("Low, Medium, High", EnumNames.List<TaskPriority>());
    }
}
