namespace EnterpriseAgentOs.Domain.Primitives;

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
