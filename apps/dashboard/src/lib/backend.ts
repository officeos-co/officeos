export const BACKEND_URL =
  process.env.NODE_ENV === "production"
    ? "https://api.officeos.co"
    : "http://localhost:5080";
