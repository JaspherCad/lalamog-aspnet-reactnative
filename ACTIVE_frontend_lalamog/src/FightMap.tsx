import { useEffect, useMemo, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { api, errorText } from './api';
import { LocationMap } from './LocationMap';
import { PageHeading } from './ui';
import type { Fight } from './types';

export function FightMap() {
  const [fights, setFights] = useState<Fight[]>([]);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [filter, setFilter] = useState('all');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [params] = useSearchParams();
  useEffect(() => {
    const controller = new AbortController();
    void api<Fight[]>('/fights', { signal: controller.signal }).then(setFights)
      .catch(err => { if (!controller.signal.aborted) setError(errorText(err)); })
      .finally(() => { if (!controller.signal.aborted) setLoading(false); });
    return () => controller.abort();
  }, []);
  const pinned = useMemo(() => fights.filter(fight => fight.mapLocation?.latitude != null && fight.mapLocation.longitude != null && (filter === 'all' || fight.status === filter)), [fights, filter]);
  const markers = useMemo(() => pinned.map(fight => ({ id: fight.id, latitude: fight.mapLocation!.latitude!, longitude: fight.mapLocation!.longitude!, label: fight.location })), [pinned]);
  const selected = pinned.find(fight => fight.id === selectedId || (selectedId == null && fight.id === Number(params.get('fightId')))) || pinned[0];
  return <section><PageHeading eyebrow="Your places. Your stories." title="Your fight map"><Link className="button-link secondary" to="/sessions">Arrange a fight</Link></PageHeading>
    <p className="muted">Only your fights appear here. Venue pins and addresses stay private to the two participants.</p>
    <label className="map-filter">Fight status<select value={filter} onChange={e => { setFilter(e.target.value); setSelectedId(null); }} aria-label="Fight status"><option value="all">All fights</option>{['scheduled', 'confirmed', 'completed', 'canceled'].map(status => <option key={status}>{status}</option>)}</select></label>
    {error && <p role="alert" className="error">{error}</p>}
    {loading ? <p role="status">Loading fight locations…</p> : <div className="fight-map-layout"><div className="panel"><LocationMap value={(selectedId != null || params.has('fightId')) ? selected?.mapLocation || {} : {}} markers={markers} onSelect={setSelectedId}/>
      {!pinned.length && <p>No pinned venues yet. Choose a map pin when arranging your next fight; existing text addresses are unchanged.</p>}</div>
      <div className="map-venue-list">{pinned.map(fight => <button className={`panel map-venue ${selected?.id === fight.id ? 'is-selected' : ''}`} key={fight.id} onClick={() => setSelectedId(fight.id)}><strong>{fight.location}</strong><span>{fight.address}</span><small>{new Date(fight.scheduledAt).toLocaleString()} · {fight.status}</small></button>)}
        {selected && <Link className="button-link" to={`/sessions?matchId=${selected.matchId}`}>Open selected fight</Link>}
        {!!fights.filter(fight => fight.mapLocation?.latitude == null).length && <p className="small muted">Some fights have a text address only. <Link to="/sessions">View all your fights</Link>.</p>}</div>
    </div>}
  </section>;
}
