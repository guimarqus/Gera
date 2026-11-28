import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import AssistenteDiagnosticoPage from '../pages/AssistenteDiagnosticoPage.jsx';
import { api } from '../services/api.js';

vi.mock('../services/api.js', () => ({ api: { diagnosticarProblema: vi.fn() } }));
afterEach(cleanup);

it('exige consentimento e mostra categorias e ofertas retornadas pelo Gera', async () => {
  api.diagnosticarProblema.mockResolvedValue({
    resumo: 'Um encanador pode avaliar o vazamento.',
    categorias: [{ id: 7, nome: 'Hidráulica' }],
    sequenciaSugerida: ['Feche o registro de água.'],
    requerCautela: false,
    aviso: null,
    ofertas: [{ id: 32, categoria: 'Hidráulica', titulo: 'Reparos hidráulicos', prestador: 'Ana Souza', cidade: 'Manaus' }],
  });
  const client = new QueryClient({ defaultOptions: { mutations: { retry: false } } });
  render(<QueryClientProvider client={client}><MemoryRouter><AssistenteDiagnosticoPage /></MemoryRouter></QueryClientProvider>);

  fireEvent.change(screen.getByLabelText('O que aconteceu ou precisa ser feito?'), { target: { value: 'A torneira da cozinha está vazando.' } });
  fireEvent.change(screen.getByLabelText(/Cidade ou bairro/), { target: { value: 'Manaus' } });
  const submit = screen.getByRole('button', { name: /Sugerir profissionais/ });
  expect(submit).toBeDisabled();
  fireEvent.click(screen.getByRole('checkbox'));
  fireEvent.click(submit);

  await waitFor(() => expect(api.diagnosticarProblema).toHaveBeenCalled());
  expect(api.diagnosticarProblema.mock.calls[0][0]).toEqual({
    problema: 'A torneira da cozinha está vazando.', cidade: 'Manaus', consentiuCompartilhamento: true,
  });
  expect(await screen.findByRole('heading', { name: 'Profissionais encontrados' })).toBeInTheDocument();
  expect(screen.getByRole('link', { name: 'Reparos hidráulicos' })).toHaveAttribute('href', '/ofertas/32');
});
