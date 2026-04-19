/**
 * Fix common SVG issues from backend-provided logos.
 * Strips malformed viewBox attributes that cause browser parsing errors.
 */
export function sanitizeSvg(svg: string): string {
  if (!svg) return ""
  // Unescape backslash-escaped quotes from double-encoded JSON in skill manifests
  let clean = svg.replace(/\\"/g, '"')
  // Remove viewBox attributes with invalid values (e.g. missing closing quote, non-numeric content)
  clean = clean.replace(/viewBox="([^"]*)"/g, (match, value: string) => {
    const parts = value.trim().split(/\s+/)
    if (parts.length === 4 && parts.every((p: string) => !isNaN(Number(p)))) {
      return match // valid viewBox
    }
    return "" // strip malformed viewBox
  })
  return clean
}
