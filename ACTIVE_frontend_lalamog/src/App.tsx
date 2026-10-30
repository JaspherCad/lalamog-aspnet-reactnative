import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { BrowserRouter, Navigate, NavLink, Route, Routes } from 'react-router-dom';
import { api, errorText, post, refreshCsrf } from './api';
import type { Profile, Session } from './types';
import { Matches } from './Matches';
import { Fights } from './Fights';
import { ProfileEditor } from './ProfileEditor';

export function Avatar({ profile, large = false }: { profile: Profile; large?: boolean }) {
  return <div className={large ? 'avatar avatar-large' : 'avatar'}>
    {profile.avatarUrl ? <img src={profile.avatarUrl} alt={`${profile.nickname}'s profile`} /> :
      <span aria-hidden="true">{profile.nickname.slice(0, 1).toUpperCase()}</span>}
  </div>;
}
function Auth({ onLogin }: { onLogin: (session: Session) => void }) {
  const [register, setRegister] = useState(false);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setBusy(true); setError('');
    const data = Object.fromEntries(new FormData(event.currentTarget));
    try {
      const session = await post<Session>(`/auth/${register ? 'register' : 'login'}`, data);
      await refreshCsrf(); onLogin(session);
    } catch (err) { setError(errorText(err)); } finally { setBusy(false); }
  }
  return <main className="auth-layout">
    <section className="intro"><a className="brand" href="/">LALAMOG<span>TRAIN TOGETHER</span></a>
      <p className="eyebrow">YOUR NEXT ROUND STARTS HERE</p><h1>Find your<br />sparring partner.</h1>
      <p>Meet local fighters. Make a connection. Get a session on the calendar.</p>
      <div className="intro-tags"><span>Boxing</span><span>Muay Thai</span><span>MMA</span></div>
    </section>
    <section className="panel auth-panel"><h2>{register ? 'Join the corner' : 'Back for another round?'}</h2>
      <p className="muted">{register ? 'Create your profile and start connecting.' : 'Sign in to find your next training session.'}</p>
      <form onSubmit={submit}>
        <fieldset disabled={busy}>
          {register && <label>Nickname<input name="nickname" autoComplete="nickname" minLength={2} maxLength={50} required /></label>}
          <label>Email<input name="email" type="email" autoComplete="email" maxLength={254} required /></label>
          <label>Password<input name="password" type="password" autoComplete={register ? 'new-password' : 'current-password'} minLength={register ? 10 : 1} maxLength={128} required /></label>
          {register && <small className="muted">At least 10 characters, with uppercase, lowercase, a number, and a symbol.</small>}
          {error && <p role="alert" className="error">{error}</p>}
          <button>{busy ? 'Please wait...' : register ? 'Create account' : 'Sign in'}</button>
        </fieldset>
      </form>
      <button className="link" disabled={busy} onClick={() => { setRegister(!register); setError(''); }}>
        {register ? 'Already have an account? Sign in' : 'New here? Create an account'}
      </button>
    </section>
  </main>;
}
function Discover() {
  const [profiles, setProfiles] = useState<Profile[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  async function load() {
    setLoading(true); setError('');
    try { setProfiles(await api<Profile[]>('/profiles/discover')); }
    catch (err) { setError(errorText(err)); } finally { setLoading(false); }
  }
  useEffect(() => { void load(); }, []);
  async function swipe(direction: 'like' | 'pass') {
    const profile = profiles[0]; if (!profile || busy) return;
    setBusy(true); setError(''); setNotice('');
    try {
      const result = await post<{ matched: boolean }>('/matches/swipes', { targetId: profile.userId, direction });
      setProfiles(current => current.filter(p => p.userId !== profile.userId));
      if (result.matched) setNotice(`You and ${profile.nickname} matched! Open Matches to say hello.`);
    } catch (err) { setError(errorText(err)); } finally { setBusy(false); }
  }
  const profile = profiles[0];
  return <section><div className="section-heading"><div><p className="eyebrow">FIND YOUR PEOPLE</p><h1>In your corner</h1></div><button className="secondary" disabled={loading || busy} onClick={() => void load()}>Refresh</button></div>
    <p className="muted">Like a fighter to connect. When you both like each other, it's a match.</p>
    {notice && <p role="status" className="notice">{notice}</p>}{error && <p role="alert" className="error">{error}</p>}
    {loading ? <p role="status">Finding fighters...</p> : profile ? <article className="panel fighter-card">
      <Avatar profile={profile} large /><div className="fighter-details"><span className="pill">{profile.fightingStyle}</span>
        <h2>{profile.nickname}</h2><p className="muted">{['', 'Beginner', 'Intermediate', 'Experienced'][profile.experienceLevel]}</p>
        <p>{profile.bio || 'Ready to put in the rounds.'}</p>
        <dl><dt>Based in</dt><dd>{profile.location || 'Not added yet'}</dd><dt>Available</dt><dd>{profile.availability || 'Ask in chat'}</dd></dl>
        <div className="actions"><button className="secondary" disabled={busy} onClick={() => void swipe('pass')}>Pass</button><button disabled={busy} onClick={() => void swipe('like')}>{busy ? 'Saving...' : 'Like & connect'}</button></div>
      </div></article> : !error && <div className="panel empty"><h2>You're all caught up</h2><p>New fighters will appear here when they join. Try a second account in a private browser window to test matching.</p></div>}
  </section>;
}
export default function App() {
  const [session, setSession] = useState<Session | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  async function restore() {
    setLoading(true); setError('');
    try {
      await refreshCsrf();
      const value = await api<Session>('/auth/me');
      setSession(value); await refreshCsrf();
    } catch (err) {
      if ((err as { status?: number }).status !== 401) setError(errorText(err));
    } finally { setLoading(false); }
  }
  useEffect(() => { void restore(); }, []);
  useEffect(() => {
    function expired() { setSession(null); void refreshCsrf().catch(() => {}); }
    window.addEventListener('session-expired', expired);
    return () => window.removeEventListener('session-expired', expired);
  }, []);
  async function logout() {
    try { await post('/auth/logout'); setSession(null); await refreshCsrf(); }
    catch (err) { setError(errorText(err)); }
  }
  if (loading) return <main className="app-main" role="status">Loading your corner...</main>;
  if (error && !session) return <main className="app-main"><p role="alert" className="error">{error}</p><button onClick={() => void restore()}>Retry connection</button></main>;
  if (!session) return <Auth onLogin={setSession} />;
  return <BrowserRouter><div className="app-layout">
    <header className="app-header"><a className="brand" href="/">LALAMOG<span>TRAIN TOGETHER</span></a><button className="link" onClick={() => void logout()}>Sign out</button></header>
    <nav aria-label="Main navigation">{[['/', 'Discover'], ['/matches', 'Matches'], ['/sessions', 'Sessions'], ['/profile', 'My profile']].map(([path, label]) =>
      <NavLink key={path} to={path} end={path === '/'} className={({ isActive }) => isActive ? 'nav-active' : ''}>{label}</NavLink>)}</nav>
    <main className="app-main" key={session.userId}>{error && <p role="alert" className="error">{error}</p>}
      <Routes><Route path="/" element={<Discover />} /><Route path="/matches/*" element={<Matches session={session} />} />
        <Route path="/sessions" element={<Fights session={session} />} /><Route path="/profile" element={<ProfileEditor />} />
        <Route path="*" element={<Navigate to="/" replace />} /></Routes>
    </main><footer>Good training starts with good communication.</footer>
  </div></BrowserRouter>;
}