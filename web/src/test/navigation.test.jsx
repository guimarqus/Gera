import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { AuthContext } from '../auth/AuthContext.jsx';
import AppShell from '../layout/AppShell.jsx';
import ProtectedRoute from '../routing/ProtectedRoute.jsx';
import CategoriesPage from '../pages/CategoriesPage.jsx';
import { api } from '../services/api.js';

vi.mock('../services/api.js', () => ({ api: { categories: vi.fn() } }));
const auth = (overrides = {}) => ({ token: 'access', ready: true, user: { nome: 'Ana Souza' }, activeRole: 'Cliente', chooseRole: vi.fn(), logout: vi.fn(), ...overrides });
function Location() { const location = useLocation(); return <output data-testid="location">{location.pathname}{location.state?.from || ''}</output>; }
function renderWithAuth(ui, value = auth(), initialEntries = ['/']) {
 return render(<AuthContext.Provider value={value}><MemoryRouter initialEntries={initialEntries}><Routes>{ui}</Routes></MemoryRouter></AuthContext.Provider>);
}
afterEach(() => cleanup());

describe('AppShell', () => {
 beforeEach(() => { window.matchMedia = vi.fn().mockImplementation(query => ({ matches: query === '(max-width: 767px)' ? window.__mobile : false, media: query, addEventListener: vi.fn(), removeEventListener: vi.fn() })); });
 it('mostra menu lateral desktop completo', () => { window.__mobile = false; renderWithAuth(<Route element={<AppShell />}><Route path="/" element={<p>Conteúdo</p>} /></Route>); const nav = screen.getByRole('navigation', { name: 'Navegação principal' }); expect(nav).toBeInTheDocument(); expect(nav).toHaveTextContent('Painel do Trabalhador'); expect(nav).toHaveTextContent('Configurações'); });
 it('mostra quatro destinos na navegação mobile e drawer com opções extras', () => { window.__mobile = true; renderWithAuth(<Route element={<AppShell />}><Route path="/" element={<p>Conteúdo</p>} /></Route>); expect(screen.getByRole('navigation', { name: 'Navegação principal no celular' }).querySelectorAll('a')).toHaveLength(4); fireEvent.click(screen.getByRole('button', { name: 'Abrir menu adicional' })); expect(screen.getByRole('navigation', { name: 'Menu adicional' })).toHaveTextContent('Painel do Trabalhador'); });
 it('alterna papel ativo pela preferência de interface', () => { window.__mobile = false; const context = auth(); renderWithAuth(<Route element={<AppShell />}><Route path="/" element={<p>Conteúdo</p>} /></Route>, context); fireEvent.click(screen.getAllByRole('button', { name: 'Prestador' })[0]); expect(context.chooseRole).toHaveBeenCalledWith('Prestador'); });
});

describe('rotas e categorias', () => {
 it('redireciona para entrar e preserva a página original', async () => { renderWithAuth(<><Route element={<ProtectedRoute />}><Route path="/privada" element={<p>Privada</p>} /></Route><Route path="/entrar" element={<Location />} /></>, auth({ token: null }), ['/privada?aba=1']); await waitFor(() => expect(screen.getByTestId('location')).toHaveTextContent('/entrar/privada?aba=1')); });
 it('renderiza categorias e subcategorias vindas da API', async () => { api.categories.mockResolvedValue([{ id: 1, nome: 'Casa', subcategorias: [{ id: 2, nome: 'Elétrica', subcategorias: [] }] }]); const client = new QueryClient({ defaultOptions: { queries: { retry: false } } }); render(<QueryClientProvider client={client}><MemoryRouter><CategoriesPage /></MemoryRouter></QueryClientProvider>); expect(await screen.findByRole('heading', { name: 'Casa' })).toBeInTheDocument(); expect(screen.getByRole('link', { name: 'Elétrica' })).toHaveAttribute('href', '/categorias/2'); });
});
