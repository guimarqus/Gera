import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, expect, it, vi } from 'vitest';
import { AuthContext } from '../auth/AuthContext.jsx';
import OfertaEditorPage from '../pages/OfertaEditorPage.jsx';
import NovaOfertaPage from '../pages/NovaOfertaPage.jsx';
import WorkerPage from '../pages/WorkerPage.jsx';
import { api } from '../services/api.js';

vi.mock('../services/api.js', () => ({
  api: {
    minhasOfertas: vi.fn(), criarOferta: vi.fn(), oferta: vi.fn(), categories: vi.fn(),
    modeloOferta: vi.fn(), editarOferta: vi.fn(), fotosOferta: vi.fn(), publicarOferta: vi.fn(), adicionarFotoOferta: vi.fn(),
  },
}));

afterEach(cleanup);

it('cria um rascunho, salva a oferta e retorna à listagem', async () => {
  const rascunho = { id: 42, status: 1, titulo: '', versao: 'versao-1', criadoEm: '2026-11-24T00:00:00Z' };
  api.minhasOfertas.mockResolvedValue([]);
  api.criarOferta.mockResolvedValue(rascunho);
  api.oferta.mockResolvedValue(rascunho);
  api.categories.mockResolvedValue([{ id: 4, nome: 'Pintura', subcategorias: [] }]);
  api.modeloOferta.mockResolvedValue({ versao: 1, campos: [{ chave: 'ambiente', rotulo: 'Ambiente', tipo: 'selecao', opcoes: ['Interno', 'Externo'] }] });
  api.editarOferta.mockImplementation(async (_, dados) => ({ ...rascunho, ...dados, versao: 'versao-2' }));

  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(<AuthContext.Provider value={{ token: 'sessao-teste' }}>
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/trabalhador']}>
        <Routes>
          <Route path="/trabalhador" element={<WorkerPage />} />
          <Route path="/trabalhador/ofertas/nova" element={<NovaOfertaPage />} />
          <Route path="/trabalhador/ofertas/:id" element={<OfertaEditorPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  </AuthContext.Provider>);

  fireEvent.click(await screen.findByRole('button', { name: 'Nova oferta' }));
  fireEvent.change(await screen.findByLabelText('Título'), { target: { value: 'Pintura interna' } });
  fireEvent.change(screen.getByLabelText('Categoria'), { target: { value: '4' } });
  fireEvent.click(screen.getByRole('button', { name: 'Continuar' }));
  fireEvent.change(await screen.findByLabelText('Ambiente'), { target: { value: 'Interno' } });
  fireEvent.change(screen.getByLabelText('Tags'), { target: { value: 'Reforma, Apartamento' } });
  fireEvent.click(screen.getByRole('button', { name: 'Salvar oferta' }));

  await waitFor(() => expect(api.editarOferta).toHaveBeenCalledWith(42, expect.objectContaining({
    titulo: 'Pintura interna', categoriaId: 4, versao: 'versao-1', versaoModelo: 1,
    atributos: { ambiente: 'Interno' }, tags: ['Reforma', 'Apartamento'],
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

it('publica pela revisão quando os dados estão salvos e existe uma foto', async () => {
  const rascunho = {
    id: 42, status: 1, titulo: 'Pintura interna', versao: 'versao-1',
    descricao: 'Pintura de paredes, tetos e portas em apartamentos.',
    categoriaId: 4, latitude: 0, longitude: -60, raioKm: 15,
    tipoPreco: 5, disponibilidade: 'Segunda a sexta',
  };
  api.oferta.mockResolvedValue(rascunho);
  api.categories.mockResolvedValue([{ id: 4, nome: 'Pintura', subcategorias: [] }]);
  api.modeloOferta.mockResolvedValue({ versao: 1, campos: [] });
  api.fotosOferta.mockResolvedValue({ versao: 'versao-1', fotos: [{ id: 1, url: '/foto.webp', ehCapa: true }] });
  api.publicarOferta.mockResolvedValue({ ...rascunho, status: 2, versao: 'versao-2' });
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

  render(<AuthContext.Provider value={{ token: 'sessao-teste' }}>
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/trabalhador/ofertas/42']}>
        <Routes><Route path="/trabalhador/ofertas/:id" element={<OfertaEditorPage />} /></Routes>
      </MemoryRouter>
    </QueryClientProvider>
  </AuthContext.Provider>);

  await screen.findByRole('heading', { name: 'Dados do serviço' });
  for (let i = 0; i < 6; i++) fireEvent.click(screen.getByRole('button', { name: 'Continuar' }));
  await waitFor(() => expect(screen.getByRole('button', { name: 'Publicar oferta' })).toBeEnabled());
  fireEvent.click(screen.getByRole('button', { name: 'Publicar oferta' }));
  expect(await screen.findByText('Oferta publicada.')).toBeInTheDocument();
  expect(api.publicarOferta).toHaveBeenCalledWith(42);
});

it('envia duas fotos em sequência usando a versão atualizada da oferta', async () => {
  api.oferta.mockResolvedValue({ id: 42, status: 1, versao: 'versao-1' });
  api.categories.mockResolvedValue([]);
  api.fotosOferta.mockResolvedValue({ versao: 'versao-1', fotos: [] });
  const primeira = new File(['primeira'], 'primeira.png', { type: 'image/png' });
  const segunda = new File(['segunda'], 'segunda.jpg', { type: 'image/jpeg' });
  const foto1 = { id: 1, url: '/primeira.webp', ehCapa: true };
  const foto2 = { id: 2, url: '/segunda.webp', ehCapa: false };
  api.adicionarFotoOferta.mockResolvedValueOnce({ versao: 'versao-2', fotos: [foto1] })
    .mockResolvedValueOnce({ versao: 'versao-3', fotos: [foto1, foto2] });
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

  render(<AuthContext.Provider value={{ token: 'sessao-teste' }}>
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/trabalhador/ofertas/42']}>
        <Routes><Route path="/trabalhador/ofertas/:id" element={<OfertaEditorPage />} /></Routes>
      </MemoryRouter>
    </QueryClientProvider>
  </AuthContext.Provider>);

  await screen.findByRole('heading', { name: 'Dados do serviço' });
  for (let i = 0; i < 3; i++) fireEvent.click(screen.getByRole('button', { name: 'Continuar' }));
  fireEvent.change(await screen.findByLabelText('Adicionar fotos'), { target: { files: [primeira, segunda] } });
  expect(await screen.findByText('Fotos enviadas: 2 de 2')).toBeInTheDocument();
  expect(api.adicionarFotoOferta).toHaveBeenNthCalledWith(1, 42, primeira, 'versao-1');
  expect(api.adicionarFotoOferta).toHaveBeenNthCalledWith(2, 42, segunda, 'versao-2');
});
