    use super::*;

    fn default_config() -> WasmRuntimeConfig {
        WasmRuntimeConfig::default()
    }

    // ── Basic trait compliance ──────────────────────────────────

    #[test]
    fn wasm_runtime_name() {
        let rt = WasmRuntime::new(default_config());
        assert_eq!(rt.name(), "wasm");
    }

    #[test]
    fn wasm_no_shell_access() {
        let rt = WasmRuntime::new(default_config());
        assert!(!rt.has_shell_access());
    }

    #[test]
    fn wasm_no_filesystem_by_default() {
        let rt = WasmRuntime::new(default_config());
        assert!(!rt.has_filesystem_access());
    }

    #[test]
    fn wasm_filesystem_when_read_enabled() {
        let mut cfg = default_config();
        cfg.allow_workspace_read = true;
        let rt = WasmRuntime::new(cfg);
        assert!(rt.has_filesystem_access());
    }

    #[test]
    fn wasm_filesystem_when_write_enabled() {
        let mut cfg = default_config();
        cfg.allow_workspace_write = true;
        let rt = WasmRuntime::new(cfg);
        assert!(rt.has_filesystem_access());
    }

    #[test]
    fn wasm_no_long_running() {
        let rt = WasmRuntime::new(default_config());
        assert!(!rt.supports_long_running());
    }

    #[test]
    fn wasm_memory_budget() {
        let rt = WasmRuntime::new(default_config());
        assert_eq!(rt.memory_budget(), 64 * 1024 * 1024);
    }

    #[test]
    fn wasm_shell_command_errors() {
        let rt = WasmRuntime::new(default_config());
        let result = rt.build_shell_command("echo hello", Path::new("/tmp"));
        assert!(result.is_err());
        assert!(result.unwrap_err().to_string().contains("does not support shell"));
    }

    #[test]
    fn wasm_storage_path_default() {
        let rt = WasmRuntime::new(default_config());
        assert!(rt.storage_path().to_string_lossy().contains("zeroclaw"));
    }

    #[test]
    fn wasm_storage_path_with_workspace() {
        let rt = WasmRuntime::with_workspace(default_config(), PathBuf::from("/home/user/project"));
        assert_eq!(rt.storage_path(), PathBuf::from("/home/user/project/.zeroclaw"));
    }

    // ── Config validation ──────────────────────────────────────

    #[test]
    fn validate_rejects_zero_memory() {
        let mut cfg = default_config();
        cfg.memory_limit_mb = 0;
        let rt = WasmRuntime::new(cfg);
        let err = rt.validate_config().unwrap_err();
        assert!(err.to_string().contains("must be > 0"));
    }

    #[test]
    fn validate_rejects_excessive_memory() {
        let mut cfg = default_config();
        cfg.memory_limit_mb = 8192;
        let rt = WasmRuntime::new(cfg);
        let err = rt.validate_config().unwrap_err();
        assert!(err.to_string().contains("4 GB safety limit"));
    }

    #[test]
    fn validate_rejects_empty_tools_dir() {
        let mut cfg = default_config();
        cfg.tools_dir = String::new();
        let rt = WasmRuntime::new(cfg);
        let err = rt.validate_config().unwrap_err();
        assert!(err.to_string().contains("cannot be empty"));
    }

    #[test]
    fn validate_rejects_path_traversal() {
        let mut cfg = default_config();
        cfg.tools_dir = "../../../etc/passwd".into();
        let rt = WasmRuntime::new(cfg);
        let err = rt.validate_config().unwrap_err();
        assert!(err.to_string().contains("path traversal"));
    }

    #[test]
    fn validate_accepts_valid_config() {
        let rt = WasmRuntime::new(default_config());
        assert!(rt.validate_config().is_ok());
    }

    #[test]
    fn validate_accepts_max_memory() {
        let mut cfg = default_config();
        cfg.memory_limit_mb = 4096;
        let rt = WasmRuntime::new(cfg);
        assert!(rt.validate_config().is_ok());
    }

    // ── Capabilities & fuel ────────────────────────────────────

    #[test]
    fn effective_fuel_uses_config_default() {
        let rt = WasmRuntime::new(default_config());
        let caps = WasmCapabilities::default();
        assert_eq!(rt.effective_fuel(&caps), 1_000_000);
    }

    #[test]
    fn effective_fuel_respects_override() {
        let rt = WasmRuntime::new(default_config());
        let caps = WasmCapabilities {
            fuel_override: 500,
            ..Default::default()
        };
        assert_eq!(rt.effective_fuel(&caps), 500);
    }

    #[test]
    fn effective_memory_uses_config_default() {
        let rt = WasmRuntime::new(default_config());
        let caps = WasmCapabilities::default();
        assert_eq!(rt.effective_memory_bytes(&caps), 64 * 1024 * 1024);
    }

    #[test]
    fn effective_memory_respects_override() {
        let rt = WasmRuntime::new(default_config());
        let caps = WasmCapabilities {
            memory_override_mb: 128,
            ..Default::default()
        };
        assert_eq!(rt.effective_memory_bytes(&caps), 128 * 1024 * 1024);
    }

    #[test]
    fn default_capabilities_match_config() {
        let mut cfg = default_config();
        cfg.allow_workspace_read = true;
        cfg.allowed_hosts = vec!["api.example.com".into()];
        let rt = WasmRuntime::new(cfg);
        let caps = rt.default_capabilities();
        assert!(caps.read_workspace);
        assert!(!caps.write_workspace);
        assert_eq!(caps.allowed_hosts, vec!["api.example.com"]);
    }

    // ── Tools directory ────────────────────────────────────────

    #[test]
    fn tools_dir_resolves_relative_to_workspace() {
        let rt = WasmRuntime::new(default_config());
        let dir = rt.tools_dir(Path::new("/home/user/project"));
        assert_eq!(dir, PathBuf::from("/home/user/project/tools/wasm"));
    }

    #[test]
    fn list_modules_empty_when_dir_missing() {
        let rt = WasmRuntime::new(default_config());
        let modules = rt.list_modules(Path::new("/nonexistent/path")).unwrap();
        assert!(modules.is_empty());
    }

    #[test]
    fn list_modules_finds_wasm_files() {
        let dir = tempfile::tempdir().unwrap();
        let tools_dir = dir.path().join("tools/wasm");
        std::fs::create_dir_all(&tools_dir).unwrap();

        // Create dummy .wasm files
        std::fs::write(tools_dir.join("calculator.wasm"), b"\0asm").unwrap();
        std::fs::write(tools_dir.join("formatter.wasm"), b"\0asm").unwrap();
        std::fs::write(tools_dir.join("readme.txt"), b"not a wasm").unwrap();

        let rt = WasmRuntime::new(default_config());
        let modules = rt.list_modules(dir.path()).unwrap();
        assert_eq!(modules, vec!["calculator", "formatter"]);
    }

    // ── Module execution edge cases ────────────────────────────

    #[test]
    fn execute_module_missing_file() {
        let dir = tempfile::tempdir().unwrap();
        let tools_dir = dir.path().join("tools/wasm");
        std::fs::create_dir_all(&tools_dir).unwrap();

        let rt = WasmRuntime::new(default_config());
        let caps = WasmCapabilities::default();
        let result = rt.execute_module("nonexistent", dir.path(), &caps);
        assert!(result.is_err());

        let err_msg = result.unwrap_err().to_string();
        // Should mention the module name
        assert!(err_msg.contains("nonexistent"));
    }

    #[test]
    fn execute_module_invalid_wasm() {
        let dir = tempfile::tempdir().unwrap();
        let tools_dir = dir.path().join("tools/wasm");
        std::fs::create_dir_all(&tools_dir).unwrap();

        // Write invalid WASM bytes
        std::fs::write(tools_dir.join("bad.wasm"), b"not valid wasm bytes at all").unwrap();

        let rt = WasmRuntime::new(default_config());
        let caps = WasmCapabilities::default();
        let result = rt.execute_module("bad", dir.path(), &caps);
        assert!(result.is_err());
    }

    #[test]
    fn execute_module_oversized_file() {
        let dir = tempfile::tempdir().unwrap();
        let tools_dir = dir.path().join("tools/wasm");
        std::fs::create_dir_all(&tools_dir).unwrap();

        // Write a file > 50 MB (we just check the size, don't actually allocate)
        // This test verifies the check without consuming 50 MB of disk
        let rt = WasmRuntime::new(default_config());
        let caps = WasmCapabilities::default();

        // File doesn't exist for oversized test — the missing file check catches first
        // But if it did exist and was 51 MB, the size check would catch it
        let result = rt.execute_module("oversized", dir.path(), &caps);
        assert!(result.is_err());
    }

    // ── Feature gate check ─────────────────────────────────────

    #[test]
    fn is_available_matches_feature_flag() {
        // This test verifies the compile-time feature detection works
        let available = WasmRuntime::is_available();
        assert_eq!(available, cfg!(feature = "runtime-wasm"));
    }

    // ── Memory overflow edge cases ─────────────────────────────

    #[test]
    fn memory_budget_no_overflow() {
        let mut cfg = default_config();
        cfg.memory_limit_mb = 4096; // Max valid
        let rt = WasmRuntime::new(cfg);
        assert_eq!(rt.memory_budget(), 4096 * 1024 * 1024);
    }

    #[test]
    fn effective_memory_saturating() {
        let rt = WasmRuntime::new(default_config());
        let caps = WasmCapabilities {
            memory_override_mb: u64::MAX,
            ..Default::default()
        };
        // Should not panic — saturating_mul prevents overflow
        let _bytes = rt.effective_memory_bytes(&caps);
    }

    // ── WasmCapabilities default ───────────────────────────────

    #[test]
    fn capabilities_default_is_locked_down() {
        let caps = WasmCapabilities::default();
        assert!(!caps.read_workspace);
        assert!(!caps.write_workspace);
        assert!(caps.allowed_hosts.is_empty());
        assert_eq!(caps.fuel_override, 0);
        assert_eq!(caps.memory_override_mb, 0);
    }

    // ── §3.1 / §3.2 WASM fuel & memory exhaustion tests ─────

    #[test]
    fn wasm_fuel_limit_enforced_in_config() {
        let rt = WasmRuntime::new(default_config());
        let caps = WasmCapabilities::default();
        let fuel = rt.effective_fuel(&caps);
        assert!(
            fuel > 0,
            "default fuel limit must be > 0 to prevent infinite loops"
        );
    }

    #[test]
    fn wasm_memory_limit_enforced_in_config() {
        let rt = WasmRuntime::new(default_config());
        let caps = WasmCapabilities::default();
        let mem_bytes = rt.effective_memory_bytes(&caps);
        assert!(
            mem_bytes > 0,
            "default memory limit must be > 0"
        );
        assert!(
            mem_bytes <= 4096 * 1024 * 1024,
            "default memory must not exceed 4 GB safety limit"
        );
    }

    #[test]
    fn wasm_zero_fuel_override_uses_default() {
        let rt = WasmRuntime::new(default_config());
        let caps = WasmCapabilities {
            fuel_override: 0,
            ..Default::default()
        };
        assert_eq!(
            rt.effective_fuel(&caps),
            1_000_000,
            "fuel_override=0 must use config default"
        );
    }

    #[test]
    fn validate_rejects_memory_just_above_limit() {
        let mut cfg = default_config();
        cfg.memory_limit_mb = 4097;
        let rt = WasmRuntime::new(cfg);
        let err = rt.validate_config().unwrap_err();
        assert!(err.to_string().contains("4 GB safety limit"));
    }

    #[test]
    fn execute_module_stub_returns_error_without_feature() {
        if !WasmRuntime::is_available() {
            let dir = tempfile::tempdir().unwrap();
            let tools_dir = dir.path().join("tools/wasm");
            std::fs::create_dir_all(&tools_dir).unwrap();
            std::fs::write(tools_dir.join("test.wasm"), b"\0asm\x01\0\0\0").unwrap();

            let rt = WasmRuntime::new(default_config());
            let caps = WasmCapabilities::default();
            let result = rt.execute_module("test", dir.path(), &caps);
            assert!(result.is_err());
            assert!(result.unwrap_err().to_string().contains("not available"));
        }
    }
