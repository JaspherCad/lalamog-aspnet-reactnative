import { useEffect, useState } from 'react';
import type { FormEvent, ReactNode } from 'react';
import { BrowserRouter, Link, Navigate, NavLink, Route, Routes } from 'react-router-dom';
import { errorText, post, api, refreshCsrf } from './api';
import type { Session } from './types';
import { Matches } from './Matches';
import { Fights } from './Fights';
import { FightMap } from './FightMap';
import { ProfileEditor } from './ProfileEditor';
import { Discover } from './Discover';
import { Icon } from './ui';
import type { IconName } from './ui';

function Brand() { return <Link className="brand" to="/">LALAMOG<span>People. Fights. Stories.</span></Link>; }
function Auth({ onLogin }: { onLogin: (session: Session) => void }) {
  const [register, setRegister] = useState(false); const [error, setError] = useState(''); const [busy, setBusy] = useState(false);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setBusy(true); setError('');
    const data = Object.fromEntries(new FormData(event.currentTarget));
    try { const session = await post<Session>(`/auth/${register ? 'register' : 'login'}`, data); await refreshCsrf(); onLogin(session); }
    catch (err) { setError(errorText(err)); } finally { setBusy(false); }
  }
  return <main className="auth-layout"><section className="intro"><Brand/><p className="eyebrow">Good stories start with a connection</p>
    <h1>Same spirit.<br/>Different <span>opponent.</span></h1><p>Find your people. Swipe for a match. Take the next round from there.</p>
    <div className="intro-tags"><span><Icon name="fist"/>Swipe</span><span><Icon name="inbox"/>Connect</span><span><Icon name="calendar"/>Make a plan</span></div>
    <Link className="text-action" to="/preview">Explore the design preview <Icon name="arrow"/></Link></section>
    <section className="panel auth-panel"><span className="corner-label">Your way in</span><h2>{register ? 'Join the lineup.' : 'Back for another round?'}</h2><p className="muted">{register ? 'Create your profile. Your next match is out there.' : 'Sign in and pick up where you left off.'}</p>
      <form onSubmit={submit}><fieldset disabled={busy}>
        {register && <><label>Fighter name (unique)<input name="fighterName" autoComplete="username" minLength={2} maxLength={50} pattern="[A-Za-z0-9][A-Za-z0-9._-]*" placeholder="e.g. Miko" required/></label><label>Alias<input name="nickname" autoComplete="nickname" minLength={2} maxLength={50} placeholder="e.g. Crow" required/></label></>}
        <label>Email<input name="email" type="email" autoComplete="email" maxLength={254} required/></label>
        <label>Password<input name="password" type="password" autoComplete={register ? 'new-password' : 'current-password'} minLength={register ? 10 : 1} maxLength={128} required/></label>
        {register && <small className="muted">At least 10 characters, with uppercase, lowercase, a number, and a symbol.</small>}
        {error && <p role="alert" className="error">{error}</p>}<button>{busy ? 'Please wait…' : register ? 'Create account' : 'Sign in'}<Icon name="arrow"/></button>
      </fieldset></form><button className="link" disabled={busy} onClick={() => { setRegister(!register); setError(''); }}>{register ? 'Already have an account? Sign in' : 'New here? Create an account'}</button>
      <p className="auth-footnote">A personal fight-club portfolio project.</p>
    </section></main>;
}
function AppShell({ children, logout, preview = false }: { children: ReactNode; logout?: () => void; preview?: boolean }) {
  const [texture, setTexture] = useState(true);
  useEffect(() => { document.documentElement.dataset.texture = String(texture); return () => { delete document.documentElement.dataset.texture; }; }, [texture]);
  const nav: { path: string; label: string; icon: IconName }[] = [
    { path: preview ? '/preview' : '/', label: 'Discover', icon: 'discover' }, { path: '/matches', label: 'Inbox', icon: 'inbox' },
    { path: '/map', label: 'Map', icon: 'map' }, { path: '/profile', label: 'Profile', icon: 'profile' },
  ];
  return <div className="app-layout"><a href="#main" className="skip-link">Skip to content</a>
    <header className="app-header"><Brand/><nav className="main-nav" aria-label="Main navigation">{nav.map(item => <NavLink key={item.path} to={item.path} end={item.path === '/' || item.path === '/preview'} className={({ isActive }) => isActive ? 'nav-active' : ''}><Icon name={item.icon}/><span>{item.label}</span></NavLink>)}</nav>
      <div className="header-actions"><button className="texture-toggle" aria-pressed={texture} onClick={() => setTexture(!texture)} title="Toggle paper texture"><span className="texture-swatch"/><span className="sr-only">Paper texture</span></button>{preview ? <Link className="header-link" to="/">Sign in <Icon name="arrow"/></Link> : <button className="header-link" onClick={logout}>Sign out <Icon name="logout"/></button>}</div></header>
    <main id="main" className="app-main" tabIndex={-1}>{children}</main><footer className="site-footer"><span>LALAMOG / Made for the next story.</span><Link to="/sessions">Your fights <Icon name="arrow"/></Link></footer>
  </div>;
}
function Application() {
  const [session, setSession] = useState<Session | null>(null); const [loading, setLoading] = useState(true); const [error, setError] = useState('');
  async function restore() {
    setLoading(true); setError('');
    try { await refreshCsrf(); setSession(await api<Session>('/auth/me')); await refreshCsrf(); }
    catch (err) { if ((err as { status?: number }).status !== 401) setError(errorText(err)); }
    finally { setLoading(false); }
  }
  useEffect(() => { void restore(); }, []);
  useEffect(() => { function expired() { setSession(null); void refreshCsrf().catch(() => {}); } window.addEventListener('session-expired', expired); return () => window.removeEventListener('session-expired', expired); }, []);
  async function logout() { try { await post('/auth/logout'); setSession(null); await refreshCsrf(); } catch (err) { setError(errorText(err)); } }
  if (loading) return <main className="boot-screen" role="status"><span className="brand">LALAMOG</span><p>Getting your corner ready…</p></main>;
  if (error && !session) return <main className="boot-screen panel"><p role="alert" className="error">{error}</p><button onClick={() => void restore()}>Retry connection</button><Link to="/preview">Explore the design preview</Link></main>;
  if (!session) return <Auth onLogin={setSession}/>;
  return <AppShell logout={() => void logout()}><div key={session.userId}>{error && <p className="error" role="alert">{error}</p>}<Routes>
    <Route path="/" element={<Discover/>}/><Route path="/matches/*" element={<Matches session={session}/>}/><Route path="/sessions" element={<Fights session={session}/>}/>
    <Route path="/profile" element={<ProfileEditor/>}/><Route path="/map" element={<FightMap/>}/><Route path="*" element={<Navigate to="/" replace/>}/>
  </Routes></div></AppShell>;
}
export default function App() { return <BrowserRouter><Routes><Route path="/preview" element={<AppShell preview><Discover preview/></AppShell>}/><Route path="/*" element={<Application/>}/></Routes></BrowserRouter>; }
