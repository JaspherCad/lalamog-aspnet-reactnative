import { useEffect, useId, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import type { Profile } from './types';
import { fightingStyles, parseStyles } from './fightingStyles';

export type IconName = 'discover' | 'inbox' | 'map' | 'profile' | 'arrow' | 'heart' | 'x' | 'pin' | 'refresh' | 'trophy' | 'shield' | 'clock' | 'check' | 'fist' | 'kick' | 'submission' | 'wrestling' | 'martial' | 'calendar' | 'logout' | 'info';
// Original small SVG set. No remote assets, icon font or per-icon texture filters.
export function Icon({ name, className = '' }: { name: IconName; className?: string }) {
  const paths: Record<IconName, ReactNode> = {
    discover: <><path d="m19 5-4 10-10 4L9 9Z"/><path d="m9 9 6 6"/></>,
    inbox: <path d="M4 4h16v12H9l-5 4Z"/>,
    map: <><path d="m3 5 6-2 6 2 6-2v16l-6 2-6-2-6 2Z"/><path d="M9 3v16M15 5v16"/></>,
    profile: <><circle cx="12" cy="7" r="4"/><path d="M4 21v-2a8 8 0 0 1 16 0v2Z"/></>,
    arrow: <path d="M4 12h16m-6-6 6 6-6 6"/>, heart: <path d="M12 21 3.5 12A5.5 5.5 0 0 1 12 5a5.5 5.5 0 0 1 8.5 7Z"/>,
    x: <path d="m6 6 12 12M18 6 6 18"/>, pin: <><path d="M19 9c0 5-7 12-7 12S5 14 5 9a7 7 0 0 1 14 0Z"/><circle cx="12" cy="9" r="2"/></>,
    refresh: <><path d="M20 7v5h-5M4 17v-5h5"/><path d="M19 7a8 8 0 0 0-14-1M5 17a8 8 0 0 0 14 1"/></>,
    trophy: <><path d="M7 3h10v7a5 5 0 0 1-10 0ZM7 5H3v3a4 4 0 0 0 4 4m10-7h4v3a4 4 0 0 1-4 4M12 15v6m-4 0h8"/></>,
    shield: <><path d="m12 2 8 3v6c0 5-8 11-8 11S4 16 4 11V5Z"/><path d="m8 11 3 3 5-6"/></>,
    clock: <><circle cx="12" cy="12" r="9"/><path d="M12 6v6l4 3"/></>, check: <path d="m5 12 5 5L20 6"/>,
    calendar: <><path d="M3 5h18v16H3ZM7 2v6m10-6v6M3 10h18M7 14h3m4 0h3m-10 4h3"/></>,
    logout: <><path d="M10 3H4v18h6m4-15 6 6-6 6m-6-6h12"/></>, info: <><circle cx="12" cy="12" r="9"/><path d="M12 10v7m0-11v1"/></>,
    fist: <><path d="m5 14-2-4 2-5 3-1 2 2 2-3 3 1 1 3 3-1 2 3-2 7-5 5H7l-3-5Z" fill="currentColor" strokeWidth="1"/><path d="m7 8 3 4 4-2m-4 2-2 4" stroke="var(--icon-cutout, var(--paper))"/></>,
    kick: <><circle cx="9" cy="4" r="2" fill="currentColor"/><path d="m10 8-3 6-4 6m5-7 7 8m-5-11 10-5m-9 3-6 1-2-2" strokeWidth="3"/></>,
    submission: <><circle cx="7" cy="14" r="2" fill="currentColor"/><circle cx="17" cy="9" r="2" fill="currentColor"/><path d="m5 20 7-5 6 3 3-2m-11-2 4-5 5 4m-7 2 1 6m0-10-7-3" strokeWidth="3"/></>,
    wrestling: <><circle cx="7" cy="5" r="2" fill="currentColor"/><circle cx="17" cy="5" r="2" fill="currentColor"/><path d="m7 9 4 4-4 3-3 5m3-5 4 5m6-12-4 4 4 3 3 5m-3-5-4 5M7 9l7-1m3 1-7-1" strokeWidth="2.7"/></>,
    martial: <><circle cx="12" cy="4" r="2" fill="currentColor"/><path d="M12 8v6m0-4-6 2-3-4m9 2 6-2 3-4m-9 10-6 7m6-7 7 7" strokeWidth="3"/></>,
  };
  return <svg className={`icon ${className}`} width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.9" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">{paths[name]}</svg>;
}
export function fighterName(profile: Profile) { return profile.fighterName || profile.nickname; }
export function Avatar({ profile, large = false }: { profile: Profile; large?: boolean }) {
  const [failed, setFailed] = useState(false);
  useEffect(() => setFailed(false), [profile.avatarUrl]);
  return <div className={`avatar ${large ? 'avatar-large' : ''}`}>
    {profile.avatarUrl && !failed ? <img src={profile.avatarUrl} alt={`${fighterName(profile)}'s profile`} onError={() => setFailed(true)} draggable={false}/> : <span aria-label={`${fighterName(profile)}, no profile photo`}>{fighterName(profile).slice(0, 1).toUpperCase()}</span>}
  </div>;
}
export function PageHeading({ eyebrow, title, children }: { eyebrow: string; title: string; children?: ReactNode }) {
  return <div className="section-heading"><div><p className="eyebrow">{eyebrow}</p><h1>{title}</h1></div>{children}</div>;
}
export function StyleStack({ value }: { value: string }) {
  const { keys, legacy } = parseStyles(value);
  return <div className="style-stack" aria-label="Fighting styles">{keys.slice(0, 3).map(key => {
    const style = fightingStyles.find(s => s.key === key)!;
    return <div className={`style-badge style-${key}`} key={key}><span className="style-emblem"><Icon name={style.icon}/></span><span>{style.label}</span></div>;
  })}{legacy.slice(0, Math.max(0, 3 - keys.length)).map(label => <div className="style-badge" key={label}><span className="style-emblem"><Icon name="fist"/></span><span>{label}</span></div>)}
    {!keys.length && !legacy.length && <p className="muted">No styles added yet.</p>}</div>;
}
export interface FightRecord { wins: number; losses: number; draws: number }
export interface FighterStats {
  heightCm?: number;
  weightKg?: number;
  opponentRating?: { average: number; count: number };
}
export function RecordPanel({ record, stats }: { record?: FightRecord; stats?: FighterStats }) {
  const total = record ? record.wins + record.losses + record.draws : 0;
  const outcomes = ['wins', 'losses', 'draws'] as const;
  const rating = stats?.opponentRating;
  const hasRating = !!rating && rating.count > 0 && Number.isFinite(rating.average) && rating.average >= 0 && rating.average <= 5;
  const ratingPercent = hasRating ? rating.average / 5 * 100 : 0;
  return <section className="record-panel" aria-label="Public record and fighter stats"><p className="eyebrow">Public record</p>
    <div className="record-legend">{outcomes.map(key => <span className={`record-entry ${key}`} key={key}><span><i aria-hidden="true"/>{key}</span><strong>{record ? record[key] : '—'}</strong></span>)}</div>
    <div className="outcome-bar" role="img" aria-label={record ? `${record.wins} wins, ${record.losses} losses, ${record.draws} draws. ${total} recorded fights.` : 'Public fight record not available'}>{outcomes.map(key => <span className={`outcome-segment ${key}`} key={key} style={{ width: `${record && total ? record[key] / total * 100 : 0}%` }}/>)}</div>
    <small className="record-note">{record ? `${total} recorded · ${total ? `${Math.round(record.wins / total * 100)}% win rate` : 'No recorded fights'}` : 'Record not available yet'}</small>
    <dl className="fighter-measurements"><div><dt>Height</dt><dd>{stats?.heightCm ? <>{stats.heightCm}<small> cm</small></> : <span className="stat-unavailable">Not available</span>}</dd></div><div><dt>Weight</dt><dd>{stats?.weightKg ? <>{stats.weightKg}<small> kg</small></> : <span className="stat-unavailable">Not available</span>}</dd></div></dl>
    <div className="opponent-rating"><span className="rating-label">Opponent rating</span><div className="rating-score"><span className="rating-stars" aria-hidden="true">★★★★★<span style={{ width: `${ratingPercent}%` }}>★★★★★</span></span><strong>{hasRating ? `${Number(rating.average.toFixed(1))} / 5` : 'Not rated'}</strong></div>
      {hasRating ? <div className="rating-track" role="meter" aria-label="Average rating from opponents" aria-valuemin={0} aria-valuemax={5} aria-valuenow={rating.average} aria-valuetext={`${rating.average} out of 5 from ${rating.count} opponent reviews. Green shows the score; red shows the remainder of the five-point scale.`}><span style={{ width: `${ratingPercent}%` }}/></div> : <div className="rating-track rating-unavailable" aria-hidden="true"/>}
      <small className="record-note">{hasRating ? `${rating.count} opponent ${rating.count === 1 ? 'review' : 'reviews'}` : 'No opponent rating available'}</small></div>
  </section>;
}
export function Modal({ title, onClose, children, className = '' }: { title: string; onClose: () => void; children: ReactNode; className?: string }) {
  const ref = useRef<HTMLDialogElement>(null); const titleId = useId();
  useEffect(() => { const dialog = ref.current!; dialog.showModal(); return () => dialog.close(); }, []);
  return <dialog ref={ref} className={`paper-dialog ${className}`} aria-labelledby={titleId} onClose={() => { if (!ref.current?.open) onClose(); }} onClick={event => { if (event.target === ref.current) onClose(); }}>
    <div className="dialog-heading"><h2 id={titleId}>{title}</h2><button className="icon-button secondary" aria-label="Close dialog" onClick={onClose}><Icon name="x"/></button></div>{children}</dialog>;
}
