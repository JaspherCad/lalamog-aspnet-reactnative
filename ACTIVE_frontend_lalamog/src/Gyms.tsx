import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api, errorText } from './api';
import { PageHeading } from './ui';
import type { Gym, Session } from './types';

export function price(value: number) { return new Intl.NumberFormat('en-PH', { style: 'currency', currency: 'PHP' }).format(value); }
export function GymCover({ gym }: { gym: Gym }) {
  const [failed, setFailed] = useState(false);
  useEffect(() => setFailed(false), [gym.coverUrl]);
  return failed || !gym.coverUrl ? <div className="gym-cover-placeholder"><span>YOUR NEXT ROUND</span><strong>{gym.name}</strong><small>Gym photo coming soon</small></div> : <img className="gym-cover" src={gym.coverUrl} alt={`${gym.name} gym`} onError={() => setFailed(true)}/>;
}
export function GymHistory({ gym }: { gym: Gym }) {
  return <section><h2>Fight history</h2>{!gym.fights.length ? <p>No completed fights yet.</p> : <ul className="gym-fight-list">{gym.fights.map(fight => <li key={fight.id}><strong>{fight.fighter1Name && fight.fighter2Name ? `${fight.fighter1Name} vs ${fight.fighter2Name}` : 'Anonymous fight'}</strong>
    {fight.fighter1Name && <div className="history-portraits">{[fight.fighter1AvatarUrl, fight.fighter2AvatarUrl].map((url, i) => url && <img key={i} src={url} alt="" onError={event => { event.currentTarget.style.display = 'none'; }}/>)}</div>}
    <span>{new Date(fight.scheduledAt).toLocaleString()}</span><span>{fight.status}</span></li>)}</ul>}</section>;
}
export function Gyms() {
  const [gyms, setGyms] = useState<Gym[]>([]); const [error, setError] = useState(''); const [loading, setLoading] = useState(true); const [reload, setReload] = useState(0);
  useEffect(() => { const c = new AbortController(); setLoading(true); setError(''); void api<Gym[]>('/gyms', { signal: c.signal }).then(setGyms).catch(e => { if (!c.signal.aborted) setError(errorText(e)); }).finally(() => { if (!c.signal.aborted) setLoading(false); }); return () => c.abort(); }, [reload]);
  return <section><PageHeading eyebrow="Find a place for your next round" title="Gyms open for fights"><Link className="button-link secondary" to="/map">View map</Link></PageHeading><p>Find a gym, connect with a matched fighter, and request a session. Pay at the gym.</p>
    {error && <div role="alert" className="error">{error}<button onClick={() => setReload(v => v + 1)}>Retry gyms</button></div>}{loading ? <p role="status">Loading gyms…</p> : <div className="gym-grid">{gyms.map(gym => <article className="panel gym-card" key={gym.id}><GymCover gym={gym}/><div><h2><Link to={`/gyms/${gym.id}`}>{gym.name}</Link></h2><p>{gym.address}</p><strong>{price(gym.sessionPrice)} / {gym.sessionDurationMinutes} min</strong><p className="small muted">For both fighters · Pay at gym{gym.isDemo ? ' · Fictional demo gym' : ''}</p><Link className="button-link" to={`/gyms/${gym.id}`}>View gym & book</Link></div></article>)}{!gyms.length && <p>No published gyms yet.</p>}</div>}
  </section>;
}
export function GymDetailsPage({ session }: { session: Session | null }) {
  const { id } = useParams(); const [gym, setGym] = useState<Gym | null>(null); const [error, setError] = useState(''); const [reload, setReload] = useState(0);
  useEffect(() => { const c = new AbortController(); setError(''); void api<Gym>(`/gyms/${id}`, { signal: c.signal }).then(setGym).catch(e => { if (!c.signal.aborted) setError(errorText(e)); }); return () => c.abort(); }, [id, reload]);
  if (error) return <div role="alert" className="panel error">{error}<button onClick={() => setReload(v => v + 1)}>Retry gym details</button></div>;
  if (!gym) return <p role="status">Loading gym details…</p>;
  const fighter = session?.permissions?.includes('user_canSchedule');
  return <section><Link className="text-action" to="/gyms">← All gyms</Link><PageHeading eyebrow="A place for your next story" title={gym.name}/><div className="gym-details-layout"><div><GymCover gym={gym}/><article className="panel"><p>{gym.description || 'This gym welcomes matched fighters for booked sessions.'}</p><p>{gym.address}</p><p className="small muted">{gym.areaPath.map(a => a.name).join(' › ')}</p><p>{gym.contactInformation}</p>{gym.isDemo && <p className="notice">Fictional demo gym. This is a test listing.</p>}</article><GymHistory gym={gym}/></div><aside className="panel gym-book-box"><h2>Your session</h2><p className="gym-price">{price(gym.sessionPrice)}</p><p>{gym.sessionDurationMinutes} minutes · Both fighters</p><p>Pay at the gym. Owner approval and both fighters’ waivers are required.</p>{fighter ? <Link className="button-link" to={`/sessions?gymId=${gym.id}`}>Request a session</Link> : !session ? <Link className="button-link" to={`/login?returnTo=${encodeURIComponent(`/gyms/${gym.id}`)}`}>Sign in to book</Link> : <p>Enable Fighter access from your account to book a session with a match.</p>}</aside></div></section>;
}
