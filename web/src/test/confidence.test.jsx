import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import AvaliacaoSolicitacao from '../pages/AvaliacaoSolicitacao.jsx';
import { api } from '../services/api.js';

vi.mock('../services/api.js', () => ({ api: { minhaAvaliacao: vi.fn(), avaliarSolicitacao: vi.fn() } }));
afterEach(cleanup);

it('permite avaliar uma solicitacao concluida e guarda a resposta no cache', async () => {
  api.minhaAvaliacao.mockResolvedValue(null);
  api.avaliarSolicitacao.mockResolvedValue({ nota: 4, comentario: 'Bom atendimento.' });
  const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  render(<QueryClientProvider client={client}><AvaliacaoSolicitacao item={{ id: 7, ofertaTitulo: 'Instalacao' }} /></QueryClientProvider>);
  fireEvent.change(await screen.findByLabelText('Nota para Instalacao'), { target: { value: '4' } });
  fireEvent.change(screen.getByLabelText('Comentário opcional'), { target: { value: 'Bom atendimento.' } });
  fireEvent.click(screen.getByRole('button', { name: 'Enviar avaliação' }));
  expect(await screen.findByRole('status')).toHaveTextContent('Sua avaliação: 4/5');
  expect(api.avaliarSolicitacao).toHaveBeenCalledWith(7, { nota: 4, comentario: 'Bom atendimento.' });
});
