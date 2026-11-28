import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, expect, it, vi } from 'vitest';
import { AuthContext } from '../auth/AuthContext.jsx';
import HomePage from '../pages/HomePage.jsx';
import { api } from '../services/api.js';

vi.mock('../services/api.js', () => ({ api: {
  categories: vi.fn(), feedOfertas: vi.fn(), favoritarOferta: vi.fn(), desfavoritarOferta: vi.fn(),
} }));

afterEach(cleanup);

it('carrega o feed, envia filtros e permite salvar um favorito', async () => {
  api.categories.mockResolvedValue([{ id: 4, nome: 'Casa', subcategorias: [{ id: 8, nome: 'Elétrica', subcategorias: [] }] }]);
  api.feedOfertas.mockResolvedValue({
    pagina: 1, tamanho: 12, total: 1, totalPaginas: 1,
    ofertas: [{ id: 42, categoriaId: 8, categoria: 'Elétrica', titulo: 'Instalação elétrica',
      descricao: 'Instalação e manutenção residencial.', tipoPreco: 5, valor: null, cidade: 'Manaus', prestadorId: 9,
      bairro: 'Centro', fotoCapaUrl: null, prestador: 'Ana Souza', totalAvaliacoes: 0,
      distanciaKm: null, ehFavorita: false }],
  });
  api.favoritarOferta.mockResolvedValue(null);
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(<AuthContext.Provider value={{ token: 'sessao-teste' }}>
    <QueryClientProvider client={client}><MemoryRouter><HomePage /></MemoryRouter></QueryClientProvider>
  </AuthContext.Provider>);

  expect(await screen.findByRole('heading', { name: 'Instalação elétrica' })).toBeInTheDocument();
  fireEvent.change(screen.getByLabelText('O que você precisa?'), { target: { value: 'instalação' } });
  fireEvent.change(screen.getByLabelText('Cidade ou bairro'), { target: { value: 'Manaus' } });
  fireEvent.change(screen.getByLabelText('Disponibilidade'), { target: { value: 'fim de semana' } });
  fireEvent.click(screen.getByRole('button', { name: 'Buscar' }));
  await waitFor(() => expect(api.feedOfertas).toHaveBeenLastCalledWith(expect.objectContaining({ busca: 'instalação', cidade: 'Manaus', disponibilidade: 'fim de semana' })));
  fireEvent.click(await screen.findByRole('button', { name: 'Salvar Instalação elétrica nos favoritos' }));
  await waitFor(() => expect(api.favoritarOferta).toHaveBeenCalledWith(42));
});
