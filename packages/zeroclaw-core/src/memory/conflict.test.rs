    use super::*;

    #[test]
    fn jaccard_identical_strings() {
        let sim = jaccard_similarity("hello world", "hello world");
        assert!((sim - 1.0).abs() < f64::EPSILON);
    }

    #[test]
    fn jaccard_disjoint_strings() {
        let sim = jaccard_similarity("hello world", "foo bar");
        assert!(sim.abs() < f64::EPSILON);
    }

    #[test]
    fn jaccard_partial_overlap() {
        let sim = jaccard_similarity("the quick brown fox", "the slow brown dog");
        // overlap: "the", "brown" = 2; union: "the", "quick", "brown", "fox", "slow", "dog" = 6
        assert!((sim - 2.0 / 6.0).abs() < 0.01);
    }

    #[test]
    fn jaccard_empty_strings() {
        assert!((jaccard_similarity("", "") - 1.0).abs() < f64::EPSILON);
        assert!(jaccard_similarity("hello", "").abs() < f64::EPSILON);
        assert!(jaccard_similarity("", "hello").abs() < f64::EPSILON);
    }

    #[test]
    fn find_text_conflicts_filters_correctly() {
        let entries = vec![
            MemoryEntry {
                id: "1".into(),
                key: "pref".into(),
                content: "User prefers Rust for systems work".into(),
                category: MemoryCategory::Core,
                timestamp: "now".into(),
                session_id: None,
                score: None,
                namespace: "default".into(),
                importance: Some(0.7),
                superseded_by: None,
            },
            MemoryEntry {
                id: "2".into(),
                key: "daily1".into(),
                content: "User prefers Rust for systems work".into(),
                category: MemoryCategory::Daily,
                timestamp: "now".into(),
                session_id: None,
                score: None,
                namespace: "default".into(),
                importance: Some(0.3),
                superseded_by: None,
            },
        ];

        // Only Core entries should be flagged
        let conflicts = find_text_conflicts(&entries, "User now prefers Go for systems work", 0.3);
        assert_eq!(conflicts.len(), 1);
        assert_eq!(conflicts[0], "1");
    }
