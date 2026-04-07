    use super::{Input, trim_trailing_line_ending};
    use anyhow::Result;
    use std::io::Cursor;

    #[test]
    fn trim_trailing_line_ending_strips_newlines() {
        assert_eq!(trim_trailing_line_ending("value\n"), "value");
        assert_eq!(trim_trailing_line_ending("value\r\n"), "value");
        assert_eq!(trim_trailing_line_ending("value\r"), "value");
        assert_eq!(trim_trailing_line_ending("value"), "value");
    }

    #[test]
    fn interact_text_returns_typed_value_without_newline() -> Result<()> {
        let input = Input::new().with_prompt("Prompt");
        let mut output = Vec::new();

        let value = input.interact_text_with_io(Cursor::new(b"typed-value\n"), &mut output)?;

        assert_eq!(value, "typed-value");
        assert_eq!(String::from_utf8(output)?, "Prompt: ");
        Ok(())
    }

    #[test]
    fn interact_text_returns_default_for_blank_input() -> Result<()> {
        let input = Input::new().with_prompt("Prompt").default("fallback");
        let mut output = Vec::new();

        let value = input.interact_text_with_io(Cursor::new(b"\n"), &mut output)?;

        assert_eq!(value, "fallback");
        assert_eq!(String::from_utf8(output)?, "Prompt [fallback]: ");
        Ok(())
    }

    #[test]
    fn interact_text_allows_empty_when_requested() -> Result<()> {
        let input = Input::new().with_prompt("Prompt").allow_empty(true);
        let mut output = Vec::new();

        let value = input.interact_text_with_io(Cursor::new(b"\n"), &mut output)?;

        assert_eq!(value, "");
        assert_eq!(String::from_utf8(output)?, "Prompt: ");
        Ok(())
    }

    #[test]
    fn interact_text_reprompts_when_empty_is_not_allowed() -> Result<()> {
        let input = Input::new().with_prompt("Prompt");
        let mut output = Vec::new();

        let value = input.interact_text_with_io(Cursor::new(b"\nsecond-try\n"), &mut output)?;

        assert_eq!(value, "second-try");
        assert_eq!(
            String::from_utf8(output)?,
            "Prompt: Input cannot be empty.\nPrompt: "
        );
        Ok(())
    }
