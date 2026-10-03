import { createContext, useContext, useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { BrowserRouter, NavLink, Navigate, Route, Routes } from 'react-router-dom'
import { api } from './api'
import type { Group, Message, Post, User } from './types'
import './App.css'

type AuthState = { user: User | null; login: (token: string, user: User) => void; logout: () => void; setUser: (user: User) => void }
const AuthContext = createContext<AuthState>(null!)
const useAuth = () => useContext(AuthContext)

function AsyncState({ loading, error, empty, children }: { loading: boolean; error?: string; empty?: boolean; children: React.ReactNode }) {
  if (loading) return <div className="state"><span className="spinner" /> Cargando…</div>
  if (error) return <div className="state error">{error}</div>
  if (empty) return <div className="state">Todavía no hay contenido aquí.</div>
  return <>{children}</>
}

function AuthPage() {
  const [registering, setRegistering] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const { login } = useAuth()
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setBusy(true); setError('')
    const form = new FormData(event.currentTarget)
    try {
      const result = registering
        ? await api.register({ email: form.get('email'), password: form.get('password'), displayName: form.get('name'), role: form.get('role') })
        : await api.login({ email: form.get('email'), password: form.get('password') })
      login(result.token, result.user)
    } catch (e) { setError(e instanceof Error ? e.message : 'Error de acceso.') } finally { setBusy(false) }
  }
  return <main className="auth-shell">
    <section className="auth-hero"><span className="eyebrow">COMUNIDAD UPT</span><h1>Conecta con tu universidad.</h1><p>Ideas, grupos académicos y conversaciones en un solo lugar seguro.</p><div className="hero-orbit"><span>✦</span></div></section>
    <section className="auth-card"><div className="brand"><span className="brand-mark">U</span> Campus Social</div><h2>{registering ? 'Crea tu cuenta' : 'Qué bueno verte'}</h2><p className="muted">{registering ? 'Únete a la comunidad universitaria.' : 'Ingresa para continuar a tu comunidad.'}</p>
      <form onSubmit={submit}>
        {registering && <><label>Nombre completo<input required minLength={2} maxLength={100} name="name" placeholder="Andrea Ramos" /></label><label>Rol<select name="role"><option value="student">Estudiante</option><option value="teacher">Docente</option><option value="staff">Personal universitario</option></select></label></>}
        <label>Correo institucional<input required type="email" name="email" placeholder="nombre@universidad.edu" /></label>
        <label>Contraseña<input required minLength={10} type="password" name="password" placeholder="Mínimo 10 caracteres" /></label>
        {error && <div className="inline-error">{error}</div>}<button className="primary" disabled={busy}>{busy ? 'Procesando…' : registering ? 'Crear cuenta' : 'Ingresar'}</button>
      </form><button className="link-button" onClick={() => { setRegistering(!registering); setError('') }}>{registering ? 'Ya tengo una cuenta' : 'Crear una cuenta nueva'}</button>
    </section>
  </main>
}

function Layout() {
  const { user, logout } = useAuth()
  const links = [['/', '⌂', 'Inicio'], ['/search', '⌕', 'Buscar'], ['/groups', '◉', 'Grupos'], ['/messages', '✉', 'Mensajes'], ['/profile', '♙', 'Mi espacio']]
  return <div className="app-shell"><aside><div className="brand"><span className="brand-mark">U</span><span>Campus Social</span></div><nav>{links.map(([to, icon, text]) => <NavLink key={to} to={to} end={to === '/'}><b>{icon}</b><span>{text}</span></NavLink>)}{user?.role === 'administrator' && <NavLink to="/admin"><b>⚙</b><span>Administración</span></NavLink>}</nav><div className="account"><div className="avatar">{user?.displayName.slice(0, 2).toUpperCase()}</div><div><strong>{user?.displayName}</strong><small>{user?.role}</small></div><button title="Cerrar sesión" onClick={logout}>↪</button></div></aside><section className="content"><header className="mobile-header"><div className="brand"><span className="brand-mark">U</span> Campus</div><button onClick={logout}>Salir</button></header><Routes><Route path="/" element={<Feed />} /><Route path="/search" element={<Search />} /><Route path="/groups" element={<Groups />} /><Route path="/messages" element={<Messages />} /><Route path="/profile" element={<Profile />} /><Route path="/admin" element={<Admin />} /><Route path="*" element={<Navigate to="/" />} /></Routes></section></div>
}

function Feed() {
  const { user } = useAuth(); const [posts, setPosts] = useState<Post[]>([]); const [text, setText] = useState(''); const [loading, setLoading] = useState(true); const [error, setError] = useState('')
  const load = () => { setLoading(true); api.posts().then(x => setPosts(x.items)).catch(e => setError(e.message)).finally(() => setLoading(false)) }
  useEffect(() => { api.posts().then(x => setPosts(x.items)).catch(e => setError(e.message)).finally(() => setLoading(false)) }, [])
  async function publish(e: FormEvent) { e.preventDefault(); if (!text.trim()) return; try { await api.createPost(text); setText(''); load() } catch (x) { setError(x instanceof Error ? x.message : 'Error') } }
  return <Page title="Inicio" subtitle={`Hola, ${user?.displayName.split(' ')[0]}. Esto está pasando en tu campus.`}><form className="composer" onSubmit={publish}><div className="avatar">{user?.displayName.slice(0, 2).toUpperCase()}</div><textarea value={text} onChange={e => setText(e.target.value)} maxLength={2000} placeholder="Comparte una idea, recurso o noticia…" /><div className="composer-actions"><span>{text.length}/2000</span><button className="primary">Publicar</button></div></form><AsyncState loading={loading} error={error} empty={!posts.length}><div className="stack">{posts.map(post => <PostCard key={post.id} post={post} refresh={load} />)}</div></AsyncState></Page>
}

function PostCard({ post, refresh }: { post: Post; refresh: () => void }) {
  const { user } = useAuth(); const [comment, setComment] = useState(''); const [open, setOpen] = useState(false)
  return <article className="card post"><div className="post-head"><div className="avatar soft">{post.authorName.slice(0, 2).toUpperCase()}</div><div><strong>{post.authorName}</strong><small>{new Date(post.createdAt).toLocaleString()}</small></div>{(post.authorId === user?.id || user?.role === 'administrator') && <button className="ghost danger" onClick={() => api.deletePost(post.id).then(refresh)}>Eliminar</button>}</div><p>{post.content}</p><div className="post-actions"><button onClick={() => api.react(post.id).then(refresh)}>♡ {post.reactionCount}</button><button onClick={() => setOpen(!open)}>◌ {post.commentCount} comentarios</button></div>{open && <form className="comment-box" onSubmit={e => { e.preventDefault(); api.comment(post.id, comment).then(() => { setComment(''); setOpen(false); refresh() }) }}><input required maxLength={1000} value={comment} onChange={e => setComment(e.target.value)} placeholder="Escribe un comentario…" /><button>Enviar</button></form>}</article>
}

function Groups() {
  const [groups, setGroups] = useState<Group[]>([]); const [loading, setLoading] = useState(true); const [error, setError] = useState(''); const [creating, setCreating] = useState(false)
  const load = () => api.groups().then(x => setGroups(x.items)).catch(e => setError(e.message)).finally(() => setLoading(false)); useEffect(() => { void load() }, [])
  async function create(e: FormEvent<HTMLFormElement>) { e.preventDefault(); const data = new FormData(e.currentTarget); await api.createGroup(String(data.get('name')), String(data.get('description'))); setCreating(false); load() }
  return <Page title="Grupos" subtitle="Encuentra personas con tus mismos intereses." action={<button className="primary" onClick={() => setCreating(!creating)}>+ Crear grupo</button>}>{creating && <form className="card form-grid" onSubmit={create}><label>Nombre<input required minLength={2} name="name" /></label><label>Descripción<textarea required name="description" /></label><button className="primary">Guardar grupo</button></form>}<AsyncState loading={loading} error={error} empty={!groups.length}><div className="grid">{groups.map(g => <article className="card group" key={g.id}><div className="group-icon">{g.name.slice(0, 1).toUpperCase()}</div><h3>{g.name}</h3><p>{g.description}</p><small>{g.memberCount} miembros · {g.postCount} publicaciones</small><button disabled={g.isMember} onClick={() => api.joinGroup(g.id).then(load)}>{g.isMember ? 'Ya eres miembro' : 'Unirme'}</button></article>)}</div></AsyncState></Page>
}

function Search() {
  const [q, setQ] = useState(''); const [result, setResult] = useState<{ users: User[]; groups: Group[]; posts: Post[] }>(); const [loading, setLoading] = useState(false); const [error, setError] = useState('')
  async function submit(e: FormEvent) { e.preventDefault(); setLoading(true); setError(''); try { const r = await api.search(q); setResult({ users: r.users.items, groups: r.groups.items, posts: r.posts.items }) } catch (x) { setError(x instanceof Error ? x.message : 'Error') } finally { setLoading(false) } }
  return <Page title="Buscar" subtitle="Personas, grupos y publicaciones de la comunidad."><form className="searchbar" onSubmit={submit}><input required value={q} onChange={e => setQ(e.target.value)} placeholder="¿Qué estás buscando?" /><button className="primary">Buscar</button></form><AsyncState loading={loading} error={error}>{result && <div className="search-results"><h2>Personas <span>{result.users.length}</span></h2><div className="row-list">{result.users.map(u => <div className="mini" key={u.id}><div className="avatar soft">{u.displayName.slice(0, 2).toUpperCase()}</div><div><strong>{u.displayName}</strong><small>{u.program || u.role}</small></div></div>)}</div><h2>Grupos <span>{result.groups.length}</span></h2>{result.groups.map(g => <div className="mini" key={g.id}><strong>{g.name}</strong><small>{g.description}</small></div>)}<h2>Publicaciones <span>{result.posts.length}</span></h2>{result.posts.map(p => <PostCard key={p.id} post={p} refresh={() => submit({ preventDefault() {} } as FormEvent)} />)}</div>}</AsyncState></Page>
}

function Profile() {
  const auth = useAuth(); const [stats, setStats] = useState<{ postCount: number; groupCount: number; unreadMessages: number }>(); const [error, setError] = useState(''); const user = auth.user!
  useEffect(() => { api.dashboard().then(x => setStats(x)).catch(e => setError(e.message)) }, [])
  async function save(e: FormEvent<HTMLFormElement>) { e.preventDefault(); const data = new FormData(e.currentTarget); try { const updated = await api.updateUser(user.id, { displayName: data.get('displayName'), bio: data.get('bio'), program: data.get('program'), academicYear: data.get('academicYear') }); auth.setUser(updated) } catch (x) { setError(x instanceof Error ? x.message : 'Error') } }
  return <Page title="Mi espacio" subtitle="Tu perfil académico y actividad."><div className="profile-banner"><div className="avatar large">{user.displayName.slice(0, 2).toUpperCase()}</div><div><h2>{user.displayName}</h2><p>{user.email}</p><span className="pill">{user.role}</span></div></div><div className="stats"><div><strong>{stats?.postCount ?? '—'}</strong><span>Publicaciones</span></div><div><strong>{stats?.groupCount ?? '—'}</strong><span>Grupos</span></div><div><strong>{stats?.unreadMessages ?? '—'}</strong><span>Mensajes nuevos</span></div></div><form className="card form-grid" onSubmit={save}><h2>Información del perfil</h2><label>Nombre<input required name="displayName" defaultValue={user.displayName} /></label><label>Programa académico<input name="program" defaultValue={user.program} /></label><label>Año o ciclo<input name="academicYear" defaultValue={user.academicYear} /></label><label>Biografía<textarea name="bio" maxLength={500} defaultValue={user.bio} /></label>{error && <div className="inline-error">{error}</div>}<button className="primary">Guardar cambios</button></form></Page>
}

function Messages() {
  const { user } = useAuth(); const [people, setPeople] = useState<User[]>([]); const [selected, setSelected] = useState<User>(); const [messages, setMessages] = useState<Message[]>([]); const [text, setText] = useState(''); const [error, setError] = useState('')
  useEffect(() => { api.users().then(x => setPeople(x.items.filter(p => p.id !== user?.id))).catch(e => setError(e.message)) }, [user?.id])
  useEffect(() => { if (selected) api.conversation(selected.id).then(x => setMessages(x.items)).catch(e => setError(e.message)) }, [selected])
  async function send(e: FormEvent) { e.preventDefault(); if (!selected || !text.trim()) return; await api.sendMessage(selected.id, text); setText(''); setMessages((await api.conversation(selected.id)).items) }
  return <Page title="Mensajes" subtitle="Conversaciones privadas dentro del campus."><div className="messenger card"><div className="contacts"><h3>Personas</h3>{people.map(p => <button className={selected?.id === p.id ? 'active' : ''} onClick={() => setSelected(p)} key={p.id}><div className="avatar soft">{p.displayName.slice(0, 2).toUpperCase()}</div><span>{p.displayName}<small>{p.role}</small></span></button>)}</div><div className="conversation">{selected ? <><div className="conversation-head"><strong>{selected.displayName}</strong><small>{selected.program || selected.role}</small></div><div className="messages">{messages.length ? messages.map(m => <div key={m.id} className={m.senderId === user?.id ? 'bubble mine' : 'bubble'}>{m.body}<small>{new Date(m.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</small></div>) : <div className="state">Inicia la conversación.</div>}</div><form onSubmit={send} className="message-form"><input required value={text} onChange={e => setText(e.target.value)} placeholder="Escribe un mensaje…" /><button className="primary">Enviar</button></form></> : <div className="state">Selecciona una persona para conversar.</div>}</div></div>{error && <div className="inline-error">{error}</div>}</Page>
}

function Admin() {
  const { user } = useAuth(); const [stats, setStats] = useState<Record<string, number>>(); const [users, setUsers] = useState<User[]>([]); const [error, setError] = useState('')
  const load = () => { api.adminDashboard().then(setStats).catch(e => setError(e.message)); api.users('?pageSize=100').then(x => setUsers(x.items)).catch(e => setError(e.message)) }; useEffect(load, [])
  if (user?.role !== 'administrator') return <Navigate to="/" />
  return <Page title="Administración" subtitle="Supervisa la salud y seguridad de la comunidad."><div className="stats admin-stats">{stats && Object.entries(stats).map(([key, value]) => <div key={key}><strong>{value}</strong><span>{key}</span></div>)}</div><div className="card table-wrap"><table><thead><tr><th>Usuario</th><th>Rol</th><th>Estado</th><th>Acción</th></tr></thead><tbody>{users.map(u => <tr key={u.id}><td><strong>{u.displayName}</strong><small>{u.email}</small></td><td>{u.role}</td><td><span className={`pill ${u.isActive ? '' : 'off'}`}>{u.isActive ? 'Activo' : 'Suspendido'}</span></td><td><button disabled={u.id === user.id} onClick={() => api.adminUser(u.id, { isActive: !u.isActive }).then(load)}>{u.isActive ? 'Suspender' : 'Activar'}</button></td></tr>)}</tbody></table></div>{error && <div className="inline-error">{error}</div>}</Page>
}

function Page({ title, subtitle, action, children }: { title: string; subtitle: string; action?: React.ReactNode; children: React.ReactNode }) { return <main className="page"><div className="page-head"><div><h1>{title}</h1><p>{subtitle}</p></div>{action}</div>{children}</main> }

export default function App() {
  const [user, setUserState] = useState<User | null>(() => { try { return JSON.parse(localStorage.getItem('university-social-user') || 'null') } catch { return null } })
  const login = (token: string, nextUser: User) => { localStorage.setItem('university-social-token', token); localStorage.setItem('university-social-user', JSON.stringify(nextUser)); setUserState(nextUser) }
  const logout = () => { localStorage.removeItem('university-social-token'); localStorage.removeItem('university-social-user'); setUserState(null) }
  const setUser = (nextUser: User) => { localStorage.setItem('university-social-user', JSON.stringify(nextUser)); setUserState(nextUser) }
  return <AuthContext.Provider value={{ user, login, logout, setUser }}><BrowserRouter>{user ? <Layout /> : <AuthPage />}</BrowserRouter></AuthContext.Provider>
}
