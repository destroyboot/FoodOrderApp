export function tokenizeSearch(input: string): string[] {
  return input
    .trim()
    .toLowerCase()
    .split(/\s+/)
    .filter(Boolean);
}

export function matchesTokenizedSearch(haystack: string, query: string): boolean {
  const terms = tokenizeSearch(query);
  if (terms.length === 0) return true;

  const normalizedHaystack = haystack.toLowerCase();
  return terms.every((term) => normalizedHaystack.includes(term));
}
