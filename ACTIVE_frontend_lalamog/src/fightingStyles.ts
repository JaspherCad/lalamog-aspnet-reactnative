// Five compact, stable wire keys fit the existing 50-character profile field (47 total).
// Keep this compatibility adapter in one place until the API gains a collection field.
export const fightingStyles = [
  { key: 'streetfighting', label: 'Streetfighting', icon: 'fist', description: 'An informal, self-described fighting background.' },
  { key: 'kickboxing', label: 'Kickboxing', icon: 'kick', description: 'Punches, kicks and a striking background.' },
  { key: 'bjj', label: 'Submission (BJJ)', icon: 'submission', description: 'Positions, ground grappling and submissions.' },
  { key: 'wrestling', label: 'Wrestling', icon: 'wrestling', description: 'Takedowns, clinches and control.' },
  { key: 'martial', label: 'Martial Artist', icon: 'martial', description: 'A broader martial-arts background. Tell your story in your bio.' },
] as const;
export type StyleKey = typeof fightingStyles[number]['key'];
const aliases: Record<string, StyleKey> = {
  streetfighter: 'streetfighting', 'street fighting': 'streetfighting', submissions: 'bjj',
  submission: 'bjj', 'submission (bjj)': 'bjj', submission_bjj: 'bjj',
  'martial artist': 'martial', 'martial arts': 'martial', martial_artist: 'martial',
};
export function parseStyles(value: string): { keys: StyleKey[]; legacy: string[] } {
  const keys: StyleKey[] = []; const legacy: string[] = [];
  for (const token of value.split(/[|,]/).map(s => s.trim()).filter(Boolean)) {
    const lower = token.toLowerCase();
    const key = fightingStyles.find(s => s.key === lower || s.label.toLowerCase() === lower)?.key ?? aliases[lower];
    if (key) { if (!keys.includes(key)) keys.push(key); } else legacy.push(token);
  }
  return { keys, legacy };
}
export function serializeStyles(keys: StyleKey[]) {
  return fightingStyles.filter(s => keys.includes(s.key)).map(s => s.key).join('|');
}
export function styleLabels(value: string) {
  const { keys, legacy } = parseStyles(value);
  return [...keys.map(key => fightingStyles.find(s => s.key === key)!.label), ...legacy].join(' · ') || 'Style not added';
}
