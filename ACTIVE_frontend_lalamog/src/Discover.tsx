import { useEffect, useRef, useState } from 'react';
import type { PointerEvent as ReactPointerEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api, errorText, post } from './api';
import type { Match, Profile } from './types';
import { Avatar, fighterName, Icon, Modal, PageHeading, RecordPanel, StyleStack } from './ui';
import demoPortrait from './assets/demo-fighter.jpg';

const demoProfiles: Profile[] = [
  { userId: 'demo-miko', fighterName: 'Miko', nickname: 'Crow', bio: 'Here for a good story and another round. Let’s make it count.', fightingStyle: 'streetfighting|kickboxing|wrestling', experienceLevel: 2, location: 'Rizal', availability: 'Weekends', avatarUrl: demoPortrait },
  { userId: 'demo-jae', fighterName: 'Jae', nickname: 'Grim Reaper', bio: 'New faces. New stories. Same fighting spirit.', fightingStyle: 'bjj|martial', experienceLevel: 1, location: 'Quezon City', availability: 'Evenings', avatarUrl: null },
];
export function Discover({ preview = false }: { preview?: boolean }) {
  const [profiles, setProfiles] = useState<Profile[]>([]);
  const [loading, setLoading] = useState(true); const [busy, setBusy] = useState(false);
  const [error, setError] = useState(''); const [lastChoice, setLastChoice] = useState<'like' | 'pass'>('like');
  const [notice, setNotice] = useState(''); const [intel, setIntel] = useState(false);
  const [hoverIntel, setHoverIntel] = useState(false);
  const [match, setMatch] = useState<{ profile: Profile; id: number | null } | null>(null);
  const [dragX, setDragX] = useState(0); const [dragging, setDragging] = useState(false);
  const lock = useRef(false); const generation = useRef(0);
  const gesture = useRef<{ x: number; y: number; dx: number; time: number; intent: 'pending' | 'horizontal' | 'vertical' } | null>(null);
  const navigate = useNavigate(); const profile = profiles[0];
  // These are view-only sample values, never a fallback for an actual account.
  const sampleRecord = preview && profile?.userId === 'demo-miko' ? { wins: 8, losses: 2, draws: 1 } : undefined;
  const sampleStats = sampleRecord ? { heightCm: 178, weightKg: 76, opponentRating: { average: 4, count: 8 } } : undefined;
  async function load() {
    const request = ++generation.current; setLoading(true); setError(''); setNotice('');
    try {
      const values = preview ? demoProfiles : await api<Profile[]>('/profiles/discover');
      if (request === generation.current) setProfiles(values);
    } catch (err) { if (request === generation.current) setError(errorText(err)); }
    finally { if (request === generation.current) setLoading(false); }
  }
  useEffect(() => { void load(); return () => { generation.current++; }; }, [preview]);
  async function swipe(direction: 'like' | 'pass') {
    if (!profile || lock.current) return;
    lock.current = true; setBusy(true); setLastChoice(direction); setError(''); setNotice(''); setHoverIntel(false);
    const request = generation.current;
    try {
      const result = preview ? { matched: direction === 'like', matchId: null } : await post<{ matched: boolean; matchId: number | null }>('/matches/swipes', { targetId: profile.userId, direction });
      if (request !== generation.current) return;
      setProfiles(current => current.filter(p => p.userId !== profile.userId));
      setNotice(result.matched ? `You and ${fighterName(profile)} matched.` : `${direction === 'like' ? 'Liked' : 'Passed'} ${fighterName(profile)}.`);
      if (result.matched) setMatch({ profile, id: result.matchId });
    } catch (err) { if (request === generation.current) setError(errorText(err)); }
    finally { lock.current = false; setBusy(false); setDragX(0); }
  }
  function start(event: ReactPointerEvent<HTMLElement>) {
    if (busy || intel || event.button !== 0 || (event.target as HTMLElement).closest('button,a,input,summary')) return;
    setHoverIntel(false); gesture.current = { x: event.clientX, y: event.clientY, dx: 0, time: performance.now(), intent: 'pending' };
    event.currentTarget.setPointerCapture(event.pointerId);
  }
  function move(event: ReactPointerEvent<HTMLElement>) {
    const g = gesture.current; if (!g) return;
    const dx = event.clientX - g.x; const dy = event.clientY - g.y;
    if (g.intent === 'pending' && Math.hypot(dx, dy) > 10) g.intent = Math.abs(dx) > Math.abs(dy) * 1.25 ? 'horizontal' : 'vertical';
    if (g.intent === 'horizontal') { g.dx = dx; setDragging(true); setDragX(Math.max(-150, Math.min(150, dx))); }
  }
  function cancel() { gesture.current = null; setDragging(false); setDragX(0); }
  function end(event: ReactPointerEvent<HTMLElement>) {
    const g = gesture.current; gesture.current = null; setDragging(false);
    if (g?.intent === 'horizontal') {
      const distance = Math.abs(g.dx); const velocity = distance / Math.max(1, performance.now() - g.time);
      if (distance >= Math.min(120, event.currentTarget.clientWidth * .28) || (distance >= 48 && velocity > .65)) { void swipe(g.dx > 0 ? 'like' : 'pass'); return; }
    }
    setDragX(0);
  }
  async function openConversation() {
    if (!match) return;
    if (preview) { setMatch(null); setNotice('Preview only. Sign in to start a real conversation.'); return; }
    try {
      const id = match.id ?? (await api<Match[]>('/matches')).find(m => m.opponent.userId === match.profile.userId)?.id;
      if (!id) throw new Error('Your match is saved. Open Inbox or try again to find the conversation.');
      navigate(`/matches/${id}`);
    } catch (err) { setError(errorText(err)); setMatch(null); }
  }
  return <section className="discover-page">
    <PageHeading eyebrow="The next round starts with a connection" title="Discover"><button className="secondary refresh-button" disabled={busy || loading} onClick={() => void load()}><Icon name="refresh"/><span>Refresh</span></button></PageHeading>
    <div className="discover-intro"><p>Choose your next match<span className="red-dot">.</span></p><span className="keyboard-hint">← Pass <span>/</span> Like →</span></div>
    {preview && <p className="preview-banner"><Icon name="info"/>Design preview · Fictional profiles and sample records. Choices aren’t saved. <Link to="/">Sign in</Link></p>}
    {notice && <p className="choice-notice" role="status">{notice}</p>}
    {error && <div className="error" role="alert"><p>{error}</p><button className="secondary" disabled={busy} onClick={() => profile ? void swipe(lastChoice) : void load()}>Retry {profile ? 'choice' : 'loading'}</button></div>}
    {loading ? <div className="panel dossier-loading" role="status"><Icon name="fist"/><h2>Finding your next match…</h2><p>Getting the dossiers ready.</p></div> : profile ? <>
      <div className="deck"><span className="deck-sheet sheet-two" aria-hidden="true"/><span className="deck-sheet sheet-one" aria-hidden="true"/>
        <article className={`fighter-dossier ${dragging ? 'is-dragging' : ''} ${busy ? 'is-saving' : ''}`} data-testid="swipe-card" tabIndex={0} aria-label={`Fighter: ${fighterName(profile)}. Use left to pass, right to like.`}
          style={{ transform: `translateX(${dragX}px) rotate(${dragX / 22}deg)` }} onPointerDown={start} onPointerMove={move} onPointerUp={end} onPointerCancel={cancel} onLostPointerCapture={() => { if (gesture.current) cancel(); }} onKeyDown={event => {
            if (event.target !== event.currentTarget) return;
            if (event.key === 'Escape') cancel();
            if (['ArrowLeft', 'ArrowRight'].includes(event.key)) { event.preventDefault(); void swipe(event.key === 'ArrowRight' ? 'like' : 'pass'); }
          }}>
          {Math.abs(dragX) > 20 && <span className={`swipe-stamp ${dragX > 0 ? 'stamp-like' : 'stamp-pass'}`} aria-hidden="true">{dragX > 0 ? 'LIKE' : 'PASS'}</span>}
          <header className="dossier-heading fighter-details"><div><h2 className="fighter-name">{fighterName(profile)}</h2>{profile.fighterName && profile.nickname && <p className="fighter-alias">“{profile.nickname}”</p>}</div><span className="location-label"><Icon name="pin"/>{profile.location || 'Location not added'}</span></header>
          <div className="dossier-content"><div className="portrait-panel" onMouseLeave={() => setHoverIntel(false)}><Avatar profile={profile} large/><button className="intel-button" onMouseEnter={() => { if (matchMedia('(hover: hover)').matches) setHoverIntel(true); }} onFocus={() => setHoverIntel(true)} onBlur={() => setHoverIntel(false)} onKeyDown={event => { if (event.key === 'Escape') { setHoverIntel(false); event.stopPropagation(); } }} onClick={() => { setHoverIntel(false); setIntel(true); }}><Icon name="info"/>Fight intel</button>
            {hoverIntel && !dragging && <aside className="intel-hover"><strong>Public Lalamog record</strong><p>{preview && profile.userId === 'demo-miko' ? '8 wins · 2 losses · 1 draw' : 'Results and verified titles are not available yet.'}</p><small>Tap for the full profile.</small></aside>}
            <span className="portrait-caption">{preview ? 'Fictional demo fighter' : 'Meet the person. Make the connection.'}</span></div>
            <div className="dossier-info"><div className="fighter-tags">{preview ? <><span className="fighter-tag title-tag">Weekly fighter</span><span className="fighter-tag">Early member</span></> : <span className="fighter-tag"><Icon name="fist"/>{['', 'Beginner', 'Intermediate', 'Experienced'][profile.experienceLevel] || 'Fighter'}</span>}</div>
              <StyleStack value={profile.fightingStyle}/><RecordPanel record={sampleRecord} stats={sampleStats}/>
              <div className="fighter-facts"><span><Icon name="profile"/>{['', 'Beginner', 'Intermediate', 'Experienced'][profile.experienceLevel] || 'Experience not added'}</span><span><Icon name="calendar"/>{profile.availability || 'Ask in chat'}</span></div>
              <p className="fighter-bio">“{profile.bio || 'Every connection has a story. Say hello when you match.'}”</p>
              <button className="text-action" onClick={() => setIntel(true)}>View profile & history <Icon name="arrow"/></button>
            </div></div>
          <footer className="dossier-footer"><span><span className="small-square"/> {preview ? 'Sample record · Demo data' : 'Mutual likes unlock your private conversation'}</span><span>NO. {String(profiles.length).padStart(2, '0')}</span></footer>
        </article></div>
      <div className="swipe-actions" aria-label="Swipe actions"><button className="pass-button secondary" disabled={busy} onClick={() => void swipe('pass')}><Icon name="x"/>Pass</button><button className="like-button" disabled={busy} onClick={() => void swipe('like')}><Icon name="heart"/>{busy ? 'Saving…' : 'Like'}</button></div>
      <p className="swipe-caption">A like is a hello. A mutual like is a match.</p>
    </> : !error && <div className="panel empty"><Icon name="discover"/><p className="eyebrow">End of the stack</p><h2>You’re all caught up.</h2><p>Come back for another round. Your next connection could be just around the corner.</p><button className="secondary" onClick={() => void load()}>Check again</button></div>}
    {intel && profile && <Modal title={`${fighterName(profile)} · Fight intel`} onClose={() => setIntel(false)}><p className="fighter-alias">{profile.fighterName ? `“${profile.nickname}”` : ''}</p><StyleStack value={profile.fightingStyle}/><p>{profile.bio || 'No bio added yet.'}</p><dl className="intel-facts"><dt>Based in</dt><dd>{profile.location || 'Not added'}</dd><dt>Available</dt><dd>{profile.availability || 'Ask in chat'}</dd></dl><RecordPanel record={sampleRecord} stats={sampleStats}/><p className="muted small">{preview ? 'Sample record, measurements and opponent reviews for this fictional preview only.' : 'Public results, measurements, opponent reviews and verified titles aren’t available yet. No record or credentials are assumed.'}</p><button className="secondary" onClick={() => setIntel(false)}>Back to swiping</button></Modal>}
    {match && <Modal title="It’s a match" onClose={() => setMatch(null)}><div className="match-portraits"><span className="self-portrait"><Icon name="profile"/><small>You</small></span><Icon name="x"/><Avatar profile={match.profile}/></div><p className="match-reveal-copy">You and <strong>{fighterName(match.profile)}</strong> liked each other.</p><div className="dialog-actions"><button onClick={() => void openConversation()}>Open conversation <Icon name="arrow"/></button><button className="secondary" onClick={() => setMatch(null)}>Keep swiping</button></div></Modal>}
  </section>;
}
