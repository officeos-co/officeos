namespace EnterpriseAgentOs.Domain.Common.Primitives;

public enum AgentErrorCategory
{
    PodConnection,
    LlmCall,
    ToolExecution,
    SkillExecution,
    TurnOrchestration,
    Memory,
    Configuration,
}
