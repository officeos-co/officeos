import { describe, it } from "bun:test";

describe("cloudwatch", () => {
  describe("get_metrics", () => {
    it.todo("should call GetMetricStatistics with namespace and metric_name");
    it.todo("should apply dimensions from JSON param");
    it.todo("should return timestamp, value, unit");
  });

  describe("put_metric_alarm", () => {
    it.todo("should call PutMetricAlarm with all required params");
    it.todo("should add AlarmActions for each SNS ARN");
    it.todo("should return alarm_arn with region in path");
  });

  describe("list_alarms", () => {
    it.todo("should call DescribeAlarms without StateValue for 'all'");
    it.todo("should filter by state when specified");
    it.todo("should filter by prefix when provided");
  });

  describe("get_log_events", () => {
    it.todo("should call GetLogEvents when log_stream provided");
    it.todo("should call FilterLogEvents when no log_stream");
    it.todo("should apply start_time and end_time");
  });
});
