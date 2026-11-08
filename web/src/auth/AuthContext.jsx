import { createContext, useContext, useEffect, useState } from 'react';
import { api, setAccessToken } from '../services/api.js';

const AuthContext = createContext(null);
export function AuthProvider({ children }) {
  const [token, setToken] = useState(null); const [user, setUser] = useState(null); const [ready, setReady] = useState(false);
  function applyToken(value) { setAccessToken(value); setToken(value); }
  async function login(result) { applyToken(result.accessToken); try { setUser(await api.me()); } catch { setUser(null); } }
  async function logout() { try { await api.logout(); } finally { applyToken(null); setUser(null); } }
  useEffect(() => { let active = true; api.refresh().then(async result => { if (!active) return; applyToken(result.accessToken); try { setUser(await api.me()); } catch {} }).catch(() => {}).finally(() => active && setReady(true)); return () => { active = false; }; }, []);
  return <AuthContext.Provider value={{ token, user, ready, login, logout }}>{children}</AuthContext.Provider>;
}
export const useAuth = () => useContext(AuthContext);
