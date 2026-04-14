import { describe, it } from "bun:test";

describe("google-calendar", () => {
  describe("actions", () => {
    // Calendars
    it.todo("should expose list_calendars action");
    it.todo("should expose get_calendar action");
    it.todo("should expose create_calendar action");
    it.todo("should expose delete_calendar action");
    // Events
    it.todo("should expose list_events action");
    it.todo("should expose get_event action");
    it.todo("should expose create_event action");
    it.todo("should expose update_event action");
    it.todo("should expose delete_event action");
    it.todo("should expose move_event action");
    // Quick Add
    it.todo("should expose quick_add action");
    // Attendees
    it.todo("should expose add_attendee action");
    it.todo("should expose remove_attendee action");
    it.todo("should expose respond action");
    // Recurring Events
    it.todo("should expose list_instances action");
    // Free/Busy
    it.todo("should expose check_availability action");
    // Settings
    it.todo("should expose get_settings action");
  });

  describe("params", () => {
    describe("list_calendars", () => {
      it.todo("should require no parameters");
    });
    describe("get_calendar", () => {
      it.todo("should accept optional calendar_id with default primary");
    });
    describe("create_calendar", () => {
      it.todo("should require summary");
      it.todo("should accept optional time_zone");
    });
    describe("delete_calendar", () => {
      it.todo("should require calendar_id");
    });
    describe("list_events", () => {
      it.todo("should accept optional calendar_id");
      it.todo("should accept optional time_min");
      it.todo("should accept optional time_max");
      it.todo("should accept optional query");
      it.todo("should accept optional max_results");
    });
    describe("get_event", () => {
      it.todo("should require event_id");
      it.todo("should accept optional calendar_id");
    });
    describe("create_event", () => {
      it.todo("should require summary");
      it.todo("should require start");
      it.todo("should require end");
      it.todo("should accept optional calendar_id");
      it.todo("should accept optional location");
      it.todo("should accept optional description");
      it.todo("should accept optional attendees");
      it.todo("should accept optional recurrence");
      it.todo("should accept optional time_zone");
    });
    describe("update_event", () => {
      it.todo("should require event_id");
      it.todo("should accept optional calendar_id");
      it.todo("should accept optional summary");
      it.todo("should accept optional start");
      it.todo("should accept optional end");
      it.todo("should accept optional location");
      it.todo("should accept optional description");
      it.todo("should accept optional attendees");
    });
    describe("delete_event", () => {
      it.todo("should require event_id");
      it.todo("should accept optional calendar_id");
    });
    describe("move_event", () => {
      it.todo("should require event_id");
      it.todo("should require destination_calendar_id");
      it.todo("should accept optional calendar_id");
    });
    describe("quick_add", () => {
      it.todo("should require text");
      it.todo("should accept optional calendar_id");
    });
    describe("add_attendee", () => {
      it.todo("should require event_id");
      it.todo("should require email");
      it.todo("should accept optional calendar_id");
    });
    describe("remove_attendee", () => {
      it.todo("should require event_id");
      it.todo("should require email");
      it.todo("should accept optional calendar_id");
    });
    describe("respond", () => {
      it.todo("should require event_id");
      it.todo("should require status");
      it.todo("should accept optional calendar_id");
    });
    describe("list_instances", () => {
      it.todo("should require event_id");
      it.todo("should accept optional calendar_id");
      it.todo("should accept optional time_min");
      it.todo("should accept optional time_max");
      it.todo("should accept optional max_results");
    });
    describe("check_availability", () => {
      it.todo("should require time_min");
      it.todo("should require time_max");
      it.todo("should require calendars");
    });
    describe("get_settings", () => {
      it.todo("should require no parameters");
    });
  });

  describe("execute", () => {
    describe("list_calendars", () => {
      it.todo("should call Calendar API calendarList.list correctly");
      it.todo("should return list of calendars");
      it.todo("should handle errors");
    });
    describe("create_calendar", () => {
      it.todo("should call Calendar API calendars.insert correctly");
      it.todo("should return id and summary");
      it.todo("should handle errors");
    });
    describe("list_events", () => {
      it.todo("should call Calendar API events.list correctly");
      it.todo("should return list of events with attendees");
      it.todo("should handle errors");
    });
    describe("create_event", () => {
      it.todo("should call Calendar API events.insert correctly");
      it.todo("should return id and html_link");
      it.todo("should handle errors");
    });
    describe("update_event", () => {
      it.todo("should call Calendar API events.patch correctly");
      it.todo("should return updated event");
      it.todo("should handle errors");
    });
    describe("move_event", () => {
      it.todo("should call Calendar API events.move correctly");
      it.todo("should return id and new calendar_id");
      it.todo("should handle errors");
    });
    describe("quick_add", () => {
      it.todo("should call Calendar API events.quickAdd correctly");
      it.todo("should return parsed event");
      it.todo("should handle errors");
    });
    describe("add_attendee", () => {
      it.todo("should fetch event then patch with new attendee");
      it.todo("should return updated attendees list");
      it.todo("should handle errors");
    });
    describe("respond", () => {
      it.todo("should update self attendee status");
      it.todo("should return id and status");
      it.todo("should handle errors");
    });
    describe("check_availability", () => {
      it.todo("should call Calendar API freebusy.query correctly");
      it.todo("should return per-calendar busy intervals");
      it.todo("should handle errors");
    });
    describe("list_instances", () => {
      it.todo("should call Calendar API events.instances correctly");
      it.todo("should return list of individual occurrences");
      it.todo("should handle errors");
    });
  });
});
