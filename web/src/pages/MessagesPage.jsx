import { useEffect, useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Paperclip, Send } from 'lucide-react';
import { useAuth } from '../auth/AuthContext.jsx';
import { Avatar, Button, EmptyState, ErrorState, PageHeader, Skeleton } from '../design/components.jsx';
import { api, criarConexaoChat } from '../services/api.js';
import './mensagens.css';

function Anexo({ mensagem }) {
  const query = useQuery({ queryKey: ['midia-chat', mensagem.midiaUrl], queryFn: () => api.midiaChat(mensagem.midiaUrl), staleTime: Infinity });
  const [url, setUrl] = useState(null);
  useEffect(() => {
    if (!query.data) return undefined;
    const objeto = URL.createObjectURL(query.data);
    setUrl(objeto);
    return () => URL.revokeObjectURL(objeto);
  }, [query.data]);
  if (query.isError) return <span>Não foi possível abrir o anexo.</span>;
  if (!url) return <span>Carregando anexo…</span>;
  return mensagem.tipo === 2 ? <img className="mensagem__foto" src={url} alt="Imagem enviada na conversa" />
    : <audio controls preload="metadata" src={url}>Seu navegador não reproduz este áudio.</audio>;
}

export default function MessagesPage() {
  const { token, user } = useAuth();
  const [params, setParams] = useSearchParams();
  const cache = useQueryClient();
  const idParam = Number(params.get('id'));
  const solicitacaoId = Number.isSafeInteger(idParam) && idParam > 0 ? idParam : null;
  const [texto, setTexto] = useState('');
  const [digitando, setDigitando] = useState('');
  const [conexaoOk, setConexaoOk] = useState(false);
  const arquivoRef = useRef(null);
  const fimRef = useRef(null);
  const typingTimer = useRef(null);
  const typingClearTimer = useRef(null);
  const conexaoRef = useRef(null);
  const conversas = useQuery({ queryKey: ['conversas'], queryFn: api.conversas });
  const chat = useQuery({ queryKey: ['mensagens', solicitacaoId], queryFn: () => api.mensagens(solicitacaoId), enabled: !!solicitacaoId, refetchOnWindowFocus: true });
  const enviar = useMutation({ mutationFn: value => api.enviarMensagem(solicitacaoId, value), onSuccess: () => { setTexto(''); cache.invalidateQueries({ queryKey: ['mensagens', solicitacaoId] }); cache.invalidateQueries({ queryKey: ['conversas'] }); } });
  const anexo = useMutation({ mutationFn: file => api.enviarMidiaChat(solicitacaoId, file), onSuccess: () => { cache.invalidateQueries({ queryKey: ['mensagens', solicitacaoId] }); cache.invalidateQueries({ queryKey: ['conversas'] }); } });
  const anteriores = useMutation({ mutationFn: () => api.mensagens(solicitacaoId, chat.data?.[0]?.id), onSuccess: old => cache.setQueryData(['mensagens', solicitacaoId], current => [...old, ...(current || [])]) });

  useEffect(() => {
    if (!solicitacaoId && conversas.data?.length) setParams({ id: String(conversas.data[0].solicitacaoId) }, { replace: true });
  }, [conversas.data, solicitacaoId, setParams]);
  const temNaoLidas = !!chat.data?.some(item => String(item.remetenteId) !== String(user?.id) && !item.lida);
  useEffect(() => {
    if (chat.isSuccess && solicitacaoId && temNaoLidas) {
      api.marcarMensagensLidas(solicitacaoId).then(() => {
        cache.setQueryData(['mensagens', solicitacaoId], old => old?.map(item => String(item.remetenteId) !== String(user?.id) ? { ...item, lida: true } : item));
        cache.invalidateQueries({ queryKey: ['conversas'] });
      }).catch(() => {});
    }
  }, [chat.isSuccess, solicitacaoId, temNaoLidas, user?.id, cache]);
  useEffect(() => { if (typeof fimRef.current?.scrollIntoView === 'function') fimRef.current.scrollIntoView({ block: 'end' }); }, [chat.data]);

  useEffect(() => {
    if (!token || !solicitacaoId) return undefined;
    const connection = criarConexaoChat();
    conexaoRef.current = connection;
    let ativo = true;
    const atualizar = () => {
      cache.invalidateQueries({ queryKey: ['mensagens', solicitacaoId] });
      cache.invalidateQueries({ queryKey: ['conversas'] });
    };
    const receber = message => {
      cache.setQueryData(['mensagens', solicitacaoId], old => old && !old.some(item => item.id === message.id) ? [...old, message].sort((a, b) => a.id - b.id) : old);
      cache.invalidateQueries({ queryKey: ['conversas'] });
    };
    const digitandoAgora = (senderId, nome) => {
      if (String(senderId) === String(user?.id)) return;
      setDigitando(nome);
      clearTimeout(typingClearTimer.current);
      typingClearTimer.current = window.setTimeout(() => setDigitando(''), 1800);
    };
    const reconectado = () => connection.invoke('EntrarSolicitacao', solicitacaoId).then(() => setConexaoOk(true)).catch(() => setConexaoOk(false));
    connection.on('NovaMensagem', receber);
    connection.on('SolicitacaoAtualizada', atualizar);
    connection.on('MensagensLidas', senderId => {
      if (String(senderId) !== String(user?.id)) cache.setQueryData(['mensagens', solicitacaoId], old => old?.map(item => String(item.remetenteId) === String(user?.id) ? { ...item, lida: true } : item));
    });
    connection.on('UsuarioDigitando', digitandoAgora);
    connection.onreconnected(reconectado);
    connection.start().then(() => connection.invoke('EntrarSolicitacao', solicitacaoId)).then(() => ativo && setConexaoOk(true)).catch(() => ativo && setConexaoOk(false));
    return () => {
      ativo = false;
      connection.off('NovaMensagem', receber); connection.off('SolicitacaoAtualizada', atualizar);
      connection.off('UsuarioDigitando', digitandoAgora); connection.offreconnected(reconectado); connection.stop(); conexaoRef.current = null; setConexaoOk(false);
    };
  }, [token, solicitacaoId, cache, user?.id]);

  const selecionada = conversas.data?.find(item => item.solicitacaoId === solicitacaoId);
  return <div className="page-content">
    <PageHeader title="Mensagens" description="Converse com clientes e profissionais sobre os serviços." />
    {conversas.isPending && <Skeleton lines={5} />}
    {conversas.isError && <ErrorState title="Não foi possível carregar as conversas" onRetry={conversas.refetch} />}
    {conversas.isSuccess && conversas.data.length === 0 && <EmptyState title="Nenhuma conversa por enquanto" description="Ao solicitar ou receber um serviço, a conversa ficará disponível aqui." />}
    {conversas.isSuccess && conversas.data.length > 0 && <section className="chat-layout" aria-label="Mensagens do Gera">
      <nav className="chat-lista" aria-label="Conversas">
        {conversas.data.map(item => <button type="button" key={item.solicitacaoId} aria-current={item.solicitacaoId === solicitacaoId ? 'true' : undefined}
          onClick={() => setParams({ id: String(item.solicitacaoId) })}>
          <Avatar name={item.outroUsuario} src={item.fotoUrl} size="sm" /><span><strong>{item.outroUsuario}</strong><small>{item.titulo}</small><small>{item.ultimaMensagem || 'Inicie a conversa'}</small></span>
          {item.naoLidas > 0 && <b aria-label={`${item.naoLidas} não lidas`}>{item.naoLidas}</b>}
        </button>)}
      </nav>
      {!selecionada ? <EmptyState title="Selecione uma conversa" description="Escolha um pedido para ver o histórico." /> : <section className="chat-conversa" aria-label={`Conversa com ${selecionada.outroUsuario}`}>
        <header className="chat-conversa__cabecalho"><Avatar name={selecionada.outroUsuario} src={selecionada.fotoUrl} size="sm" /><div><strong>{selecionada.outroUsuario}</strong><small>{selecionada.titulo} · {conexaoOk ? 'Conectado' : 'Reconectando…'}</small></div></header>
        {chat.isPending ? <Skeleton lines={5} /> : chat.isError ? <ErrorState title="Não foi possível carregar as mensagens" onRetry={chat.refetch} /> : <div className="chat-mensagens" aria-live="polite">
          {chat.data.length === 50 && <Button variant="secondary" loading={anteriores.isPending} onClick={() => anteriores.mutate()}>Carregar mensagens anteriores</Button>}
          {anteriores.isError && <p role="alert">{anteriores.error.message}</p>}
          {chat.data.map(message => {
            const minha = String(message.remetenteId) === String(user?.id);
            const sistema = message.tipo === 4 || message.tipo === 5;
            return <article key={message.id} className={`mensagem ${minha ? 'mensagem--minha' : ''} ${sistema ? 'mensagem--sistema' : ''}`}>
              {!minha && <small>{message.remetente}</small>}
              {message.midiaUrl ? <Anexo mensagem={message} /> : <p>{message.conteudo}</p>}
              <time dateTime={message.dataHora}>{new Date(message.dataHora).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}</time>
              {minha && message.tipo < 4 && <small>{message.lida ? 'Lida' : 'Enviada'}</small>}
            </article>;
          })}
          {digitando && <p className="chat-digitando" role="status">{digitando} está digitando…</p>}
          <div ref={fimRef} />
        </div>}
        <form className="chat-compositor" onSubmit={event => { event.preventDefault(); if (texto.trim()) enviar.mutate(texto); }}>
          {(enviar.isError || anexo.isError) && <p role="alert">{(enviar.error || anexo.error).message}</p>}
          <textarea aria-label="Escreva sua mensagem" value={texto} maxLength={2000} rows={2} placeholder="Escreva uma mensagem…"
            onChange={event => { setTexto(event.target.value); if (conexaoOk) { clearTimeout(typingTimer.current); typingTimer.current = window.setTimeout(() => conexaoRef.current?.invoke('Digitar', solicitacaoId).catch(() => {}), 300); } }} />
          <div><input ref={arquivoRef} className="sr-only" type="file" accept="image/jpeg,image/png,image/webp,audio/*" aria-label="Selecionar foto ou áudio" onChange={event => { const file = event.target.files?.[0]; if (file) anexo.mutate(file); event.target.value = ''; }} />
            <Button variant="secondary" disabled={anexo.isPending} aria-label="Anexar foto ou áudio" onClick={() => arquivoRef.current?.click()}><Paperclip size={18} /> Anexar</Button>
            <Button type="submit" loading={enviar.isPending || anexo.isPending} disabled={!texto.trim()}><Send size={18} /> Enviar</Button>
          </div>
        </form>
      </section>}
    </section>}
  </div>;
}
