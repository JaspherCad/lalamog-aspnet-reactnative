import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { api, errorText, upload } from './api';
import { Avatar } from './App';
import type { Profile } from './types';
export function ProfileEditor() {
  const [profile, setProfile] = useState<Profile | null>(null);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  async function load() {
    setLoading(true); setError('');
    try { setProfile(await api<Profile>('/profiles/me')); }
    catch (err) { setError(errorText(err)); } finally { setLoading(false); }
  }
  useEffect(() => { void load(); }, []);
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setBusy(true); setError(''); setNotice('');
    const data = Object.fromEntries(new FormData(event.currentTarget));
    try {
      setProfile(await api<Profile>('/profiles/me', { method: 'PUT', body: JSON.stringify({ ...data, experienceLevel: Number(data.experienceLevel) }) }));
      setNotice('Profile saved.');
    } catch (err) { setError(errorText(err)); } finally { setBusy(false); }
  }
  async function avatar(file: File) {
    setBusy(true); setError(''); setNotice('');
    try {
      if (file.size > 5 * 1024 * 1024) throw new Error('Choose a photo up to 5 MB.');
      setProfile(await upload<Profile>('/profiles/me/avatar', file)); setNotice('Photo updated.');
    } catch (err) { setError(errorText(err)); } finally { setBusy(false); }
  }
  return <section><p className="eyebrow">THIS IS YOUR CORNER</p><h1>My profile</h1>
    {error && <p role="alert" className="error">{error}</p>}{notice && <p role="status" className="notice">{notice}</p>}
    {loading ? <p role="status">Loading profile...</p> : profile ? <div className="profile-layout">
      <aside className="panel profile-summary"><Avatar profile={profile} /><h2>{profile.nickname}</h2>
        <label>Profile photo<input type="file" accept="image/jpeg,image/png,image/webp" disabled={busy} onChange={e => { const file = e.target.files?.[0]; if (file) void avatar(file); e.target.value = ''; }} /></label>
        <small className="muted">JPEG, PNG, or WebP. Up to 5 MB. Profile photos are publicly viewable.</small>
      </aside>
      <form className="panel" onSubmit={save}><fieldset disabled={busy}>
        <label>Nickname<input name="nickname" defaultValue={profile.nickname} minLength={2} maxLength={50} required /></label>
        <label>About you<textarea name="bio" defaultValue={profile.bio} maxLength={500} rows={3} placeholder="Your training goals" /></label>
        <label>Fighting style<input name="fightingStyle" defaultValue={profile.fightingStyle} maxLength={50} required /></label>
        <label>Experience<select name="experienceLevel" defaultValue={profile.experienceLevel}><option value={1}>Beginner</option><option value={2}>Intermediate</option><option value={3}>Experienced</option></select></label>
        <label>City / area<input name="location" defaultValue={profile.location} maxLength={200} placeholder="City or area" /></label>
        <label>Availability<input name="availability" defaultValue={profile.availability} maxLength={200} placeholder="e.g. Weekdays after 6 pm" /></label>
        <button>{busy ? 'Saving...' : 'Save profile'}</button>
      </fieldset></form>
    </div> : <button className="secondary" onClick={() => void load()}>Retry</button>}
  </section>;
}