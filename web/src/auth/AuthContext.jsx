import { createContext, useContext, useEffect, useState } from 'react';
import { api, setAccessToken } from '../services/api.js';

export const AuthContext = createContext(null);
export function AuthProvider({ children }) {
  const [token, setToken] = useState(null); const [user, setUser] = useState(null); const [ready, setReady] = useState(false);
  const [activeRole, setActiveRole] = useState(() => window.localStorage.getItem('gera.activeRole') || 'Cliente');
  function applyToken(value) { setAccessToken(value); setToken(value); }
  function saveProfile(profile) {
    setUser(profile);
    if (!window.localStorage.getItem('gera.activeRole') && profile?.papelAtivo) setActiveRole(profile.papelAtivo);
  }
  async function loadProfile() {
    try { saveProfile(await api.profileSummary()); }
    catch { try { saveProfile(await api.me()); } catch { setUser(null); } }
  }
  async function login(result) { applyToken(result.accessToken); await loadProfile(); }
  async function logout() { try { await api.logout(); } finally { applyToken(null); setUser(null); } }
  function chooseRole(role) { setActiveRole(role); window.localStorage.setItem('gera.activeRole', role); }
  useEffect(() => { let active = true; api.refresh().then(async result => { if (!active) return; applyToken(result.accessToken); await loadProfile(); }).catch(() => {}).finally(() => active && setReady(true)); return () => { active = false; }; }, []);
  return <AuthContext.Provider value={{ token, user, ready, login, logout, activeRole, chooseRole }}>{children}</AuthContext.Provider>;
}
export const useAuth = () => useContext(AuthContext);
