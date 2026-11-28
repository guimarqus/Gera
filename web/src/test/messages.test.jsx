import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, expect, it, vi } from 'vitest';
import MessagesPage from '../pages/MessagesPage.jsx';
import { api } from '../services/api.js';

const hub = vi.hoisted(() => ({ on: vi.fn(), off: vi.fn(), onreconnected: vi.fn(), offreconnected: vi.fn(), start: vi.fn().mockResolvedValue(), stop: vi.fn().mockResolvedValue(), invoke: vi.fn().mockResolvedValue() }));
vi.mock('../services/api.js', () => ({ api: { conversas: vi.fn(), mensagens: vi.fn(), enviarMensagem: vi.fn(), marcarMensagensLidas: vi.fn().mockResolvedValue({ marcadas: 0 }) }, criarConexaoChat: () => hub }));
vi.mock('../auth/AuthContext.jsx', () => ({ useAuth: () => ({ token: 'token', user: { id: 2 } }) }));
afterEach(cleanup);

it('carrega uma conversa, exibe mensagens e envia texto', async () => {
  api.conversas.mockResolvedValue([{ solicitacaoId: 7, titulo: 'Reparo elétrico', outroUsuarioId: 3, outroUsuario: 'João', etapa: 2, ultimaMensagem: 'Olá', naoLidas: 0 }]);
  api.mensagens.mockResolvedValue([{ id: 11, solicitacaoId: 7, remetenteId: 3, remetente: 'João', tipo: 1, conteudo: 'Olá, como posso ajudar?', lida: false, dataHora: '2026-11-28T12:00:00Z' }]);
  api.enviarMensagem.mockResolvedValue({ id: 12, solicitacaoId: 7, remetenteId: 2, remetente: 'Ana', tipo: 1, conteudo: 'Pode vir amanhã?', lida: false, dataHora: '2026-11-28T12:01:00Z' });
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(<QueryClientProvider client={client}><MemoryRouter initialEntries={['/mensagens?id=7']}><MessagesPage /></MemoryRouter></QueryClientProvider>);
  expect(await screen.findByText('Olá, como posso ajudar?')).toBeInTheDocument();
  fireEvent.change(screen.getByRole('textbox', { name: 'Escreva sua mensagem' }), { target: { value: 'Pode vir amanhã?' } });
  fireEvent.click(screen.getByRole('button', { name: 'Enviar' }));
  await waitFor(() => expect(api.enviarMensagem).toHaveBeenCalledWith(7, 'Pode vir amanhã?'));
});
