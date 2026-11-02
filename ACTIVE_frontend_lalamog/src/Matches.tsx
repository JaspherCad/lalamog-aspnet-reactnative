import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api, csrf, errorText, post } from './api';
import { Avatar, fighterName, Icon, PageHeading } from './ui';
import { styleLabels } from './fightingStyles';
import type { Fight, Match, Message, Session } from './types';

function Conversation({ match, session }: { match: Match; session: Session }) {
  const [messages, setMessages] = useState<Message[]>([]);
  const [draft, setDraft] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [connectionStatus, setConnectionStatus] = useState('Connecting...');
  const [loading, setLoading] = useState(true);
  const [olderAvailable, setOlderAvailable] = useState(false);
  const [olderBusy, setOlderBusy] = useState(false);
  const [fights, setFights] = useState<Fight[] | null>(null);
  const [fightError, setFightError] = useState('');
  useEffect(() => {
    const controller = new AbortController();
    void api<Fight[]>('/fights', { signal: controller.signal }).then(values => setFights(values.filter(f => f.matchId === match.id))).catch(err => { if (!controller.signal.aborted) setFightError(errorText(err)); });
    return () => controller.abort();
  }, [match.id]);
  const currentFight = fights?.find(f => ['scheduled', 'confirmed'].includes(f.status));
  function merge(incoming: Message[]) {
    setMessages(current => [...new Map([...current, ...incoming].map(m => [m.id, m])).values()].sort((a, b) => a.id - b.id));
  }
  useEffect(() => {
    const controller = new AbortController();
    let newest = 0;
    let retryTimer: ReturnType<typeof setTimeout>;
    let stopped = false;
    let recovering = false;
    let recoveryRequested = false;
    let connection: ReturnType<HubConnectionBuilder['build']> | undefined;
    async function recover() {
      if (recovering) { recoveryRequested = true; return; }
      recovering = true;
      try {
        do {
          recoveryRequested = false;
          const initial = newest === 0;
          let page: Message[];
          do {
            page = await api<Message[]>(`/matches/${match.id}/messages${newest ? `?after=${newest}` : ''}`, { signal: controller.signal });
            if (stopped) return;
            merge(page);
            newest = Math.max(newest, ...page.map(m => m.id), 0);
            if (initial) setOlderAvailable(page.length === 100);
          } while (!initial && page.length === 100);
        } while (recoveryRequested);
        setError('');
      } catch (err) { if (!stopped) setError(errorText(err)); }
      finally { recovering = false; if (!stopped) setLoading(false); }
    }
    async function connect() {
      try {
        connection = new HubConnectionBuilder().withUrl('/hubs/messages', {
          headers: { 'X-CSRF-TOKEN': await csrf() }, withCredentials: true
        }).withAutomaticReconnect().configureLogging(LogLevel.Warning).build();
        // Use events as a wake-up signal. History supplies a gap-free, ordered cursor.
        connection.on('MessageReceived', (message: Message) => {
          if (message.matchId === match.id) void recover();
        });
        connection.onreconnecting(() => { if (!stopped) setConnectionStatus('Reconnecting...'); });
        connection.onreconnected(() => { if (!stopped) { setConnectionStatus('Live'); void recover(); } });
        connection.onclose(() => {
          if (!stopped) { setConnectionStatus('Offline - retrying'); retryTimer = setTimeout(() => void start(), 3000); }
        });
        await start();
      } catch (err) {
        if (!stopped) { setError(errorText(err)); retryTimer = setTimeout(() => void connect(), 3000); }
      }
    }
    async function start() {
      if (stopped || !connection) return;
      try { await connection.start(); if (!stopped) { setConnectionStatus('Live'); await recover(); } }
      catch {
        if (!stopped) { setConnectionStatus('Offline - retrying'); retryTimer = setTimeout(() => void start(), 3000); }
      }
    }
    function visible() { if (!document.hidden) void recover(); }
    document.addEventListener('visibilitychange', visible);
    void recover(); void connect();
    return () => {
      stopped = true; controller.abort(); clearTimeout(retryTimer);
      document.removeEventListener('visibilitychange', visible);
      void connection?.stop();
    };
  }, [match.id]);
  async function older() {
    setOlderBusy(true);
    try {
      const data = await api<Message[]>(`/matches/${match.id}/messages?before=${messages[0].id}`);
      merge(data); setOlderAvailable(data.length === 100);
    } catch (err) { setError(errorText(err)); } finally { setOlderBusy(false); }
  }
  async function send(event: FormEvent) {
    event.preventDefault(); if (!draft.trim() || busy) return;
    const submitted = draft;
    setBusy(true); setError('');
    try { merge([await post<Message>(`/matches/${match.id}/messages`, { content: submitted })]); setDraft(current => current === submitted ? '' : current); }
    catch (err) { setError(errorText(err)); } finally { setBusy(false); }
  }
  return <section className="panel conversation"><div className="conversation-heading"><Link className="secondary button-link" to="/matches" aria-label="Back to Inbox">←</Link><Avatar profile={match.opponent} /><div><h2 className="fighter-name">{fighterName(match.opponent)}</h2>{match.opponent.fighterName && <p className="fighter-alias">“{match.opponent.nickname}”</p>}<small className="muted">{styleLabels(match.opponent.fightingStyle)}</small></div></div>
    <p className="connection-status" role="status"><span className={`connection-dot ${connectionStatus === 'Live' ? 'is-live' : ''}`}/>{connectionStatus} · Private conversation</p>
    <div className="conversation-fight"><div><p className="eyebrow">{currentFight ? 'Your next round' : 'Another story to tell'}</p><h3>{currentFight ? currentFight.location : 'Make it a match.'}</h3><p className="small">{currentFight ? `${new Date(currentFight.scheduledAt).toLocaleString()} · ${currentFight.status}` : fightError ? 'Could not check your arrangements. Open your fights to retry.' : 'Choose a place and time together.'}</p></div>
      <Link className="button-link" to={`/sessions?matchId=${match.id}`}>{currentFight || fightError || fights === null ? 'View your fights' : 'Start the Match'}<Icon name="arrow"/></Link>
      {!!fights?.filter(f => !['scheduled', 'confirmed'].includes(f.status)).length && <Link className="text-action" to={`/sessions?matchId=${match.id}`}>Previous fights <Icon name="arrow"/></Link>}</div>
    {error && <p role="alert" className="error">{error}</p>}
    <div className="message-list" role="log" aria-label="Conversation" aria-live="polite">
      {olderAvailable && <button className="link" disabled={olderBusy} onClick={() => void older()}>Load older messages</button>}
      {loading ? <p>Loading messages...</p> : messages.length === 0 && <p className="muted">Matched. Your move—say hello.</p>}
      {messages.map(message => <article key={message.id} className={`bubble ${message.senderId === session.userId ? 'mine' : ''}`}>
        <p>{message.content}</p><time dateTime={message.createdAt}>{new Date(message.createdAt).toLocaleString()}</time>
      </article>)}
    </div>
    <form className="message-form" onSubmit={send}><label className="sr-only" htmlFor="message">Message</label><input id="message" value={draft} onChange={e => setDraft(e.target.value)} maxLength={2000} placeholder="Write a message…" required /><button disabled={busy || !draft.trim()}>{busy ? 'Sending...' : 'Send'}<Icon name="arrow"/></button></form>
  </section>;
}
export function Matches({ session }: { session: Session }) {
  const [matches, setMatches] = useState<Match[]>([]);
  const { '*': matchId } = useParams();
  const navigate = useNavigate();
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  async function load() {
    setLoading(true); setError('');
    try { setMatches(await api<Match[]>('/matches')); }
    catch (err) { setError(errorText(err)); } finally { setLoading(false); }
  }
  useEffect(() => { void load(); }, []);
  const selected = matches.find(m => m.id === Number(matchId));
  return <section className="inbox-page"><PageHeading eyebrow="Every match has a story" title="Inbox"><button className="secondary" disabled={loading} onClick={() => void load()}><Icon name="refresh"/>Refresh</button></PageHeading>
    {error && <p role="alert" className="error">{error}</p>}
    {loading ? <p role="status">Loading matches...</p> : selected ? <Conversation key={selected.id} match={selected} session={session} /> : matchId ? <p role="alert">Match unavailable. <Link to="/matches">Back to matches</Link></p> :
      <div className="match-list">{matches.map(match =>
        <button className="panel match-row" key={match.id} onClick={() => navigate(`/matches/${match.id}`)}><Avatar profile={match.opponent} /><span><strong className="fighter-name">{fighterName(match.opponent)}</strong>{match.opponent.fighterName && <span className="fighter-alias">“{match.opponent.nickname}”</span>}<small>{styleLabels(match.opponent.fightingStyle)} · {match.opponent.location || 'Location not added'}</small></span><Icon name="arrow"/></button>)}
        {!matches.length && !error && <div className="panel empty"><h2>A good match takes two</h2><p>Like fighters in Discover. Once the feeling is mutual, you can chat here.</p></div>}
      </div>}
  </section>;
}
