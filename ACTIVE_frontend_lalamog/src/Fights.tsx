import { price } from './Gyms';
import { LocationPicker } from './LocationPicker';
import type { MapLocation } from './types';
import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { api, errorText, post } from './api';
import type { Fight, Match, Session, Gym } from './types';
import { fighterName, Icon, PageHeading } from './ui';

function SessionCard({ fight, match, session, update }: { fight: Fight; match?: Match; session: Session; update: (fight: Fight) => void }) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [canceling, setCanceling] = useState(false);
  const [allowPublicIdentity, setAllowPublicIdentity] = useState(false);
  const [accepted, setAccepted] = useState(false);
  const confirmed = fight.user1Id === session.userId ? fight.user1Confirmed : fight.user2Confirmed;
  const upcoming = ['pending_approval', 'scheduled', 'confirmed'].includes(fight.status);
  const ownConsent = fight.confirmations.find(c => c.userId === session.userId)?.allowPublicIdentity ?? false;
  async function perform(path: string, data: unknown = {}) {
    setBusy(true); setError('');
    try { update(await post<Fight>(`/fights/${fight.id}/${path}`, data)); setCanceling(false); }
    catch (err) { setError(errorText(err)); } finally { setBusy(false); }
  }
  function confirm(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = Object.fromEntries(new FormData(event.currentTarget));
    void perform('confirm', { ...data, accepted, allowPublicIdentity });
  }
  return <article className="panel session-card"><div className="section-heading"><h2>With {match ? fighterName(match.opponent) : 'your match'}</h2><span className="pill">{fight.status}</span></div>
    <p><time dateTime={fight.scheduledAt}>{new Date(fight.scheduledAt).toLocaleString()}</time></p>
    <p><strong>{fight.location}</strong><br /><span className="muted">{fight.address}</span></p>
    {fight.mapLocation?.latitude != null && <Link className="text-action" to={fight.gymId != null ? `/gyms/${fight.gymId}` : `/map?fightId=${fight.id}`}>View venue map</Link>}
    {error && <p role="alert" className="error">{error}</p>}
    {fight.gymId != null && <p>{price(fight.quotedPrice ?? 0)} · {fight.sessionDurationMinutes} minutes · Pay at gym</p>}
    {fight.status === 'pending_approval' && <p className="notice">Waiting for the gym owner to approve your request.</p>}
    {fight.gymId != null && confirmed && <label className="checkbox"><input type="checkbox" disabled={busy} checked={ownConsent} onChange={event => void perform('publication', { allowPublicIdentity: event.target.checked })}/>Allow my public name and avatar in this gym’s completed fight history. Both fighters must agree; you can withdraw later.</label>}
    {upcoming && <><p className="small">You: {confirmed ? 'waiver submitted' : 'not confirmed'} · Partner: {(fight.user1Id === session.userId ? fight.user2Confirmed : fight.user1Confirmed) ? 'waiver submitted' : 'not confirmed'}</p>
      {fight.status !== 'pending_approval' && !confirmed && new Date(fight.scheduledAt).getTime() > Date.now() && <form className="confirm-box" onSubmit={confirm}><fieldset disabled={busy}>
        <h3>Confirm your fight</h3>
        <label>Emergency contact name<input name="emergencyContactName" maxLength={100} autoComplete="off" required /></label>
        <label>Emergency contact phone<input name="emergencyContactPhone" type="tel" maxLength={40} autoComplete="off" required /></label>
        <label className="checkbox"><input type="checkbox" checked={accepted} onChange={e => setAccepted(e.target.checked)} required />I accept the risks of sparring and have agreed on the venue and training rules with my partner.</label>
        <p className="small muted">Emergency contacts are visible only to you and your matched partner. Both participants must submit before the fight is confirmed.</p>
        {fight.gymId != null && <label className="checkbox"><input type="checkbox" checked={allowPublicIdentity} onChange={e => setAllowPublicIdentity(e.target.checked)}/>Allow my public name and avatar in this gym’s completed fight history (optional).</label>}
        <button disabled={!accepted}>{busy ? 'Saving...' : 'Submit waiver & confirm'}</button>
      </fieldset></form>}
      <div className="actions"><button className="secondary" disabled={busy} onClick={() => setCanceling(!canceling)}>Cancel fight</button>
        {fight.gymId == null && fight.status === 'confirmed' && new Date(fight.scheduledAt).getTime() <= Date.now() &&
          <button disabled={busy} onClick={() => void perform('complete')}>Mark fight complete</button>}</div></>}
    {canceling && <form onSubmit={event => {
      event.preventDefault(); void perform('cancel', Object.fromEntries(new FormData(event.currentTarget)));
    }}><fieldset disabled={busy}><label>Cancellation reason<textarea name="reason" maxLength={500} required /></label><div className="actions"><button>{busy ? 'Saving...' : 'Save cancellation'}</button><button className="secondary" type="button" onClick={() => setCanceling(false)}>Back</button></div></fieldset></form>}
    {fight.confirmations.length > 0 && <details><summary>Emergency contacts</summary>{fight.confirmations.map(contact =>
      <p className="small" key={contact.userId}><strong>{contact.userId === session.userId ? 'Your contact' : "Partner's contact"}:</strong> {contact.emergencyContactName} · {contact.emergencyContactPhone}</p>)}</details>}
    {fight.status === 'completed' && <p className="notice">Fight completed. Result not recorded.</p>}
    {fight.cancellationReason && <p>Reason: {fight.cancellationReason}</p>}
    <Link className="text-action" to={`/matches/${fight.matchId}`}>Open conversation <Icon name="arrow"/></Link>
  </article>;
}
export function Fights({ session }: { session: Session }) {
  const [params] = useSearchParams();
  const [mapLocation, setMapLocation] = useState<MapLocation>({});
  const [venueAddress, setVenueAddress] = useState('');
  const contextualMatchId = Number(params.get('matchId')) || null;
  const [fights, setFights] = useState<Fight[]>([]);
  const [gyms, setGyms] = useState<Gym[]>([]);
  const [mode, setMode] = useState<'private' | 'gym'>(params.has('gymId') ? 'gym' : 'private');
  const [gymId, setGymId] = useState(params.get('gymId') || '');
  const [matches, setMatches] = useState<Match[]>([]);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const [notice, setNotice] = useState('');
  async function load() {
    setLoading(true); setError('');
    try {
      const [f, m, g] = await Promise.all([api<Fight[]>('/fights'), api<Match[]>('/matches'), api<Gym[]>('/gyms')]); setFights(f); setMatches(m); setGyms(g);
    } catch (err) { setError(errorText(err)); } finally { setLoading(false); }
  }
  useEffect(() => { void load(); }, []);
  async function schedule(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); const form = event.currentTarget;
    const data = Object.fromEntries(new FormData(form));
    setBusy(true); setError(''); setNotice('');
    try {
      const gym = gyms.find(g => String(g.id) === gymId);
      const venue = mode === 'gym' ? { gymId: gym?.id, gymVersion: gym?.version } : { mapLocation, location: data.location, address: data.address };
      if (mode === 'gym' && !gym) throw new Error('Choose a registered gym.');
      const fight = await post<Fight>('/fights', { matchId: Number(data.matchId), ...venue, scheduledAt: new Date(data.scheduledAt as string).toISOString() });
      setFights(current => [fight, ...current]); form.reset(); setMapLocation({}); setVenueAddress(''); setNotice(fight.status === 'pending_approval' ? 'Gym booking requested. Wait for the owner’s approval before submitting waivers.' : 'Fight arranged. Both participants need to submit their confirmations.');
    } catch (err) { setError(errorText(err)); } finally { setBusy(false); }
  }
  function update(value: Fight) { setFights(current => current.map(f => f.id === value.id ? value : f)); }
  const visibleFights = contextualMatchId ? fights.filter(f => f.matchId === contextualMatchId) : fights;
  const availableMatches = matches.filter(m => (!contextualMatchId || m.id === contextualMatchId) && !fights.some(f => f.matchId === m.id && ['pending_approval', 'scheduled', 'confirmed'].includes(f.status)));
  return <section><PageHeading eyebrow="One connection. Many rounds." title="Your fights"><button className="secondary" disabled={busy || loading} onClick={() => void load()}><Icon name="refresh"/>Refresh</button></PageHeading>
    {contextualMatchId && <Link className="text-action back-link" to={`/matches/${contextualMatchId}`}>← Back to conversation</Link>}
    {error && <p role="alert" className="error">{error}</p>}{notice && <p role="status" className="notice">{notice}</p>}
    {loading ? <p role="status">Loading fights...</p> : <>
      {availableMatches.length > 0 && <form className="panel schedule-form" onSubmit={schedule}><span className="corner-label">Arrange your next round</span><h2>Place. Time. It’s a plan.</h2><fieldset disabled={busy}>
        <label>Partner<select name="matchId" aria-label="Partner" required>{availableMatches.map(m => <option key={m.id} value={m.id}>{fighterName(m.opponent)}{m.opponent.fighterName ? ` · ${m.opponent.nickname}` : ''}</option>)}</select></label>
        <label>Date & time<input name="scheduledAt" aria-label="Date & time" aria-describedby="session-time-hint" type="datetime-local" required /><small id="session-time-hint" className="muted">Your local time ({Intl.DateTimeFormat().resolvedOptions().timeZone}).</small></label>
        <label>Fight location<select aria-label="Fight location" value={mode} onChange={e=>setMode(e.target.value as 'private'|'gym')}><option value="private">Private fight — choose your location</option><option value="gym">Registered gym — request a session</option></select></label>
        {mode === 'gym' ? <div className="gym-selection"><label>Registered gym<select aria-label="Registered gym" value={gymId} onChange={e=>setGymId(e.target.value)} required><option value="">Choose a gym</option>{gyms.map(g=><option key={g.id} value={g.id}>{g.name}</option>)}</select></label>{gyms.filter(g=>String(g.id)===gymId).map(g=><div className="notice" key={g.id}><strong>{g.name}</strong><p>{g.address}</p><p>{price(g.sessionPrice)} for both fighters · {g.sessionDurationMinutes} minutes</p><p>Pay at gym. Owner approval required.</p><Link to={`/gyms/${g.id}`}>View gym details</Link></div>)}{!gyms.length&&<p>No gyms are available. You can arrange a private fight.</p>}</div> : <>
        <label>Venue name<input name="location" maxLength={200} placeholder="Choose a meeting place" required /></label>
        <label>Venue address<input name="address" value={venueAddress} onChange={event => setVenueAddress(event.target.value)} maxLength={300} placeholder="Street, city" required /></label>
        <LocationPicker value={mapLocation} onChange={setMapLocation} purpose="venue" onAddressSelected={setVenueAddress}/></>}
        <button>{busy ? 'Arranging...' : 'Arrange fight'}<Icon name="arrow"/></button>
      </fieldset></form>}
      <div className="session-list">{visibleFights.map(f => <SessionCard key={f.id} fight={f} match={matches.find(m => m.id === f.matchId)} session={session} update={update} />)}</div>
      {!visibleFights.length && !error && <div className="panel empty"><h2>Your next round is waiting</h2><p>{availableMatches.length ? 'Choose a place and time above.' : 'Open a matched conversation to arrange your next fight.'}</p><Link className="text-action" to="/matches">Open Inbox <Icon name="arrow"/></Link></div>}
    </>}
  </section>;
}
