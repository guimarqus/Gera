import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { expect, it, vi } from 'vitest';
import { AuthContext } from '../auth/AuthContext.jsx';
import OfertaEditorPage from '../pages/OfertaEditorPage.jsx';
import WorkerPage from '../pages/WorkerPage.jsx';
import { api } from '../services/api.js';

vi.mock('../services/api.js', () => ({
  api: { minhasOfertas: vi.fn(), criarOferta: vi.fn(), oferta: vi.fn(), categories: vi.fn(), editarOferta: vi.fn() },
}));

it('cria um rascunho, salva a oferta e retorna à listagem', async () => {
  const rascunho = { id: 42, status: 1, titulo: '', versao: 'versao-1', criadoEm: '2026-11-24T00:00:00Z' };
  api.minhasOfertas.mockResolvedValue([]);
  api.criarOferta.mockResolvedValue(rascunho);
  api.oferta.mockResolvedValue(rascunho);
  api.categories.mockResolvedValue([{ id: 4, nome: 'Pintura', subcategorias: [] }]);
  api.editarOferta.mockImplementation(async (_, dados) => ({ ...rascunho, ...dados, versao: 'versao-2' }));

  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(<AuthContext.Provider value={{ token: 'sessao-teste' }}>
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/trabalhador']}>
        <Routes>
          <Route path="/trabalhador" element={<WorkerPage />} />
          <Route path="/trabalhador/ofertas/:id" element={<OfertaEditorPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  </AuthContext.Provider>);

  fireEvent.click(await screen.findByRole('button', { name: 'Nova oferta' }));
  fireEvent.change(await screen.findByLabelText('Título'), { target: { value: 'Pintura interna' } });
  fireEvent.change(screen.getByLabelText('Categoria'), { target: { value: '4' } });
  fireEvent.click(screen.getByRole('button', { name: 'Salvar oferta' }));

  await waitFor(() => expect(api.editarOferta).toHaveBeenCalledWith(42, expect.objectContaining({
    titulo: 'Pintura interna', categoriaId: 4, versao: 'versao-1',
  })));
  expect(await screen.findByText('Oferta salva.')).toBeInTheDocument();
  fireEvent.click(screen.getByRole('link', { name: 'Voltar às ofertas' }));
  expect(await screen.findByRole('heading', { name: 'Minhas ofertas' })).toBeInTheDocument();
});

it('envia a edição mais recente com a versão retornada pelo primeiro autosave', async () => {
  const rascunho = { id: 42, status: 1, titulo: '', versao: 'versao-1' };
  api.oferta.mockResolvedValue(rascunho);
  api.categories.mockResolvedValue([]);
  api.editarOferta.mockReset();
  let concluirPrimeiro;
  api.editarOferta.mockImplementationOnce((_, dados) => new Promise(resolve => {
    concluirPrimeiro = () => resolve({ ...dados, versao: 'versao-2' });
  })).mockImplementationOnce(async (_, dados) => ({ ...dados, versao: 'versao-3' }));
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

  render(<AuthContext.Provider value={{ token: 'sessao-teste' }}>
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/trabalhador/ofertas/42']}>
        <Routes><Route path="/trabalhador/ofertas/:id" element={<OfertaEditorPage />} /></Routes>
      </MemoryRouter>
    </QueryClientProvider>
  </AuthContext.Provider>);

  const titulo = await screen.findByLabelText('Título');
  fireEvent.change(titulo, { target: { value: 'Pintura' } });
  await waitFor(() => expect(api.editarOferta).toHaveBeenCalledTimes(1), { timeout: 2000 });
  fireEvent.change(titulo, { target: { value: 'Pintura interna' } });
  expect(api.editarOferta).toHaveBeenCalledTimes(1);
  concluirPrimeiro();
  await waitFor(() => expect(api.editarOferta).toHaveBeenNthCalledWith(2, 42,
    expect.objectContaining({ titulo: 'Pintura interna', versao: 'versao-2' })), { timeout: 2000 });
  expect(titulo).toHaveValue('Pintura interna');
});
