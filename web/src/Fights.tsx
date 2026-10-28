import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { api, errorText, post } from './api';
import type { Fight, Match, Session } from './types';

function SessionCard({ fight, match, session, update }: { fight: Fight; match?: Match; session: Session; update: (fight: Fight) => void }) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [canceling, setCanceling] = useState(false);
  const [accepted, setAccepted] = useState(false);
  const confirmed = fight.user1Id === session.userId ? fight.user1Confirmed : fight.user2Confirmed;
  const upcoming = ['scheduled', 'confirmed'].includes(fight.status);
  async function perform(path: string, data: unknown = {}) {
    setBusy(true); setError('');
    try { update(await post<Fight>(`/fights/${fight.id}/${path}`, data)); setCanceling(false); }
    catch (err) { setError(errorText(err)); } finally { setBusy(false); }
  }
  function confirm(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = Object.fromEntries(new FormData(event.currentTarget));
    void perform('confirm', { ...data, accepted });
  }
  return <article className="panel session-card"><div className="section-heading"><h2>With {match?.opponent.nickname || 'your match'}</h2><span className="pill">{fight.status}</span></div>
    <p><time dateTime={fight.scheduledAt}>{new Date(fight.scheduledAt).toLocaleString()}</time></p>
    <p><strong>{fight.location}</strong><br /><span className="muted">{fight.address}</span></p>
    {error && <p role="alert" className="error">{error}</p>}
    {upcoming && <><p className="small">You: {confirmed ? 'waiver submitted' : 'not confirmed'} · Partner: {(fight.user1Id === session.userId ? fight.user2Confirmed : fight.user1Confirmed) ? 'waiver submitted' : 'not confirmed'}</p>
      {!confirmed && new Date(fight.scheduledAt).getTime() > Date.now() && <form className="confirm-box" onSubmit={confirm}><fieldset disabled={busy}>
        <h3>Confirm your session</h3>
        <label>Emergency contact name<input name="emergencyContactName" maxLength={100} autoComplete="off" required /></label>
        <label>Emergency contact phone<input name="emergencyContactPhone" type="tel" maxLength={40} autoComplete="off" required /></label>
        <label className="checkbox"><input type="checkbox" checked={accepted} onChange={e => setAccepted(e.target.checked)} required />I accept the risks of sparring and have agreed on the venue and training rules with my partner.</label>
        <p className="small muted">Emergency contacts are visible only to you and your matched partner. Both participants must submit before the session is confirmed.</p>
        <button disabled={!accepted}>{busy ? 'Saving...' : 'Submit waiver & confirm'}</button>
      </fieldset></form>}
      <div className="actions"><button className="secondary" disabled={busy} onClick={() => setCanceling(!canceling)}>Cancel session</button>
        {fight.status === 'confirmed' && new Date(fight.scheduledAt).getTime() <= Date.now() &&
          <button disabled={busy} onClick={() => void perform('complete')}>Mark session complete</button>}</div></>}
    {canceling && <form onSubmit={event => {
      event.preventDefault(); void perform('cancel', Object.fromEntries(new FormData(event.currentTarget)));
    }}><fieldset disabled={busy}><label>Cancellation reason<textarea name="reason" maxLength={500} required /></label><div className="actions"><button>{busy ? 'Saving...' : 'Save cancellation'}</button><button className="secondary" type="button" onClick={() => setCanceling(false)}>Back</button></div></fieldset></form>}
    {fight.confirmations.length > 0 && <details><summary>Emergency contacts</summary>{fight.confirmations.map(contact =>
      <p className="small" key={contact.userId}><strong>{contact.userId === session.userId ? 'Your contact' : "Partner's contact"}:</strong> {contact.emergencyContactName} · {contact.emergencyContactPhone}</p>)}</details>}
    {fight.status === 'completed' && <p className="notice">Session completed.</p>}
    {fight.cancellationReason && <p>Reason: {fight.cancellationReason}</p>}
  </article>;
}
export function Fights({ session }: { session: Session }) {
  const [fights, setFights] = useState<Fight[]>([]);
  const [matches, setMatches] = useState<Match[]>([]);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const [notice, setNotice] = useState('');
  async function load() {
    setLoading(true); setError('');
    try {
      const [f, m] = await Promise.all([api<Fight[]>('/fights'), api<Match[]>('/matches')]); setFights(f); setMatches(m);
    } catch (err) { setError(errorText(err)); } finally { setLoading(false); }
  }
  useEffect(() => { void load(); }, []);
  async function schedule(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); const form = event.currentTarget;
    const data = Object.fromEntries(new FormData(form));
    setBusy(true); setError(''); setNotice('');
    try {
      const fight = await post<Fight>('/fights', { matchId: Number(data.matchId), location: data.location, address: data.address,
        scheduledAt: new Date(data.scheduledAt as string).toISOString() });
      setFights(current => [fight, ...current]); form.reset(); setNotice('Session proposed. Both partners need to submit their waivers.');
    } catch (err) { setError(errorText(err)); } finally { setBusy(false); }
  }
  function update(value: Fight) { setFights(current => current.map(f => f.id === value.id ? value : f)); }
  return <section><div className="section-heading"><div><p className="eyebrow">PUT IN THE ROUNDS</p><h1>Training sessions</h1></div><button className="secondary" disabled={busy || loading} onClick={() => void load()}>Refresh</button></div>
    {error && <p role="alert" className="error">{error}</p>}{notice && <p role="status" className="notice">{notice}</p>}
    {loading ? <p role="status">Loading sessions...</p> : <>
      {matches.length > 0 && <form className="panel schedule-form" onSubmit={schedule}><h2>Plan a session</h2><fieldset disabled={busy}>
        <label>Partner<select name="matchId" aria-label="Partner" required>{matches.map(m => <option key={m.id} value={m.id}>{m.opponent.nickname}</option>)}</select></label>
        <label>Date & time<input name="scheduledAt" aria-label="Date & time" aria-describedby="session-time-hint" type="datetime-local" required /><small id="session-time-hint" className="muted">Your local time ({Intl.DateTimeFormat().resolvedOptions().timeZone}).</small></label>
        <label>Venue name<input name="location" maxLength={200} placeholder="Gym name" required /></label>
        <label>Venue address<input name="address" maxLength={300} placeholder="Street, city" required /></label>
        <button>{busy ? 'Scheduling...' : 'Propose session'}</button>
      </fieldset></form>}
      <div className="session-list">{fights.map(f => <SessionCard key={f.id} fight={f} match={matches.find(m => m.id === f.matchId)} session={session} update={update} />)}</div>
      {!fights.length && !error && <div className="panel empty"><h2>Your next round is waiting</h2><p>{matches.length ? 'Choose a partner and propose a session above.' : 'Match with a fighter first, then plan a session together.'}</p></div>}
    </>}
  </section>;
}
