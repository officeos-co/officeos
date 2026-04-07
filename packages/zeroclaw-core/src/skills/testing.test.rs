    use super::*;
    use std::fs;

    #[test]
    fn parse_comment_and_empty_lines() {
        assert!(parse_test_line("").is_none());
        assert!(parse_test_line("   ").is_none());
        assert!(parse_test_line("# this is a comment").is_none());
        assert!(parse_test_line("  # indented comment").is_none());
    }

    #[test]
    fn parse_valid_test_line() {
        let case = parse_test_line("echo hello | 0 | hello").unwrap();
        assert_eq!(case.command, "echo hello");
        assert_eq!(case.expected_exit, 0);
        assert_eq!(case.expected_pattern, "hello");
    }

    #[test]
    fn parse_line_with_spaces_in_pattern() {
        let case = parse_test_line("echo 'hello world' | 0 | hello world").unwrap();
        assert_eq!(case.command, "echo 'hello world'");
        assert_eq!(case.expected_exit, 0);
        assert_eq!(case.expected_pattern, "hello world");
    }

    #[test]
    fn parse_invalid_line_missing_parts() {
        assert!(parse_test_line("just a command").is_none());
        assert!(parse_test_line("cmd | notanumber | pattern").is_none());
    }

    #[test]
    fn pattern_matches_empty() {
        assert!(pattern_matches("anything", ""));
    }

    #[test]
    fn pattern_matches_substring() {
        assert!(pattern_matches("hello world", "hello"));
        assert!(pattern_matches("hello world", "world"));
        assert!(!pattern_matches("hello world", "missing"));
    }

    #[test]
    fn pattern_matches_regex() {
        assert!(pattern_matches("hello world 42", r"world \d+"));
        assert!(pattern_matches("/usr/bin/bash", r"/"));
        assert!(!pattern_matches("hello", r"^\d+$"));
    }

    #[test]
    fn test_skill_with_echo() {
        let dir = tempfile::tempdir().unwrap();
        let skill_dir = dir.path().join("echo-skill");
        fs::create_dir_all(&skill_dir).unwrap();
        fs::write(
            skill_dir.join("TEST.sh"),
            "# Echo test\necho hello | 0 | hello\n",
        )
        .unwrap();

        let result = test_skill(&skill_dir, "echo-skill", false).unwrap();
        assert_eq!(result.tests_run, 1);
        assert_eq!(result.tests_passed, 1);
        assert!(result.failures.is_empty());
    }

    #[test]
    fn test_skill_without_test_file() {
        let dir = tempfile::tempdir().unwrap();
        let skill_dir = dir.path().join("no-tests");
        fs::create_dir_all(&skill_dir).unwrap();

        let result = test_skill(&skill_dir, "no-tests", false).unwrap();
        assert_eq!(result.tests_run, 0);
        assert_eq!(result.tests_passed, 0);
        assert!(result.failures.is_empty());
    }

    #[test]
    fn test_skill_with_failing_test() {
        let dir = tempfile::tempdir().unwrap();
        let skill_dir = dir.path().join("fail-skill");
        fs::create_dir_all(&skill_dir).unwrap();
        fs::write(skill_dir.join("TEST.sh"), "echo hello | 1 | goodbye\n").unwrap();

        let result = test_skill(&skill_dir, "fail-skill", false).unwrap();
        assert_eq!(result.tests_run, 1);
        assert_eq!(result.tests_passed, 0);
        assert_eq!(result.failures.len(), 1);
        assert_eq!(result.failures[0].expected_exit, 1);
        assert_eq!(result.failures[0].actual_exit, 0);
    }

    #[test]
    fn test_skill_exit_code_mismatch() {
        let dir = tempfile::tempdir().unwrap();
        let skill_dir = dir.path().join("exit-mismatch");
        fs::create_dir_all(&skill_dir).unwrap();
        fs::write(skill_dir.join("TEST.sh"), "false | 0 | \n").unwrap();

        let result = test_skill(&skill_dir, "exit-mismatch", false).unwrap();
        assert_eq!(result.tests_run, 1);
        assert_eq!(result.tests_passed, 0);
        assert_eq!(result.failures[0].actual_exit, 1);
    }

    #[test]
    fn test_result_aggregation() {
        let results = [
            SkillTestResult {
                skill_name: "a".to_string(),
                tests_run: 3,
                tests_passed: 3,
                failures: Vec::new(),
            },
            SkillTestResult {
                skill_name: "b".to_string(),
                tests_run: 2,
                tests_passed: 1,
                failures: vec![TestFailure {
                    command: "false".to_string(),
                    expected_exit: 0,
                    actual_exit: 1,
                    expected_pattern: String::new(),
                    actual_output: String::new(),
                }],
            },
        ];

        let total_run: usize = results.iter().map(|r| r.tests_run).sum();
        let total_passed: usize = results.iter().map(|r| r.tests_passed).sum();
        assert_eq!(total_run, 5);
        assert_eq!(total_passed, 4);
    }

    #[test]
    fn test_all_skills_finds_skills_with_tests() {
        let dir = tempfile::tempdir().unwrap();
        let skills_dir = dir.path().join("skills");

        // Skill with TEST.sh
        let skill_a = skills_dir.join("skill-a");
        fs::create_dir_all(&skill_a).unwrap();
        fs::write(skill_a.join("TEST.sh"), "echo ok | 0 | ok\n").unwrap();

        // Skill without TEST.sh — should be skipped
        let skill_b = skills_dir.join("skill-b");
        fs::create_dir_all(&skill_b).unwrap();

        let results = test_all_skills(std::slice::from_ref(&skills_dir), false).unwrap();
        assert_eq!(results.len(), 1);
        assert_eq!(results[0].skill_name, "skill-a");
        assert_eq!(results[0].tests_passed, 1);
    }

    #[test]
    fn test_truncate_output() {
        assert_eq!(truncate_output("short", 100), "short");
        let long = "a".repeat(300);
        let truncated = truncate_output(&long, 200);
        assert!(truncated.ends_with("..."));
        assert!(truncated.len() <= 204); // 200 + "..."
    }
