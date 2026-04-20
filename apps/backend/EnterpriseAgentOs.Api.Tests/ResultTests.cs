using EnterpriseAgentOs.Domain.Primitives;

namespace EnterpriseAgentOs.Api.Tests;

public class ResultTests
{
    [Fact]
    public void Ok_result_IsSuccess_and_has_Value()
    {
        var result = AgentResult<int>.Ok(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Fail_result_IsFailure_and_has_Error_with_Category_and_Message()
    {
        var error = new AgentError(AgentErrorCategory.LlmCall, "timeout");
        var result = AgentResult<int>.Fail(error);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Equal(AgentErrorCategory.LlmCall, result.Error.Category);
        Assert.Equal("timeout", result.Error.Message);
    }

    [Fact]
    public void Match_routes_to_success_branch()
    {
        var result = AgentResult<int>.Ok(10);

        var output = result.Match(
            success: v => $"ok:{v}",
            failure: e => $"fail:{e.Message}");

        Assert.Equal("ok:10", output);
    }

    [Fact]
    public void Match_routes_to_failure_branch()
    {
        var error = new AgentError(AgentErrorCategory.PodConnection, "lost");
        var result = AgentResult<int>.Fail(error);

        var output = result.Match(
            success: v => $"ok:{v}",
            failure: e => $"fail:{e.Message}");

        Assert.Equal("fail:lost", output);
    }

    [Fact]
    public void Implicit_conversion_from_T_creates_Ok()
    {
        AgentResult<string> result = "hello";

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void Implicit_conversion_from_AgentError_creates_Fail()
    {
        var error = new AgentError(AgentErrorCategory.ToolExecution, "bad tool");
        AgentResult<string> result = error;

        Assert.True(result.IsFailure);
        Assert.Equal("bad tool", result.Error.Message);
    }

    [Fact]
    public void Bind_chains_success()
    {
        var result = AgentResult<int>.Ok(5)
            .Bind(v => AgentResult<string>.Ok($"value:{v}"));

        Assert.True(result.IsSuccess);
        Assert.Equal("value:5", result.Value);
    }

    [Fact]
    public void Bind_short_circuits_on_failure()
    {
        var error = new AgentError(AgentErrorCategory.Memory, "oom");
        var result = AgentResult<int>.Fail(error)
            .Bind(v => AgentResult<string>.Ok($"value:{v}"));

        Assert.True(result.IsFailure);
        Assert.Equal("oom", result.Error.Message);
    }

    [Fact]
    public void Accessing_Value_on_failure_throws_InvalidOperationException()
    {
        var result = AgentResult<int>.Fail(new AgentError(AgentErrorCategory.Configuration, "missing"));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Accessing_Error_on_success_throws_InvalidOperationException()
    {
        var result = AgentResult<int>.Ok(1);

        Assert.Throws<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void AgentError_Detail_is_null_by_default()
    {
        var error = new AgentError(AgentErrorCategory.SkillExecution, "fail");

        Assert.Null(error.Detail);
    }

    [Fact]
    public void AgentError_Detail_is_set_when_provided()
    {
        var error = new AgentError(AgentErrorCategory.TurnOrchestration, "fail", "stack trace here");

        Assert.Equal("stack trace here", error.Detail);
    }
}
