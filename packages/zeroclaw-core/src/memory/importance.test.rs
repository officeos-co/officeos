    use super::*;

    #[test]
    fn core_category_has_high_base_score() {
        let score = compute_importance("some fact", &MemoryCategory::Core);
        assert!((score - 0.7).abs() < f64::EPSILON);
    }

    #[test]
    fn conversation_category_has_low_base_score() {
        let score = compute_importance("chat message", &MemoryCategory::Conversation);
        assert!((score - 0.2).abs() < f64::EPSILON);
    }

    #[test]
    fn keywords_boost_importance() {
        let score = compute_importance(
            "This is a critical decision that must always be followed",
            &MemoryCategory::Core,
        );
        // base 0.7 + boost for "critical", "decision", "must", "always" = 0.7 + 0.2 (capped) = 0.9
        assert!(score > 0.85);
    }

    #[test]
    fn boost_capped_at_point_two() {
        let score = compute_importance(
            "important critical decision rule policy must always never requirement principle",
            &MemoryCategory::Conversation,
        );
        // base 0.2 + max boost 0.2 = 0.4
        assert!((score - 0.4).abs() < f64::EPSILON);
    }

    #[test]
    fn weighted_final_score_formula() {
        let score = weighted_final_score(1.0, 1.0, 1.0);
        assert!((score - 1.0).abs() < f64::EPSILON);

        let score = weighted_final_score(0.0, 0.0, 0.0);
        assert!(score.abs() < f64::EPSILON);

        let score = weighted_final_score(0.5, 0.5, 0.5);
        assert!((score - 0.5).abs() < f64::EPSILON);
    }
