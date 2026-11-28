import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, expect, it, vi } from 'vitest';
import OfertaDetailPage from '../pages/OfertaDetailPage.jsx';
import PrestadorPublicPage from '../pages/PrestadorPublicPage.jsx';
import { api } from '../services/api.js';

vi.mock('../services/api.js', () => ({ api: { ofertaDetalhe: vi.fn(), prestador: vi.fn(), avaliacoesPrestador: vi.fn(), bloqueios: vi.fn(), denunciar: vi.fn(), bloquear: vi.fn(), desbloquear: vi.fn() } }));
vi.mock('../auth/AuthContext.jsx', () => ({ useAuth: () => ({ user: { id: 1 } }) }));
afterEach(cleanup);

function renderRoute(path, route, element) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={client}><MemoryRouter initialEntries={[path]}>
    <Routes><Route path={route} element={element} /></Routes>
  </MemoryRouter></QueryClientProvider>);
}

it('exibe detalhes públicos, galeria e link para o prestador', async () => {
  api.ofertaDetalhe.mockResolvedValue({
    id: 42, categoriaId: 4, categoria: 'Pintura', titulo: 'Pintura de parede',
    descricao: 'Pintura residencial de paredes internas e tetos.', tipoPreco: 5, valor: null,
    cidade: 'Manaus', bairro: 'Centro', raioKm: 20, disponibilidade: 'Segunda a sexta',
    formasPagamento: 'Pix', caracteristicas: { versaoModelo: 1, atributos: { ambiente: 'Residencial' } },
    tags: ['Reforma'], fotos: [
      { url: '/capa.webp', urlMiniatura: '/capa-mini.webp', ordem: 1, ehCapa: true },
      { url: '/parede.webp', urlMiniatura: '/parede-mini.webp', ordem: 2, ehCapa: false },
    ], prestadorId: 9, prestador: 'Ana Souza', prestadorFotoUrl: null,
    prestadorBiografia: 'Pintora residencial.', prestadorDesde: '2025-01-01T00:00:00Z',
  });
  renderRoute('/ofertas/42', '/ofertas/:id', <OfertaDetailPage />);
  expect(await screen.findByRole('heading', { name: 'Pintura de parede' })).toBeInTheDocument();
  expect(screen.getByText('Atende em até 20 km')).toBeInTheDocument();
  expect(screen.getByText('ambiente')).toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Solicitar orçamento' })).toBeInTheDocument();
  expect(screen.getByRole('link', { name: 'Ver perfil e outros serviços' })).toHaveAttribute('href', '/prestadores/9');
  fireEvent.click(screen.getByRole('button', { name: 'Mostrar foto 2 de 2' }));
  expect(screen.getByRole('img', { name: 'Foto de Pintura de parede' })).toHaveAttribute('src', '/parede.webp');
});

it('exibe o perfil público e liga as ofertas aos detalhes', async () => {
  api.avaliacoesPrestador.mockResolvedValue([]);
  api.bloqueios.mockResolvedValue([]);
  api.prestador.mockResolvedValue({
    id: 9, nome: 'Ana Souza', fotoUrl: null, biografia: 'Pintora residencial.',
    desde: '2025-01-01T00:00:00Z', notaMedia: 4.5, totalAvaliacoes: 2,
    totalOfertas: 1, pagina: 1, tamanho: 12, totalPaginas: 1,
    ofertas: [{ id: 42, categoriaId: 4, categoria: 'Pintura', titulo: 'Pintura de parede',
      descricao: 'Pintura residencial.', tipoPreco: 5, valor: null, cidade: 'Manaus',
      bairro: 'Centro', fotoCapaUrl: '/capa-mini.webp', publicadoEm: '2026-11-01T00:00:00Z' }],
  });
  renderRoute('/prestadores/9', '/prestadores/:id', <PrestadorPublicPage />);
  expect(await screen.findByRole('heading', { name: 'Ana Souza' })).toBeInTheDocument();
  expect(screen.getByText('4,5 (2 avaliações)')).toBeInTheDocument();
  expect(screen.getByRole('link', { name: 'Pintura de parede' })).toHaveAttribute('href', '/ofertas/42');
});
