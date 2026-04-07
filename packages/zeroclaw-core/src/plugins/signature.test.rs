    use super::*;

    const TEST_MANIFEST: &str = r#"
name = "test-plugin"
version = "0.1.0"
description = "A test plugin"
wasm_path = "plugin.wasm"
capabilities = ["tool"]
permissions = []
"#;

    fn generate_test_keypair() -> (Vec<u8>, String) {
        generate_signing_key().expect("keygen should succeed")
    }

    #[test]
    fn test_canonical_manifest_strips_signature_fields() {
        let manifest_with_sig = r#"
name = "test-plugin"
version = "0.1.0"
signature = "abc123"
publisher_key = "deadbeef"
wasm_path = "plugin.wasm"
capabilities = ["tool"]
"#;
        let canonical = canonical_manifest_bytes(manifest_with_sig);
        let canonical_str = String::from_utf8(canonical).unwrap();
        assert!(!canonical_str.contains("signature"));
        assert!(!canonical_str.contains("publisher_key"));
        assert!(canonical_str.contains("name = \"test-plugin\""));
        assert!(canonical_str.contains("wasm_path = \"plugin.wasm\""));
    }

    #[test]
    fn test_canonical_manifest_without_signature_fields() {
        let canonical = canonical_manifest_bytes(TEST_MANIFEST);
        let canonical_str = String::from_utf8(canonical).unwrap();
        assert!(canonical_str.contains("name = \"test-plugin\""));
    }

    #[test]
    fn test_sign_and_verify_roundtrip() {
        let (pkcs8, pub_hex) = generate_test_keypair();
        let sig = sign_manifest(TEST_MANIFEST, &pkcs8).unwrap();
        let trusted_keys = vec![pub_hex.clone()];
        let result = verify_manifest(TEST_MANIFEST, &sig, &pub_hex, &trusted_keys);
        assert!(result.is_valid());
        assert_eq!(
            result,
            VerificationResult::Valid {
                publisher_key: pub_hex.to_lowercase()
            }
        );
    }

    #[test]
    fn test_verify_rejects_tampered_manifest() {
        let (pkcs8, pub_hex) = generate_test_keypair();
        let sig = sign_manifest(TEST_MANIFEST, &pkcs8).unwrap();
        let tampered = TEST_MANIFEST.replace("0.1.0", "0.2.0");
        let trusted_keys = vec![pub_hex.clone()];
        let result = verify_manifest(&tampered, &sig, &pub_hex, &trusted_keys);
        assert!(matches!(result, VerificationResult::Invalid { .. }));
    }

    #[test]
    fn test_verify_rejects_wrong_key() {
        let (pkcs8, _pub_hex) = generate_test_keypair();
        let (_pkcs8_2, pub_hex_2) = generate_test_keypair();
        let sig = sign_manifest(TEST_MANIFEST, &pkcs8).unwrap();
        let trusted_keys = vec![pub_hex_2.clone()];
        let result = verify_manifest(TEST_MANIFEST, &sig, &pub_hex_2, &trusted_keys);
        assert!(matches!(result, VerificationResult::Invalid { .. }));
    }

    #[test]
    fn test_verify_untrusted_publisher() {
        let (pkcs8, pub_hex) = generate_test_keypair();
        let sig = sign_manifest(TEST_MANIFEST, &pkcs8).unwrap();
        let trusted_keys: Vec<String> = vec![]; // no trusted keys
        let result = verify_manifest(TEST_MANIFEST, &sig, &pub_hex, &trusted_keys);
        assert_eq!(result, VerificationResult::Untrusted);
    }

    #[test]
    fn test_public_key_hex_matches_generate() {
        let (pkcs8, pub_hex) = generate_test_keypair();
        let derived_hex = public_key_hex(&pkcs8).unwrap();
        assert_eq!(pub_hex, derived_hex);
    }

    #[test]
    fn test_hex_roundtrip() {
        let data = vec![0xDE, 0xAD, 0xBE, 0xEF];
        let encoded = hex_encode(&data);
        assert_eq!(encoded, "deadbeef");
        let decoded = hex_decode(&encoded).unwrap();
        assert_eq!(decoded, data);
    }

    #[test]
    fn test_enforce_policy_disabled_mode() {
        let result = enforce_signature_policy(
            "test",
            TEST_MANIFEST,
            None,
            None,
            &[],
            SignatureMode::Disabled,
        )
        .unwrap();
        assert_eq!(result, VerificationResult::Unsigned);
    }

    #[test]
    fn test_enforce_policy_strict_rejects_unsigned() {
        let err = enforce_signature_policy(
            "test",
            TEST_MANIFEST,
            None,
            None,
            &[],
            SignatureMode::Strict,
        )
        .unwrap_err();
        assert!(matches!(err, PluginError::UnsignedPlugin(_)));
    }

    #[test]
    fn test_enforce_policy_permissive_allows_unsigned() {
        let result = enforce_signature_policy(
            "test",
            TEST_MANIFEST,
            None,
            None,
            &[],
            SignatureMode::Permissive,
        )
        .unwrap();
        assert_eq!(result, VerificationResult::Unsigned);
    }

    #[test]
    fn test_enforce_policy_strict_rejects_untrusted() {
        let (pkcs8, pub_hex) = generate_test_keypair();
        let sig = sign_manifest(TEST_MANIFEST, &pkcs8).unwrap();
        let err = enforce_signature_policy(
            "test",
            TEST_MANIFEST,
            Some(&sig),
            Some(&pub_hex),
            &[], // no trusted keys
            SignatureMode::Strict,
        )
        .unwrap_err();
        assert!(matches!(err, PluginError::UntrustedPublisher { .. }));
    }

    #[test]
    fn test_enforce_policy_strict_accepts_valid_signature() {
        let (pkcs8, pub_hex) = generate_test_keypair();
        let sig = sign_manifest(TEST_MANIFEST, &pkcs8).unwrap();
        let trusted_keys = vec![pub_hex.clone()];
        let result = enforce_signature_policy(
            "test",
            TEST_MANIFEST,
            Some(&sig),
            Some(&pub_hex),
            &trusted_keys,
            SignatureMode::Strict,
        )
        .unwrap();
        assert!(result.is_valid());
    }

    #[test]
    fn test_enforce_policy_strict_rejects_invalid_signature() {
        let (pkcs8, pub_hex) = generate_test_keypair();
        let _sig = sign_manifest(TEST_MANIFEST, &pkcs8).unwrap();
        let trusted_keys = vec![pub_hex.clone()];
        let err = enforce_signature_policy(
            "test",
            TEST_MANIFEST,
            Some("badsignature"),
            Some(&pub_hex),
            &trusted_keys,
            SignatureMode::Strict,
        )
        .unwrap_err();
        assert!(matches!(err, PluginError::SignatureInvalid(_)));
    }

    #[test]
    fn test_signature_mode_default_is_disabled() {
        assert_eq!(SignatureMode::default(), SignatureMode::Disabled);
    }

    #[test]
    fn test_manifest_with_signature_fields_verifies() {
        let (pkcs8, pub_hex) = generate_test_keypair();
        // Sign the manifest without signature fields
        let sig = sign_manifest(TEST_MANIFEST, &pkcs8).unwrap();

        // Now create a manifest that includes the signature fields
        let manifest_with_sig = format!(
            r#"
name = "test-plugin"
version = "0.1.0"
description = "A test plugin"
signature = "{sig}"
publisher_key = "{pub_hex}"
wasm_path = "plugin.wasm"
capabilities = ["tool"]
permissions = []
"#
        );

        // Verification should still work because canonical bytes strip sig fields
        let trusted_keys = vec![pub_hex.clone()];
        let result = verify_manifest(&manifest_with_sig, &sig, &pub_hex, &trusted_keys);
        assert!(result.is_valid());
    }
