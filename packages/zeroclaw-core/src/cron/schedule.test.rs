use super::*;
use chrono::{Datelike, TimeZone};

#[test]
fn next_run_for_schedule_supports_every_and_at() {
    let now = Utc::now();
    let every = Schedule::Every { every_ms: 60_000 };
    let next = next_run_for_schedule(&every, now).unwrap();
    assert!(next > now);

    let at = now + ChronoDuration::minutes(10);
    let at_schedule = Schedule::At { at };
    let next_at = next_run_for_schedule(&at_schedule, now).unwrap();
    assert_eq!(next_at, at);
}

#[test]
fn next_run_for_schedule_supports_timezone() {
    let from = Utc.with_ymd_and_hms(2026, 2, 16, 0, 0, 0).unwrap();
    let schedule = Schedule::Cron {
        expr: "0 9 * * *".into(),
        tz: Some("America/Los_Angeles".into()),
    };

    let next = next_run_for_schedule(&schedule, from).unwrap();
    assert_eq!(next, Utc.with_ymd_and_hms(2026, 2, 16, 17, 0, 0).unwrap());
}

#[test]
fn normalize_weekday_field_translates_standard_crontab_values() {
    // Single values: standard crontab → cron crate
    assert_eq!(normalize_weekday_field("0").unwrap(), "1"); // Sun
    assert_eq!(normalize_weekday_field("1").unwrap(), "2"); // Mon
    assert_eq!(normalize_weekday_field("5").unwrap(), "6"); // Fri
    assert_eq!(normalize_weekday_field("6").unwrap(), "7"); // Sat
    assert_eq!(normalize_weekday_field("7").unwrap(), "1"); // Sun (alias)
}

#[test]
fn normalize_weekday_field_translates_ranges() {
    // 1-5 (Mon-Fri) → 2-6
    assert_eq!(normalize_weekday_field("1-5").unwrap(), "2-6");
    // 0-6 (Sun-Sat) → 1-7
    assert_eq!(normalize_weekday_field("0-6").unwrap(), "1-7");
}

#[test]
fn normalize_weekday_field_translates_lists() {
    // 0,6 (Sun,Sat) → 1,7
    assert_eq!(normalize_weekday_field("0,6").unwrap(), "1,7");
    // 1,3,5 (Mon,Wed,Fri) → 2,4,6
    assert_eq!(normalize_weekday_field("1,3,5").unwrap(), "2,4,6");
}

#[test]
fn normalize_weekday_field_translates_steps() {
    // 1-5/2 (Mon-Fri every other) → 2-6/2
    assert_eq!(normalize_weekday_field("1-5/2").unwrap(), "2-6/2");
    // */2 (every other day) → */2
    assert_eq!(normalize_weekday_field("*/2").unwrap(), "*/2");
}

#[test]
fn normalize_weekday_field_passes_through_wildcards_and_names() {
    assert_eq!(normalize_weekday_field("*").unwrap(), "*");
    assert_eq!(normalize_weekday_field("?").unwrap(), "?");
    assert_eq!(normalize_weekday_field("MON-FRI").unwrap(), "MON-FRI");
    assert_eq!(
        normalize_weekday_field("MON,WED,FRI").unwrap(),
        "MON,WED,FRI"
    );
}

#[test]
fn normalize_expression_applies_weekday_fix_to_5_field() {
    // "0 9 * * 1-5" should become "0 0 9 * * 2-6"
    let result = normalize_expression("0 9 * * 1-5").unwrap();
    assert_eq!(result, "0 0 9 * * 2-6");
}

#[test]
fn normalize_expression_does_not_modify_6_field() {
    // 6-field expressions already use cron-crate semantics
    let result = normalize_expression("0 0 9 * * 1-5").unwrap();
    assert_eq!(result, "0 0 9 * * 1-5");
}

#[test]
fn weekday_1_5_schedules_monday_through_friday() {
    // 2026-02-16 is a Monday. With "0 9 * * 1-5" (Mon-Fri at 09:00 UTC),
    // the next run from Sunday 2026-02-15 should be Monday 2026-02-16.
    let sunday = Utc.with_ymd_and_hms(2026, 2, 15, 0, 0, 0).unwrap();
    let schedule = Schedule::Cron {
        expr: "0 9 * * 1-5".into(),
        tz: None,
    };
    let next = next_run_for_schedule(&schedule, sunday).unwrap();
    // Should be Monday 2026-02-16 at 09:00 UTC (weekday = Mon)
    assert_eq!(next, Utc.with_ymd_and_hms(2026, 2, 16, 9, 0, 0).unwrap());
    assert_eq!(next.weekday(), chrono::Weekday::Mon);
}

#[test]
fn weekday_1_5_does_not_fire_on_saturday_or_sunday() {
    // From Friday evening, next run should skip Sat/Sun → Monday
    let friday_evening = Utc.with_ymd_and_hms(2026, 2, 20, 18, 0, 0).unwrap();
    let schedule = Schedule::Cron {
        expr: "0 9 * * 1-5".into(),
        tz: None,
    };
    let next = next_run_for_schedule(&schedule, friday_evening).unwrap();
    // Should be Monday 2026-02-23 at 09:00 UTC
    assert_eq!(next, Utc.with_ymd_and_hms(2026, 2, 23, 9, 0, 0).unwrap());
    assert_eq!(next.weekday(), chrono::Weekday::Mon);
}

#[test]
fn weekday_0_means_sunday() {
    // "0 10 * * 0" should fire on Sunday only
    let monday = Utc.with_ymd_and_hms(2026, 2, 16, 0, 0, 0).unwrap();
    let schedule = Schedule::Cron {
        expr: "0 10 * * 0".into(),
        tz: None,
    };
    let next = next_run_for_schedule(&schedule, monday).unwrap();
    assert_eq!(next.weekday(), chrono::Weekday::Sun);
}

#[test]
fn weekday_7_means_sunday() {
    // "0 10 * * 7" should also fire on Sunday (alias)
    let monday = Utc.with_ymd_and_hms(2026, 2, 16, 0, 0, 0).unwrap();
    let schedule = Schedule::Cron {
        expr: "0 10 * * 7".into(),
        tz: None,
    };
    let next = next_run_for_schedule(&schedule, monday).unwrap();
    assert_eq!(next.weekday(), chrono::Weekday::Sun);
}
