import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { api, errorText, post, upload } from './api';
import { LocationPicker } from './LocationPicker';
import { PageHeading } from './ui';
import { GymCover, price } from './Gyms';
import type { Gym, GymBooking, MapLocation } from './types';

function GymEditor({ gym, onSaved }: { gym?: Gym; onSaved: () => void }) {
  const [location, setLocation] = useState<MapLocation>(() => ({ latitude: gym?.latitude, longitude: gym?.longitude,
    regionCode: gym?.areaPath.find(a => a.level === 'region')?.code, provinceCode: gym?.areaPath.find(a => a.level === 'province')?.code,
    cityCode: gym?.areaPath.find(a => a.level === 'city')?.code, barangayCode: gym?.areaPath.find(a => a.level === 'barangay')?.code }));
  const [address, setAddress] = useState(gym?.address || ''); const [error, setError] = useState(''); const [busy, setBusy] = useState(false);
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); const data = Object.fromEntries(new FormData(event.currentTarget)); setBusy(true); setError('');
    try { await api(`/gyms${gym ? `/${gym.id}` : ''}`, { method: gym ? 'PUT' : 'POST', body: JSON.stringify({ ...data, slug: gym?.slug || `gym-${crypto.randomUUID()}`, areaCode: location.barangayCode || location.cityCode || '', latitude: location.latitude, longitude: location.longitude, sessionPrice: Number(data.sessionPrice), sessionDurationMinutes: Number(data.sessionDurationMinutes), version: gym?.version }) }); onSaved(); }
    catch (e) { setError(errorText(e)); } finally { setBusy(false); }
  }
  return <form className="panel gym-editor" onSubmit={save}><h2>{gym ? 'Edit gym details' : 'Create your gym'}</h2>{error && <p className="error" role="alert">{error}</p>}<fieldset disabled={busy}>
    <label>Gym name<input name="name" defaultValue={gym?.name} maxLength={200} required/></label><label>About your gym<textarea name="description" defaultValue={gym?.description} maxLength={2000}/></label>
    <label>Public contact information<input name="contactInformation" defaultValue={gym?.contactInformation} maxLength={300} placeholder="Public business phone or email"/></label>
    <div className="gym-form-row"><label>Session price (PHP, both fighters)<input name="sessionPrice" type="number" min="0" max="99999999.99" step="0.01" defaultValue={gym?.sessionPrice ?? 500} required/></label><label>Session length (minutes)<input name="sessionDurationMinutes" type="number" min="30" max="240" defaultValue={gym?.sessionDurationMinutes ?? 60} required/></label></div>
    <label>Gym address<input name="address" value={address} onChange={e => setAddress(e.target.value)} maxLength={300} required/></label><LocationPicker value={location} onChange={setLocation} purpose="gym" onAddressSelected={setAddress}/><p className="small muted">Choose at least a city and pin your gym. Select a barangay only when confirmed.</p><button>{busy ? 'Saving…' : gym ? 'Save gym details' : 'Create gym draft'}</button>
  </fieldset></form>;
}
function BookingCard({ booking, refresh }: { booking: GymBooking; refresh: () => void }) {
  const [busy, setBusy] = useState(false); const [error, setError] = useState(''); const [reasonAction, setReasonAction] = useState('');
  async function act(action: string, reason?: string) { setBusy(true); setError(''); try { await post(`/gyms/bookings/${booking.id}/${action}`, { reason }); refresh(); } catch (e) { setError(errorText(e)); } finally { setBusy(false); } }
  const unfinished = ['pending_approval', 'scheduled', 'confirmed'].includes(booking.status);
  return <article className="panel"><div className="section-heading"><h3>{booking.gymName}</h3><span className="pill">{booking.status.replace('_', ' ')}</span></div><strong>{booking.fighter1Name} &amp; {booking.fighter2Name}</strong><p>{new Date(booking.scheduledAt).toLocaleString()} · {booking.sessionDurationMinutes} minutes</p><p>{price(booking.quotedPrice)} · {booking.confirmationCount}/2 waivers submitted</p>
    {error && <p role="alert" className="error">{error}</p>}<div className="actions">{booking.status === 'pending_approval' && <><button disabled={busy} onClick={() => void act('approve')}>Approve request</button><button className="secondary" disabled={busy} onClick={() => setReasonAction('decline')}>Decline request</button></>}{unfinished && <button className="secondary" disabled={busy} onClick={() => setReasonAction('cancel')}>Cancel booking</button>}{booking.status === 'confirmed' && <button disabled={busy || new Date(booking.scheduledAt).getTime() + booking.sessionDurationMinutes * 60000 > Date.now()} onClick={() => void act('complete')}>Mark booking completed</button>}</div>
    {reasonAction && <form onSubmit={e => { e.preventDefault(); void act(reasonAction, String(new FormData(e.currentTarget).get('reason'))); }}><label>Reason<textarea name="reason" maxLength={500} required/></label><button disabled={busy}>Save {reasonAction}</button><button type="button" className="secondary" onClick={() => setReasonAction('')}>Back</button></form>}{booking.cancellationReason && <p>{booking.cancellationReason}</p>}
  </article>;
}
export function OwnerGyms({ canManageGyms, canManageBookings }: { canManageGyms: boolean; canManageBookings: boolean }) {
  const [gyms, setGyms] = useState<Gym[]>([]); const [bookings, setBookings] = useState<GymBooking[]>([]); const [editing, setEditing] = useState<number | 'new' | null>(null);
  const [error, setError] = useState(''); const [loading, setLoading] = useState(true); const [busy, setBusy] = useState(false); const [reload, setReload] = useState(0); const [notice, setNotice] = useState(''); const [coverVersion, setCoverVersion] = useState(0);
  useEffect(() => { const c = new AbortController(); setError(''); setLoading(true); void Promise.all([canManageGyms ? api<Gym[]>('/gyms/mine', { signal: c.signal }) : Promise.resolve([]), canManageBookings ? api<GymBooking[]>('/gyms/bookings', { signal: c.signal }) : Promise.resolve([])]).then(([g,b]) => { setGyms(g); setBookings(b); }).catch(e => { if (!c.signal.aborted) setError(errorText(e)); }).finally(() => { if (!c.signal.aborted) setLoading(false); }); return () => c.abort(); }, [reload, canManageGyms, canManageBookings]);
  function refresh() { setReload(v => v + 1); }
  async function publish(gym: Gym) { setBusy(true); setError(''); try { await post(`/gyms/${gym.id}/${gym.isPublished ? 'unpublish' : 'publish'}`); refresh(); } catch (e) { setError(errorText(e)); } finally { setBusy(false); } }
  async function cover(id: number, file?: File) { if (!file) return; setBusy(true); setError(''); try { await upload(`/gyms/${id}/cover`, file); setCoverVersion(v => v+1); setNotice('Gym cover saved.'); } catch (e) { setError(errorText(e)); } finally { setBusy(false); } }
  const selected = gyms.find(g => g.id === editing);
  return <section><PageHeading eyebrow="Your space. Their next story." title="My gyms"><div className="actions"><button className="secondary" onClick={refresh} disabled={loading || busy}>Refresh owner dashboard</button>{canManageGyms && <button onClick={() => setEditing('new')}>Add gym</button>}</div></PageHeading>
    {error && <p role="alert" className="error">{error}</p>}{notice && <p className="notice" role="status">{notice}</p>}{loading && <p role="status">Loading your gyms…</p>}
    {editing != null && <><GymEditor key={editing === 'new' ? 'new' : `${editing}-${selected?.version}`} gym={selected} onSaved={() => { setEditing(null); setNotice('Gym saved. Upload a cover and publish when ready.'); refresh(); }}/><button className="secondary" onClick={() => setEditing(null)}>Close editor</button></>}
    <div className="gym-grid">{gyms.map(gym => <article key={gym.id} className="panel gym-card"><GymCover gym={{...gym,coverUrl: `${gym.coverUrl}?v=${coverVersion}`}}/><div><h2>{gym.name}</h2><p>{gym.isPublished ? 'Published on map' : 'Unpublished draft'}</p><p>{price(gym.sessionPrice)} · {gym.sessionDurationMinutes} minutes</p><div className="actions"><button className="secondary" onClick={() => setEditing(gym.id)}>Edit gym</button><button disabled={busy} onClick={() => void publish(gym)}>{gym.isPublished ? 'Unpublish gym' : 'Publish gym'}</button>{gym.isPublished && <Link to={`/gyms/${gym.id}`}>View listing</Link>}</div><label>Gym cover image<input aria-label={`Cover image for ${gym.name}`} type="file" accept="image/jpeg,image/png,image/webp" disabled={busy} onChange={e => void cover(gym.id, e.target.files?.[0])}/></label></div></article>)}</div>
    {canManageGyms && !loading && !gyms.length && <p>No gyms yet. Create your first draft.</p>}{canManageBookings && <><h2>Booking requests &amp; sessions</h2><p className="muted">Approve requests only when your space is available. One booking at a time. Emergency contacts stay private to the fighters.</p><div className="session-list">{bookings.map(b => <BookingCard key={`${b.id}-${b.status}`} booking={b} refresh={refresh}/>)}</div>{!loading && !bookings.length && <p>No bookings yet.</p>}</>}
  </section>;
}
