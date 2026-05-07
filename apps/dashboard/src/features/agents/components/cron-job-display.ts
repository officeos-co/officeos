export function describeCronExpression(expression: string): string {
  const known: Record<string, string> = {
    "*/30 * * * *": "Every 30 minutes",
    "0 * * * *": "Every hour",
  };
  if (known[expression]) return known[expression];

  const parts = expression.split(" ");
  if (parts.length !== 5) return expression;
  const [minute, hour, dayOfMonth, , dayOfWeek] = parts;
  const time = `${hour.padStart(2, "0")}:${minute.padStart(2, "0")} UTC`;

  if (dayOfMonth !== "*" && dayOfWeek === "*")
    return `Monthly on day ${dayOfMonth} at ${time}`;
  if (dayOfWeek === "1-5") return `Weekdays at ${time}`;
  if (dayOfWeek !== "*") return `Weekly at ${time}`;
  if (hour !== "*" && dayOfMonth === "*" && dayOfWeek === "*")
    return `Daily at ${time}`;

  return expression;
}

export function isHeartbeatCron(expression: string): boolean {
  return expression === "*/30 * * * *";
}
