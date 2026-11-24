import { useState } from 'react';
import { Button, ErrorState, Skeleton } from '../design/components.jsx';

export default function FotosOferta({ consulta, bloqueado, alterar }) {
  const [confirmar, setConfirmar] = useState(null);
  if (consulta.isPending) return <Skeleton lines={3} />;
  if (consulta.isError) return <ErrorState title="Não foi possível carregar as fotos" onRetry={consulta.refetch} />;

  const fotos = consulta.data.fotos;
  if (!fotos.length) return <p>Nenhuma foto cadastrada para esta oferta.</p>;

  function mover(indice, destino) {
    const fotoIds = fotos.map(foto => foto.id);
    [fotoIds[indice], fotoIds[destino]] = [fotoIds[destino], fotoIds[indice]];
    alterar({ tipo: 'ordem', fotoIds });
  }

  return <ol className="oferta-lista">
    {fotos.map((foto, indice) => <li className="oferta-foto" key={foto.id}>
      <img src={foto.urlMiniatura || foto.url} alt={`Foto ${indice + 1} da oferta`} loading="lazy" />
      <div>
        <p>{foto.ehCapa ? `Foto ${indice + 1} · Capa` : `Foto ${indice + 1}`}</p>
        <div className="oferta-acoes">
          {!foto.ehCapa && <Button variant="secondary" disabled={bloqueado}
            onClick={() => alterar({ tipo: 'capa', fotoId: foto.id })}>Definir como capa</Button>}
          <Button variant="secondary" disabled={bloqueado || indice === 0}
            aria-label={`Mover foto ${indice + 1} para cima`} onClick={() => mover(indice, indice - 1)}>Subir</Button>
          <Button variant="secondary" disabled={bloqueado || indice === fotos.length - 1}
            aria-label={`Mover foto ${indice + 1} para baixo`} onClick={() => mover(indice, indice + 1)}>Descer</Button>
          <Button variant="ghost" disabled={bloqueado}
            onClick={() => setConfirmar(foto.id)}>Excluir</Button>
        </div>
        {confirmar === foto.id && <div className="oferta-confirmacao oferta-acoes">
          <span>Excluir esta foto?</span>
          <Button variant="danger" disabled={bloqueado} onClick={() => {
            alterar({ tipo: 'excluir', fotoId: foto.id });
            setConfirmar(null);
          }}>Confirmar exclusão</Button>
          <Button variant="secondary" onClick={() => setConfirmar(null)}>Cancelar</Button>
        </div>}
      </div>
    </li>)}
  </ol>;
}
