export const BACKEND_URL =
  process.env.NODE_ENV === "production"
    ? "https://api.harrokrog.com"
    : "http://localhost:5080";
