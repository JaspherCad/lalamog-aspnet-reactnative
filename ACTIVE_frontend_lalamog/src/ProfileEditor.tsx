import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { api, errorText, upload } from './api';
import { Avatar, fighterName, Icon, PageHeading, StyleStack } from './ui';
import { fightingStyles, parseStyles, serializeStyles } from './fightingStyles';
import type { StyleKey } from './fightingStyles';
import type { MapLocation, Profile } from './types';
import { LocationPicker } from './LocationPicker';

export function ProfileEditor() {
  const [mapLocation, setMapLocation] = useState<MapLocation>({});
  const [profile, setProfile] = useState<Profile | null>(null); const [styles, setStyles] = useState<StyleKey[]>([]);
  const [error, setError] = useState(''); const [notice, setNotice] = useState(''); const [busy, setBusy] = useState(false); const [loading, setLoading] = useState(true);
  async function load() {
    setLoading(true); setError('');
    try { const value = await api<Profile>('/profiles/me'); setProfile(value); setMapLocation(value.mapLocation || {}); setStyles(parseStyles(value.fightingStyle).keys); }
    catch (err) { setError(errorText(err)); } finally { setLoading(false); }
  }
  useEffect(() => { void load(); }, []);
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setBusy(true); setError(''); setNotice('');
    const data = Object.fromEntries(new FormData(event.currentTarget));
    try {
      const fightingStyle = styles.length ? serializeStyles(styles) : profile && parseStyles(profile.fightingStyle).legacy.length ? profile.fightingStyle : '';
      if (!fightingStyle) throw new Error('Choose at least one fighting style.');
      const value = await api<Profile>('/profiles/me', { method: 'PUT', body: JSON.stringify({ ...data, mapLocation, fightingStyle, experienceLevel: Number(data.experienceLevel) }) });
      setProfile(value); setMapLocation(value.mapLocation || {}); setStyles(parseStyles(value.fightingStyle).keys); setNotice('Profile saved.');
    } catch (err) { setError(errorText(err)); } finally { setBusy(false); }
  }
  async function avatar(file: File) {
    setBusy(true); setError(''); setNotice('');
    try { if (file.size > 5 * 1024 * 1024) throw new Error('Choose a photo up to 5 MB.'); setProfile(await upload<Profile>('/profiles/me/avatar', file)); setNotice('Photo updated.'); }
    catch (err) { setError(errorText(err)); } finally { setBusy(false); }
  }
  return <section><PageHeading eyebrow="Your story. Your style." title="My profile"><Link className="button-link secondary" to="/sessions"><Icon name="calendar"/>Your fights</Link></PageHeading>
    {error && <p role="alert" className="error">{error}</p>}{notice && <p role="status" className="notice">{notice}</p>}
    {loading ? <p role="status">Loading profile…</p> : profile ? <div className="profile-layout"><aside className="panel profile-summary"><span className="corner-label">Your fighter card</span><Avatar profile={profile} large/><h2 className="fighter-name">{fighterName(profile)}</h2>{profile.fighterName && <p className="fighter-alias">“{profile.nickname}”</p>}<StyleStack value={styles.length ? serializeStyles(styles) : parseStyles(profile.fightingStyle).legacy.length ? profile.fightingStyle : ''}/>
      <label>Profile photo<input type="file" accept="image/jpeg,image/png,image/webp" disabled={busy} onChange={event => { const file = event.target.files?.[0]; if (file) void avatar(file); event.target.value = ''; }}/></label><small className="muted">JPEG, PNG, or WebP. Up to 5 MB. Profile photos are publicly viewable.</small></aside>
      <form className="panel profile-form" onSubmit={save}><fieldset disabled={busy}><h2>The person behind the portrait</h2>
        <label>Fighter name (unique)<input name="fighterName" aria-label="Fighter name (unique)" defaultValue={profile.fighterName || ''} minLength={2} maxLength={50} pattern="[A-Za-z0-9][A-Za-z0-9._-]*" autoComplete="username" aria-describedby="fighter-name-help" required/><small id="fighter-name-help" className="muted">Your unique name. Letters, numbers, dots, underscores and hyphens.</small></label>
        <label>Alias<input name="nickname" aria-label="Alias" aria-describedby="alias-help" defaultValue={profile.nickname} minLength={2} maxLength={50} autoComplete="nickname" placeholder="e.g. Crow, Grim Reaper" required/><small id="alias-help" className="muted">Your chosen nickname. Other fighters may use the same alias.</small></label>
        <label>About you<textarea name="bio" defaultValue={profile.bio} maxLength={500} rows={3} placeholder="A little about you. A reason to say hello."/></label>
        <fieldset className="style-picker"><legend>Build your style stack</legend><p className="muted small" id="style-help">Choose one or more. Combine your backgrounds.</p><div className="style-options">{fightingStyles.map(style => <label className={`style-option style-${style.key} ${styles.includes(style.key) ? 'is-selected' : ''}`} key={style.key} title={style.description}>
          <input type="checkbox" aria-describedby="style-help" checked={styles.includes(style.key)} onChange={event => setStyles(current => event.target.checked ? [...current, style.key] : current.filter(key => key !== style.key))}/><Icon name={style.icon}/><span>{style.label}</span>{styles.includes(style.key) && <Icon className="selection-check" name="check"/>}</label>)}</div>
          {parseStyles(profile.fightingStyle).legacy.length > 0 && <p className="small muted">Current legacy style: {profile.fightingStyle}. It stays saved until you choose a new style stack.</p>}</fieldset>
        <div className="form-grid"><label>Experience<select name="experienceLevel" defaultValue={profile.experienceLevel}><option value={1}>Beginner</option><option value={2}>Intermediate</option><option value={3}>Experienced</option></select></label><label>City / area<input key={profile.location} name="location" defaultValue={profile.location} maxLength={200} placeholder="City or area"/></label></div>
        <LocationPicker value={mapLocation} onChange={setMapLocation} purpose="profile"/>
        <label>Availability<input name="availability" defaultValue={profile.availability} maxLength={200} placeholder="e.g. Weekends, evenings"/></label><button>{busy ? 'Saving…' : 'Save profile'}<Icon name="check"/></button>
      </fieldset></form></div> : <button className="secondary" onClick={() => void load()}>Retry</button>}
  </section>;
}
