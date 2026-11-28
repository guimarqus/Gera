import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, expect, it, vi } from 'vitest';
import NotificationsPage from '../pages/NotificationsPage.jsx';
import { api } from '../services/api.js';

vi.mock('../services/api.js', () => ({ api: { notificacoes: vi.fn(), marcarTodasNotificacoesLidas: vi.fn(), marcarNotificacaoLida: vi.fn() } }));
afterEach(cleanup);

it('lista notificações e permite marcar todas como lidas', async () => {
  api.notificacoes.mockResolvedValue({ itens: [{ id: 4, titulo: 'Nova solicitação', mensagem: 'Você recebeu um pedido.', tipo: 1, link: '/solicitacoes', lida: false, criadoEm: '2026-11-28T12:00:00Z' }], naoLidas: 1, pagina: 1, totalPaginas: 1 });
  api.marcarTodasNotificacoesLidas.mockResolvedValue(null);
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(<QueryClientProvider client={client}><MemoryRouter><NotificationsPage /></MemoryRouter></QueryClientProvider>);
  expect(await screen.findByText('Você recebeu um pedido.')).toBeInTheDocument();
  fireEvent.click(screen.getByRole('button', { name: /Marcar todas/ }));
  await waitFor(() => expect(api.marcarTodasNotificacoesLidas).toHaveBeenCalled());
});
