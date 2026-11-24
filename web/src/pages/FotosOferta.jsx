import { useEffect, useState } from 'react';
import { Button, ErrorState, Field, Skeleton } from '../design/components.jsx';
import { api } from '../services/api.js';

function FotoProtegida({ url, alt }) {
  const [imagem, setImagem] = useState(null);
  const protegida = /^\/api\/ofertas\/\d+\/midia\/[a-f0-9]{32}(?:-mini)?\.webp$/.test(url);
  useEffect(() => {
    if (!protegida) return undefined;
    const cancelamento = new AbortController();
    let temporaria;
    api.imagemOferta(url, cancelamento.signal).then(blob => {
      if (cancelamento.signal.aborted) return;
      temporaria = URL.createObjectURL(blob);
      setImagem({ url, temporaria });
    }).catch(() => {
      if (!cancelamento.signal.aborted) setImagem({ url, temporaria: null });
    });
    return () => {
      cancelamento.abort();
      if (temporaria) URL.revokeObjectURL(temporaria);
    };
  }, [url, protegida]);

  if (!protegida) return <img src={url} alt={alt} loading="lazy" />;
  if (imagem?.url !== url) return <span role="status">Carregando foto…</span>;
  return imagem.temporaria ? <img src={imagem.temporaria} alt={alt} /> : <span>Foto indisponível</span>;
}

export default function FotosOferta({ consulta, bloqueado, alterar, progresso }) {
  const [confirmar, setConfirmar] = useState(null);
  const [erroSelecao, setErroSelecao] = useState('');
  if (consulta.isPending) return <Skeleton lines={3} />;
  if (consulta.isError) return <ErrorState title="Não foi possível carregar as fotos" onRetry={consulta.refetch} />;

  const fotos = consulta.data.fotos;
  function selecionar(event) {
    const arquivos = Array.from(event.target.files);
    event.target.value = '';
    setErroSelecao('');
    if (!arquivos.length) return;
    if (arquivos.length + fotos.length > 10) {
      setErroSelecao('Cada oferta pode ter até dez fotos.');
      return;
    }
    if (arquivos.some(arquivo => arquivo.size > 5 * 1024 * 1024 || arquivo.size === 0)) {
      setErroSelecao('Cada foto deve ter até 5 MB e conter uma imagem.');
      return;
    }
    alterar({ tipo: 'upload', arquivos });
  }

  function mover(indice, destino) {
    const fotoIds = fotos.map(foto => foto.id);
    [fotoIds[indice], fotoIds[destino]] = [fotoIds[destino], fotoIds[indice]];
    alterar({ tipo: 'ordem', fotoIds });
  }

  return <>
    <Field label="Adicionar fotos" hint="JPEG, PNG ou WebP. Até dez fotos de 5 MB cada.">
      {attrs => <input {...attrs} type="file" accept="image/jpeg,image/png,image/webp" multiple
        disabled={bloqueado || fotos.length >= 10} onChange={selecionar} />}
    </Field>
    {erroSelecao && <p role="alert">{erroSelecao}</p>}
    {progresso && <p role="status">Fotos enviadas: {progresso.enviadas} de {progresso.total}</p>}
    {!fotos.length && <p>Nenhuma foto cadastrada para esta oferta.</p>}
    <ol className="oferta-lista">
    {fotos.map((foto, indice) => <li className="oferta-foto" key={foto.id}>
      <FotoProtegida url={foto.urlMiniatura || foto.url} alt={`Foto ${indice + 1} da oferta`} />
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
    </ol>
  </>;
}
