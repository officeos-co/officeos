//! The agent turn loop — drives LLM streaming, tool dispatch, WS events.
//! See API.md §6.

/// Run a single turn: stream LLM response, dispatch tool calls, loop
/// until the assistant returns no tool calls or a termination condition
/// is reached.
pub(crate) async fn run_turn(_agent: &super::Agent) {
    todo!("Phase 3: implement turn loop per API.md §6 pseudocode")
}
