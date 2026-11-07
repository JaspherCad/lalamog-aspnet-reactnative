import { useEffect, useMemo, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { api, errorText } from './api';
import { LocationMap } from './LocationMap';
import { Icon, Modal } from './ui';
import { GymCover, GymHistory, price } from './Gyms';
import type { Fight, Gym, Session } from './types';
import './FightMap.css';

function MapModal({ title, onClose, children }: { title: string; onClose: () => void; children: ReactNode }) {
  return <Modal title={title} onClose={onClose} className="map-notebook-dialog">
    <span className="map-notebook-binding" aria-hidden="true"/>
    <div className="map-notebook-page">{children}</div>
  </Modal>;
}

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
  const [privateError, setPrivateError] = useState('');
  const [privateLoading, setPrivateLoading] = useState(false);
  const [showGyms, setShowGyms] = useState(true);
  const [showPrivate, setShowPrivate] = useState(true);
  const [panel, setPanel] = useState<'gyms' | 'private' | 'filters' | 'about' | 'fight' | null>(params.has('fightId') && canViewPrivate ? 'fight' : null);
  const gymsButton = useRef<HTMLButtonElement>(null);
  const privateButton = useRef<HTMLButtonElement>(null);
  const pinOpener = useRef<HTMLElement | null>(null);
  useEffect(() => {
    const controller = new AbortController();
    setLoading(true); setError(''); setPrivateError(''); setFights([]);
    setPrivateLoading(canViewPrivate);
    void api<Gym[]>('/gyms', { signal: controller.signal }).then(setGyms)
      .catch(err => { if (!controller.signal.aborted) setError(errorText(err)); })
      .finally(() => { if (!controller.signal.aborted) setLoading(false); });
    if (canViewPrivate) void api<Fight[]>('/fights', { signal: controller.signal }).then(setFights)
      .catch(err => { if (!controller.signal.aborted) setPrivateError(errorText(err)); })
      .finally(() => { if (!controller.signal.aborted) setPrivateLoading(false); });
    return () => controller.abort();
  }, [reload, session?.userId, canViewPrivate]);
  const pinned = useMemo(() => fights.filter(fight => fight.gymId == null && fight.mapLocation?.latitude != null && fight.mapLocation.longitude != null && (filter === 'all' || fight.status === filter)), [fights, filter]);
  // Negative marker keys separate shared gym IDs from private fight IDs.
  const markers = useMemo(() => [...(showGyms ? gyms.map(gym => ({ id: -gym.id, latitude: gym.latitude, longitude: gym.longitude, label: gym.name, isPublic: true })) : []),
    ...(canViewPrivate && showPrivate ? pinned.map(fight => ({ id: fight.id, latitude: fight.mapLocation!.latitude!, longitude: fight.mapLocation!.longitude!, label: fight.location })) : [])], [pinned, gyms, showGyms, showPrivate, canViewPrivate]);
  const selected = canViewPrivate ? pinned.find(fight => fight.id === (selectedId ?? Number(params.get('fightId')))) : undefined;
  function choosePin(id: number) {
    pinOpener.current = panel === 'gyms' ? gymsButton.current : panel === 'private' ? privateButton.current : document.activeElement as HTMLElement;
    if (id < 0) { setPanel(null); setGymId(-id); }
    else { setSelectedId(id); setPanel('fight'); }
  }
  function closePin() {
    setGymId(null); setPanel(null);
    requestAnimationFrame(() => {
      if (pinOpener.current?.isConnected) pinOpener.current.focus();
      else gymsButton.current?.focus();
    });
  }
  return <section className="fight-map-page" aria-label="Explore gym and fight locations">
    <LocationMap fullscreen value={showPrivate && selected ? selected.mapLocation || {} : {}} markers={markers} onSelect={choosePin}/>
    <div className="map-floating-top">
      <div className="map-title-card"><span className="eyebrow">Your places. Your stories.</span><h1>Public map</h1><span className="map-scope"><span className="map-key gym-key" aria-hidden="true">◆</span>Public gyms{canViewPrivate && <><span className="map-key private-key" aria-hidden="true">●</span>Your private pins</>}</span></div>
      <div className="map-top-actions"><button className="secondary" aria-haspopup="dialog" onClick={() => setPanel('filters')}><Icon name="map"/>Filters{(!showGyms || (canViewPrivate && (!showPrivate || filter !== 'all'))) && <span className="map-filter-dot" aria-label="Filters active"/>}</button><button className="secondary icon-button" aria-label="About this map" aria-haspopup="dialog" onClick={() => setPanel('about')}><Icon name="info"/></button></div>
    </div>
    {(loading || privateLoading || error || privateError) && <div className="map-load-notice" role={error || privateError ? 'alert' : 'status'}>{loading || privateLoading ? 'Loading locations…' : 'Some locations could not load.'}{(error || privateError) && <button className="text-action" onClick={() => setReload(value => value + 1)}>Retry map locations</button>}</div>}
    <div className="map-floating-bottom"><button ref={gymsButton} aria-haspopup="dialog" onClick={() => setPanel('gyms')}><Icon name="discover"/><span>Browse gyms<small>{loading ? 'Loading…' : `${gyms.length} public ${gyms.length === 1 ? 'gym' : 'gyms'}`}</small></span></button>{canViewPrivate && <button ref={privateButton} className="secondary" aria-haspopup="dialog" onClick={() => setPanel('private')}><Icon name="pin"/><span>Private pins<small>{privateLoading ? 'Loading…' : `${pinned.length} ${filter === 'all' ? 'locations' : filter}`}</small></span></button>}</div>
    {panel === 'filters' && <MapModal title="Map filters" onClose={() => setPanel(null)}><p>Choose the pins shown on your map.</p><label className="map-layer-option"><input type="checkbox" checked={showGyms} onChange={event => setShowGyms(event.target.checked)}/><span><strong>Public gyms</strong><small>Shared venues and completed fight history.</small></span></label>{canViewPrivate && <><label className="map-layer-option"><input type="checkbox" checked={showPrivate} onChange={event => setShowPrivate(event.target.checked)}/><span><strong>Your private pins</strong><small>Only sessions you take part in.</small></span></label><label>Your private fight status<select disabled={!showPrivate} value={filter} onChange={event => { setFilter(event.target.value); setSelectedId(null); }} aria-label="Fight status"><option value="all">All fights</option>{['scheduled', 'confirmed', 'completed', 'canceled'].map(status => <option key={status}>{status}</option>)}</select></label></>}<div className="dialog-actions"><button onClick={() => setPanel(null)}>Show map</button><button className="secondary" onClick={() => { setShowGyms(true); setShowPrivate(true); setFilter('all'); setSelectedId(null); }}>Reset filters</button></div></MapModal>}
    {panel === 'about' && <MapModal title="About this map" onClose={() => setPanel(null)}><p>Explore public gym pins and their completed fight history. Your private session pins are visible only to you and your opponent.</p><p>Diamond pins are public gyms. Round pins are your private locations. Tap a pin or browse the lists for details.</p><p className="small muted">The map does not request your GPS location. Public gym pins are venue locations, not fighters’ home locations.</p></MapModal>}
    {panel === 'gyms' && <MapModal title="Public gyms" onClose={() => setPanel(null)}><p className="muted">Choose a gym for details and fight history.</p>{loading ? <p role="status">Loading public gyms…</p> : error ? <div role="alert"><p>{error}</p><button className="secondary" onClick={() => setReload(value => value + 1)}>Retry public gyms</button></div> : <><div className="map-venue-list">{!gyms.length && <p>No public gyms yet. Check back for new venues.</p>}{gyms.map(gym => <button className="panel map-venue public-gym" key={gym.id} onClick={() => choosePin(-gym.id)}><strong>{gym.name}</strong><span>{gym.address}</span><small>{gym.isDemo ? 'Fictional demo gym · ' : ''}View details &amp; fights <Icon name="arrow"/></small></button>)}</div><Link className="text-action" to="/gyms">Open gym directory <Icon name="arrow"/></Link></>}</MapModal>}
    {panel === 'private' && canViewPrivate && <MapModal title="Your private pins" onClose={() => setPanel(null)}><p className="muted">{filter === 'all' ? 'Your pinned private arrangements.' : `Showing ${filter} private arrangements.`}</p>{privateLoading ? <p role="status">Loading your private pins…</p> : privateError ? <div role="alert"><p>{privateError}</p><button className="secondary" onClick={() => setReload(value => value + 1)}>Retry private pins</button></div> : <div className="map-venue-list">{!pinned.length && <p>No private pins for this filter. Choose a pin when arranging a fight, or change the status filter.</p>}{pinned.map(fight => <button className={`panel map-venue ${selected?.id === fight.id ? 'is-selected' : ''}`} key={fight.id} onClick={() => choosePin(fight.id)}><strong>{fight.location}</strong><span>{fight.address}</span><small>{new Date(fight.scheduledAt).toLocaleString()} · {fight.status}</small></button>)}</div>}{!!fights.filter(fight => fight.mapLocation?.latitude == null).length && <p className="small muted">Some fights have a text address only. <Link to="/sessions">View all your fights</Link>.</p>}</MapModal>}
    {panel === 'fight' && canViewPrivate && <MapModal title={selected?.location || 'Private fight location'} onClose={closePin}>{privateLoading ? <p role="status">Loading your private fight…</p> : selected ? <><span className="corner-label">Private session</span><p>{selected.address}</p><p>{new Date(selected.scheduledAt).toLocaleString()} · {selected.status}</p><Link className="button-link" to={`/sessions?matchId=${selected.matchId}`}>Open selected fight</Link></> : <p>This pin is not available for the current filter. Choose another private pin.</p>}</MapModal>}
    {gymId != null && <GymDetails key={gymId} id={gymId} onClose={closePin}/>}
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
  return <MapModal title={gym?.name || 'Public gym details'} onClose={onClose}>
    {error ? <div role="alert"><p>{error}</p><button onClick={() => setReload(value => value + 1)}>Retry gym details</button></div> : !gym ? <p role="status">Loading gym details…</p> : <>
      {gym.isDemo && <p className="notice">Fictional demo gym at a test pin. This is not a verified gym listing.</p>}
      <p className="map-notebook-label">01 / Venue notes</p><p>{gym.address}</p><p className="small muted">{gym.areaPath.map(area => area.name).join(' › ')}</p>
      <p className="small">Latitude {gym.latitude} · Longitude {gym.longitude}</p>
      <figure className="map-notebook-photo"><GymCover gym={gym}/><figcaption>{gym.coverUrl ? 'Venue photograph' : 'Photo not added yet'}</figcaption></figure>
      <p>{gym.description}</p><p className="map-notebook-label">02 / Session details</p><p><strong>{price(gym.sessionPrice)} / {gym.sessionDurationMinutes} min</strong> · Both fighters · Pay at gym</p><p>{gym.contactInformation}</p><Link className="button-link" to={`/gyms/${gym.id}`}>View gym &amp; book</Link><GymHistory gym={gym}/>
    </>}
  </MapModal>;
}
