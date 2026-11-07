import { useEffect, useMemo, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { api, errorText } from './api';
import { LocationMap } from './LocationMap';
import { Modal, PageHeading } from './ui';
import { GymCover, GymHistory, price } from './Gyms';
import type { Fight, Gym, Session } from './types';

export function FightMap({ session }: { session: Session | null }) {
  const canViewPrivate = !!session && !!session.permissions?.includes('user_canSchedule');
  const [fights, setFights] = useState<Fight[]>([]);
  const [gyms, setGyms] = useState<Gym[]>([]);
  const [gymId, setGymId] = useState<number | null>(null);
  const [reload, setReload] = useState(0);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [filter, setFilter] = useState('all');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [params] = useSearchParams();
  useEffect(() => {
    const controller = new AbortController();
    setLoading(true); setError('');
    void api<Gym[]>('/gyms', { signal: controller.signal }).then(setGyms)
      .catch(err => { if (!controller.signal.aborted) setError(errorText(err)); })
      .finally(() => { if (!controller.signal.aborted) setLoading(false); });
    if (canViewPrivate) void api<Fight[]>('/fights', { signal: controller.signal }).then(setFights)
      .catch(err => { if (!controller.signal.aborted) setError(errorText(err)); });
    return () => controller.abort();
  }, [reload, session?.userId]);
  const pinned = useMemo(() => fights.filter(fight => fight.gymId == null && fight.mapLocation?.latitude != null && fight.mapLocation.longitude != null && (filter === 'all' || fight.status === filter)), [fights, filter]);
  // Negative marker keys separate shared gym IDs from private fight IDs.
  const markers = useMemo(() => [...gyms.map(gym => ({ id: -gym.id, latitude: gym.latitude, longitude: gym.longitude, label: gym.name, isPublic: true })),
    ...pinned.map(fight => ({ id: fight.id, latitude: fight.mapLocation!.latitude!, longitude: fight.mapLocation!.longitude!, label: fight.location }))], [pinned, gyms]);
  const selected = pinned.find(fight => fight.id === selectedId || (selectedId == null && fight.id === Number(params.get('fightId')))) || pinned[0];
  return <section><PageHeading eyebrow="Your places. Your stories." title={canViewPrivate ? "Your fight map" : "Gym map"}><Link className="button-link secondary" to="/gyms">Browse gyms</Link></PageHeading>
    <p className="muted">Explore public gym pins and their fight history. Your private session pins are visible only to you and your opponent.</p>
    {canViewPrivate && <label className="map-filter">Your private fight status<select value={filter} onChange={e => { setFilter(e.target.value); setSelectedId(null); }} aria-label="Fight status"><option value="all">All fights</option>{['scheduled', 'confirmed', 'completed', 'canceled'].map(status => <option key={status}>{status}</option>)}</select></label>}
    {error && <div role="alert" className="error">{error} <button className="secondary" onClick={() => setReload(value => value + 1)}>Retry map locations</button></div>}
    {loading ? <p role="status">Loading fight locations…</p> : <div className="fight-map-layout"><div className="panel"><LocationMap value={(selectedId != null || params.has('fightId')) ? selected?.mapLocation || {} : {}} markers={markers} onSelect={id => id < 0 ? setGymId(-id) : setSelectedId(id)}/>
      {!markers.length && <p>No pinned venues yet. Choose a map pin when arranging your next fight; existing text addresses are unchanged.</p>}</div>
      <div className="map-venue-list"><h2>Public gyms</h2>{!gyms.length && <p>No public gyms yet.</p>}{gyms.map(gym => <button className="panel map-venue public-gym" key={`gym-${gym.id}`} onClick={() => setGymId(gym.id)}><strong>{gym.name}</strong><span>{gym.address}</span><small>{gym.isDemo ? 'Fictional demo gym · ' : ''}View details &amp; fights</small></button>)}
        {canViewPrivate && <h2>Your private pins</h2>}{pinned.map(fight => <button className={`panel map-venue ${selected?.id === fight.id ? 'is-selected' : ''}`} key={fight.id} onClick={() => setSelectedId(fight.id)}><strong>{fight.location}</strong><span>{fight.address}</span><small>{new Date(fight.scheduledAt).toLocaleString()} · {fight.status}</small></button>)}
        {selected && <Link className="button-link" to={`/sessions?matchId=${selected.matchId}`}>Open selected fight</Link>}
        {!!fights.filter(fight => fight.mapLocation?.latitude == null).length && <p className="small muted">Some fights have a text address only. <Link to="/sessions">View all your fights</Link>.</p>}</div>
    </div>}
    {gymId != null && <GymDetails key={gymId} id={gymId} onClose={() => setGymId(null)}/>}
  </section>;
}

function GymDetails({ id, onClose }: { id: number; onClose: () => void }) {
  const [gym, setGym] = useState<Gym | null>(null);
  const [error, setError] = useState('');
  const [reload, setReload] = useState(0);
  useEffect(() => {
    const controller = new AbortController(); setError('');
    void api<Gym>(`/gyms/${id}`, { signal: controller.signal }).then(setGym)
      .catch(err => { if (!controller.signal.aborted) setError(errorText(err)); });
    return () => controller.abort();
  }, [id, reload]);
  return <Modal title={gym?.name || 'Public gym details'} onClose={onClose}>
    {error ? <div role="alert"><p>{error}</p><button onClick={() => setReload(value => value + 1)}>Retry gym details</button></div> : !gym ? <p role="status">Loading gym details…</p> : <>
      {gym.isDemo && <p className="notice">Fictional demo gym at a test pin. This is not a verified gym listing.</p>}
      <p>{gym.address}</p><p className="small muted">{gym.areaPath.map(area => area.name).join(' › ')}</p>
      <p className="small">Latitude {gym.latitude} · Longitude {gym.longitude}</p>
      <GymCover gym={gym}/><p>{gym.description}</p><p><strong>{price(gym.sessionPrice)} / {gym.sessionDurationMinutes} min</strong> · Both fighters · Pay at gym</p><p>{gym.contactInformation}</p><Link className="button-link" to={`/gyms/${gym.id}`}>View gym &amp; book</Link><GymHistory gym={gym}/>
    </>}
  </Modal>;
}
