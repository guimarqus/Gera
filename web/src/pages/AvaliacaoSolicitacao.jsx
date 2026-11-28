import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Button } from '../design/components.jsx';
import { api } from '../services/api.js';

export default function AvaliacaoSolicitacao({ item }) {
  const [nota, setNota] = useState('5');
  const [comentario, setComentario] = useState('');
  const cache = useQueryClient();
  const review = useQuery({ queryKey: ['minha-avaliacao', item.id], queryFn: () => api.minhaAvaliacao(item.id) });
  const mutation = useMutation({ mutationFn: () => api.avaliarSolicitacao(item.id, { nota: Number(nota), comentario: comentario || null }), onSuccess: data => cache.setQueryData(['minha-avaliacao', item.id], data) });
  if (review.isPending) return <p>Carregando avaliação…</p>;
  if (review.isError) return <p role="alert">{review.error.message}</p>;
  if (review.data) return <p role="status">Sua avaliação: {review.data.nota}/5{review.data.comentario ? ` · ${review.data.comentario}` : ''}</p>;
  return <form className="kanban-proposta" onSubmit={event => { event.preventDefault(); mutation.mutate(); }}>
    <label>Nota de 1 a 5<input aria-label={`Nota para ${item.ofertaTitulo}`} required min="1" max="5" type="number" value={nota} onChange={event => setNota(event.target.value)} /></label>
    <label>Comentário opcional<textarea maxLength="1000" value={comentario} onChange={event => setComentario(event.target.value)} /></label>
    {mutation.isError && <p role="alert">{mutation.error.message}</p>}
    <Button type="submit" loading={mutation.isPending}>Enviar avaliação</Button>
  </form>;
}
