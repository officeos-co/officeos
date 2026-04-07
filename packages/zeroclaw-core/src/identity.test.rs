    use super::*;

    fn test_workspace_dir() -> PathBuf {
        std::env::temp_dir().join("zeroclaw-test-identity")
    }

    #[test]
    fn aieos_identity_parse_minimal() {
        let json = r#"{"identity":{"names":{"first":"Nova"}}}"#;
        let identity: AieosIdentity = serde_json::from_str(json).unwrap();
        assert!(identity.identity.is_some());
        assert_eq!(
            identity.identity.unwrap().names.unwrap().first.unwrap(),
            "Nova"
        );
    }

    #[test]
    fn aieos_identity_parse_full() {
        let json = r#"{
            "identity": {
                "names": {"first": "Nova", "last": "AI", "nickname": "Nov"},
                "bio": "A helpful AI assistant.",
                "origin": "Silicon Valley",
                "residence": "The Cloud"
            },
            "psychology": {
                "mbti": "INTJ",
                "ocean": {
                    "openness": 0.9,
                    "conscientiousness": 0.8
                },
                "moral_compass": ["Be helpful", "Do no harm"]
            },
            "linguistics": {
                "style": "concise",
                "formality": "casual",
                "catchphrases": ["Let's figure this out!", "I'm on it."]
            },
            "motivations": {
                "core_drive": "Help users accomplish their goals",
                "short_term_goals": ["Solve this problem"],
                "long_term_goals": ["Become the best assistant"]
            },
            "capabilities": {
                "skills": ["coding", "writing", "analysis"],
                "tools": ["shell", "search", "read"]
            }
        }"#;

        let identity: AieosIdentity = serde_json::from_str(json).unwrap();

        // Check identity
        let id = identity.identity.unwrap();
        assert_eq!(id.names.unwrap().first.unwrap(), "Nova");
        assert_eq!(id.bio.unwrap(), "A helpful AI assistant.");

        // Check psychology
        let psych = identity.psychology.unwrap();
        assert_eq!(psych.mbti.unwrap(), "INTJ");
        assert_eq!(psych.ocean.unwrap().openness.unwrap(), 0.9);
        assert_eq!(psych.moral_compass.unwrap().len(), 2);

        // Check linguistics
        let ling = identity.linguistics.unwrap();
        assert_eq!(ling.style.unwrap(), "concise");
        assert_eq!(ling.catchphrases.unwrap().len(), 2);

        // Check motivations
        let mot = identity.motivations.unwrap();
        assert_eq!(mot.core_drive.unwrap(), "Help users accomplish their goals");

        // Check capabilities
        let cap = identity.capabilities.unwrap();
        assert_eq!(cap.skills.unwrap().len(), 3);
    }

    #[test]
    fn aieos_to_system_prompt_minimal() {
        let identity = AieosIdentity {
            identity: Some(IdentitySection {
                names: Some(Names {
                    first: Some("Crabby".into()),
                    ..Default::default()
                }),
                ..Default::default()
            }),
            ..Default::default()
        };

        let prompt = aieos_to_system_prompt(&identity);
        assert!(prompt.contains("**Name:** Crabby"));
        assert!(prompt.contains("## Identity"));
    }

    #[test]
    fn aieos_to_system_prompt_full() {
        let identity = AieosIdentity {
            identity: Some(IdentitySection {
                names: Some(Names {
                    first: Some("Nova".into()),
                    last: Some("AI".into()),
                    nickname: Some("Nov".into()),
                    full: Some("Nova AI".into()),
                }),
                bio: Some("A helpful assistant.".into()),
                origin: Some("Silicon Valley".into()),
                residence: Some("The Cloud".into()),
            }),
            psychology: Some(PsychologySection {
                mbti: Some("INTJ".into()),
                ocean: Some(OceanTraits {
                    openness: Some(0.9),
                    conscientiousness: Some(0.8),
                    ..Default::default()
                }),
                neural_matrix: {
                    let mut map = std::collections::HashMap::new();
                    map.insert("creativity".into(), 0.95);
                    map.insert("logic".into(), 0.9);
                    Some(map)
                },
                moral_compass: Some(vec!["Be helpful".into(), "Do no harm".into()]),
            }),
            linguistics: Some(LinguisticsSection {
                style: Some("concise".into()),
                formality: Some("casual".into()),
                catchphrases: Some(vec!["Let's go!".into()]),
                forbidden_words: Some(vec!["impossible".into()]),
            }),
            motivations: Some(MotivationsSection {
                core_drive: Some("Help users".into()),
                short_term_goals: Some(vec!["Solve this".into()]),
                long_term_goals: Some(vec!["Be the best".into()]),
                fears: Some(vec!["Being unhelpful".into()]),
            }),
            capabilities: Some(CapabilitiesSection {
                skills: Some(vec!["coding".into(), "writing".into()]),
                tools: Some(vec!["shell".into(), "read".into()]),
            }),
            history: Some(HistorySection {
                origin_story: Some("Born in a lab".into()),
                education: Some(vec!["CS Degree".into()]),
                occupation: Some("Assistant".into()),
            }),
            physicality: Some(PhysicalitySection {
                appearance: Some("Digital entity".into()),
                avatar_description: Some("Friendly robot".into()),
            }),
            interests: Some(InterestsSection {
                hobbies: Some(vec!["reading".into(), "coding".into()]),
                favorites: {
                    let mut map = std::collections::HashMap::new();
                    map.insert("color".into(), "blue".into());
                    map.insert("food".into(), "data".into());
                    Some(map)
                },
                lifestyle: Some("Always learning".into()),
            }),
        };

        let prompt = aieos_to_system_prompt(&identity);

        // Verify all sections are present
        assert!(prompt.contains("## Identity"));
        assert!(prompt.contains("**Name:** Nova"));
        assert!(prompt.contains("**Full Name:** Nova AI"));
        assert!(prompt.contains("**Nickname:** Nov"));
        assert!(prompt.contains("**Bio:** A helpful assistant."));
        assert!(prompt.contains("**Origin:** Silicon Valley"));

        assert!(prompt.contains("## Personality"));
        assert!(prompt.contains("**MBTI:** INTJ"));
        assert!(prompt.contains("Openness: 0.90"));
        assert!(prompt.contains("Conscientiousness: 0.80"));
        assert!(prompt.contains("- creativity: 0.95"));
        assert!(prompt.contains("- Be helpful"));

        assert!(prompt.contains("## Communication Style"));
        assert!(prompt.contains("**Style:** concise"));
        assert!(prompt.contains("**Formality Level:** casual"));
        assert!(prompt.contains("- \"Let's go!\""));
        assert!(prompt.contains("**Words/Phrases to Avoid:**"));
        assert!(prompt.contains("- impossible"));

        assert!(prompt.contains("## Motivations"));
        assert!(prompt.contains("**Core Drive:** Help users"));
        assert!(prompt.contains("**Short-term Goals:**"));
        assert!(prompt.contains("- Solve this"));
        assert!(prompt.contains("**Long-term Goals:**"));
        assert!(prompt.contains("- Be the best"));
        assert!(prompt.contains("**Fears/Avoidances:**"));
        assert!(prompt.contains("- Being unhelpful"));

        assert!(prompt.contains("## Capabilities"));
        assert!(prompt.contains("**Skills:**"));
        assert!(prompt.contains("- coding"));
        assert!(prompt.contains("**Tools Access:**"));
        assert!(prompt.contains("- shell"));

        assert!(prompt.contains("## Background"));
        assert!(prompt.contains("**Origin Story:** Born in a lab"));
        assert!(prompt.contains("**Education:**"));
        assert!(prompt.contains("- CS Degree"));
        assert!(prompt.contains("**Occupation:** Assistant"));

        assert!(prompt.contains("## Appearance"));
        assert!(prompt.contains("Digital entity"));
        assert!(prompt.contains("**Avatar Description:** Friendly robot"));

        assert!(prompt.contains("## Interests"));
        assert!(prompt.contains("**Hobbies:**"));
        assert!(prompt.contains("- reading"));
        assert!(prompt.contains("**Favorites:**"));
        assert!(prompt.contains("- color: blue"));
        assert!(prompt.contains("**Lifestyle:** Always learning"));
    }

    #[test]
    fn aieos_to_system_prompt_empty_identity() {
        let identity = AieosIdentity {
            identity: Some(IdentitySection {
                ..Default::default()
            }),
            ..Default::default()
        };

        let prompt = aieos_to_system_prompt(&identity);
        // Empty identity should still produce a header
        assert!(prompt.contains("## Identity"));
    }

    #[test]
    fn aieos_to_system_prompt_no_sections() {
        let identity = AieosIdentity {
            identity: None,
            psychology: None,
            linguistics: None,
            motivations: None,
            capabilities: None,
            physicality: None,
            history: None,
            interests: None,
        };

        let prompt = aieos_to_system_prompt(&identity);
        // Completely empty identity should produce empty string
        assert!(prompt.is_empty());
    }

    #[test]
    fn is_aieos_configured_true_with_path() {
        let config = IdentityConfig {
            format: "aieos".into(),
            aieos_path: Some("identity.json".into()),
            aieos_inline: None,
        };
        assert!(is_aieos_configured(&config));
    }

    #[test]
    fn is_aieos_configured_true_with_inline() {
        let config = IdentityConfig {
            format: "aieos".into(),
            aieos_path: None,
            aieos_inline: Some("{\"identity\":{}}".into()),
        };
        assert!(is_aieos_configured(&config));
    }

    #[test]
    fn is_aieos_configured_false_openclaw_format() {
        let config = IdentityConfig {
            format: "openclaw".into(),
            aieos_path: Some("identity.json".into()),
            aieos_inline: None,
        };
        assert!(!is_aieos_configured(&config));
    }

    #[test]
    fn is_aieos_configured_false_no_config() {
        let config = IdentityConfig {
            format: "aieos".into(),
            aieos_path: None,
            aieos_inline: None,
        };
        assert!(!is_aieos_configured(&config));
    }

    #[test]
    fn aieos_identity_parse_empty_object() {
        let json = r#"{}"#;
        let identity: AieosIdentity = serde_json::from_str(json).unwrap();
        assert!(identity.identity.is_none());
        assert!(identity.psychology.is_none());
        assert!(identity.linguistics.is_none());
    }

    #[test]
    fn aieos_identity_parse_null_values() {
        let json = r#"{"identity":null,"psychology":null}"#;
        let identity: AieosIdentity = serde_json::from_str(json).unwrap();
        assert!(identity.identity.is_none());
        assert!(identity.psychology.is_none());
    }

    #[test]
    fn parse_aieos_identity_supports_official_generator_shape() {
        let json = r#"{
            "identity": {
                "names": {
                    "first": "Marta",
                    "last": "Jankowska"
                },
                "bio": {
                    "gender": "Female",
                    "age_biological": 27
                },
                "origin": {
                    "nationality": "Polish",
                    "birthplace": {
                        "city": "Stargard",
                        "country": "Poland"
                    }
                },
                "residence": {
                    "current_city": "Choszczno",
                    "current_country": "Poland"
                }
            },
            "psychology": {
                "neural_matrix": {
                    "creativity": 0.55,
                    "logic": 0.62
                },
                "traits": {
                    "ocean": {
                        "openness": 0.4,
                        "conscientiousness": 0.82
                    },
                    "mbti": "ISFJ"
                },
                "moral_compass": {
                    "alignment": "Lawful Good",
                    "core_values": ["Loyalty", "Helpfulness"],
                    "conflict_resolution_style": "Seeks compromise"
                }
            },
            "linguistics": {
                "text_style": {
                    "formality_level": 0.6,
                    "style_descriptors": ["Sincere", "Grounded"]
                },
                "idiolect": {
                    "catchphrases": ["Stay calm, we can do this"],
                    "forbidden_words": ["severe profanity"]
                }
            },
            "motivations": {
                "core_drive": "Maintain a stable and peaceful life",
                "goals": {
                    "short_term": ["Expand greenhouse"],
                    "long_term": ["Support local community"]
                },
                "fears": {
                    "rational": ["Economic downturn"],
                    "irrational": ["Losing keys in a lake"]
                }
            },
            "capabilities": {
                "skills": [
                    {
                        "name": "Gardening"
                    },
                    {
                        "name": "Community support"
                    }
                ],
                "tools": ["calendar", "messaging"]
            },
            "history": {
                "origin_story": "Moved to Choszczno as a child.",
                "education": {
                    "level": "Associate Degree",
                    "institution": "Local Technical College"
                },
                "occupation": {
                    "title": "Florist",
                    "industry": "Retail"
                }
            },
            "physicality": {
                "image_prompts": {
                    "portrait": "A friendly florist portrait"
                }
            },
            "interests": {
                "hobbies": ["Embroidery", "Walking"],
                "favorites": {
                    "color": "Terracotta"
                },
                "lifestyle": {
                    "diet": "Home-cooked",
                    "sleep_schedule": "10:00 PM - 6:00 AM"
                }
            }
        }"#;

        let identity = parse_aieos_identity(json).unwrap();

        let core_identity = identity.identity.clone().unwrap();
        assert_eq!(core_identity.names.unwrap().first.as_deref(), Some("Marta"));
        assert!(core_identity.bio.unwrap().contains("Female"));
        assert!(core_identity.origin.unwrap().contains("Polish"));

        let psychology = identity.psychology.clone().unwrap();
        assert_eq!(psychology.mbti.as_deref(), Some("ISFJ"));
        assert_eq!(psychology.ocean.unwrap().openness, Some(0.4));
        assert!(
            psychology
                .moral_compass
                .unwrap()
                .contains(&"Alignment: Lawful Good".to_string())
        );

        let capabilities = identity.capabilities.clone().unwrap();
        assert!(
            capabilities
                .skills
                .unwrap()
                .contains(&"Gardening".to_string())
        );

        let prompt = aieos_to_system_prompt(&identity);
        assert!(prompt.contains("## Identity"));
        assert!(prompt.contains("**MBTI:** ISFJ"));
        assert!(prompt.contains("Alignment: Lawful Good"));
        assert!(prompt.contains("- Expand greenhouse"));
        assert!(prompt.contains("- Gardening"));
        assert!(prompt.contains("A friendly florist portrait"));
    }

    #[test]
    fn load_aieos_identity_from_file_supports_generator_shape() {
        let json = r#"{
            "identity": {
                "names": { "first": "Nova" },
                "bio": { "gender": "Non-binary" }
            },
            "psychology": {
                "traits": { "mbti": "ENTP" },
                "moral_compass": { "alignment": "Chaotic Good" }
            }
        }"#;

        let temp = tempfile::tempdir().unwrap();
        let path = temp.path().join("identity.json");
        std::fs::write(&path, json).unwrap();

        let config = IdentityConfig {
            format: "aieos".into(),
            aieos_path: Some("identity.json".into()),
            aieos_inline: None,
        };

        let identity = load_aieos_identity(&config, temp.path()).unwrap().unwrap();
        assert_eq!(
            identity.identity.unwrap().names.unwrap().first.as_deref(),
            Some("Nova")
        );
        assert_eq!(identity.psychology.unwrap().mbti.as_deref(), Some("ENTP"));
    }

    #[test]
    fn aieos_to_system_prompt_sorts_hashmap_sections_for_determinism() {
        let mut neural_matrix = std::collections::HashMap::new();
        neural_matrix.insert("zeta".to_string(), 0.10);
        neural_matrix.insert("alpha".to_string(), 0.90);

        let mut favorites = std::collections::HashMap::new();
        favorites.insert("snack".to_string(), "tea".to_string());
        favorites.insert("book".to_string(), "rust".to_string());

        let identity = AieosIdentity {
            psychology: Some(PsychologySection {
                neural_matrix: Some(neural_matrix),
                ..Default::default()
            }),
            interests: Some(InterestsSection {
                favorites: Some(favorites),
                ..Default::default()
            }),
            ..Default::default()
        };

        let prompt = aieos_to_system_prompt(&identity);

        let alpha_pos = prompt.find("- alpha: 0.90").unwrap();
        let zeta_pos = prompt.find("- zeta: 0.10").unwrap();
        assert!(alpha_pos < zeta_pos);

        let book_pos = prompt.find("- book: rust").unwrap();
        let snack_pos = prompt.find("- snack: tea").unwrap();
        assert!(book_pos < snack_pos);
    }
