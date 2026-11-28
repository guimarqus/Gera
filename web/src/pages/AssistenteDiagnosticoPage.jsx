import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { AlertTriangle, ArrowRight, Sparkles } from 'lucide-react';
import { api } from '../services/api.js';
import { Badge, Button, Card, PageHeader } from '../design/components.jsx';
import './assistente.css';

export default function AssistenteDiagnosticoPage() {
  const [problema, setProblema] = useState('');
  const [cidade, setCidade] = useState('');
  const [consentiu, setConsentiu] = useState(false);
  const diagnostico = useMutation({ mutationFn: body => api.diagnosticarProblema(body) });

  function enviar(event) {
    event.preventDefault();
    diagnostico.mutate({ problema: problema.trim(), cidade: cidade.trim() || null, consentiuCompartilhamento: consentiu });
  }

  return <div className="page-content assistente-page">
    <PageHeader eyebrow="GERA · ASSISTENTE" title="Encontre ajuda para o seu problema" description="Conte o que precisa. Vamos sugerir categorias e profissionais disponíveis no Gera." />
    <Card className="assistente-form-card">
      <form onSubmit={enviar}>
        <label className="assistente-label" htmlFor="problema">O que aconteceu ou precisa ser feito?</label>
        <textarea id="problema" className="ds-input assistente-textarea" value={problema} onChange={event => { setProblema(event.target.value.slice(0, 2000)); diagnostico.reset(); }} maxLength={2000} minLength={8} required placeholder="Ex.: uma torneira está vazando embaixo da pia da cozinha" aria-describedby="problema-ajuda" />
        <div id="problema-ajuda" className="assistente-meta"><span>Não inclua endereço, telefone, documentos ou outras informações pessoais.</span><span>{problema.length}/2000</span></div>
        <label className="assistente-label" htmlFor="cidade">Cidade ou bairro para buscar profissionais <span>(opcional)</span></label>
        <input id="cidade" className="ds-input" value={cidade} onChange={event => { setCidade(event.target.value.slice(0, 120)); diagnostico.reset(); }} maxLength={120} placeholder="Ex.: Manaus, Centro" />
        <label className="assistente-consent"><input type="checkbox" checked={consentiu} onChange={event => setConsentiu(event.target.checked)} required /> <span>Concordo em enviar a descrição do problema ao serviço de IA para receber sugestões. A cidade informada será usada apenas na busca interna do Gera.</span></label>
        <p className="assistente-privacidade">O Gera envia somente a descrição e a lista de categorias disponíveis. Não enviamos seu nome, cidade, localização exata ou dados de conta. A descrição não fica salva no histórico do Gera; o provedor pode processá-la conforme suas políticas de retenção e segurança.</p>
        <Button type="submit" loading={diagnostico.isPending} disabled={!consentiu || problema.trim().length < 8}>
          <Sparkles size={18} aria-hidden="true" /> {diagnostico.isPending ? 'Analisando…' : 'Sugerir profissionais'}
        </Button>
      </form>
    </Card>

    {diagnostico.isError && <Card className="assistente-resultado assistente-erro" role="alert">
      <strong>Não foi possível gerar sugestões agora.</strong>
      <p>{diagnostico.error?.message || 'Tente novamente mais tarde.'}</p>
      {diagnostico.error?.status === 503 && <p>O serviço ainda aguarda configuração pela equipe do Gera.</p>}
      <Button variant="secondary" onClick={() => diagnostico.mutate({ problema: problema.trim(), cidade: cidade.trim() || null, consentiuCompartilhamento: consentiu })}>Tentar novamente</Button>
    </Card>}

    {diagnostico.data && <section className="assistente-resultado" aria-live="polite">
      <Card>
        <h2><Sparkles size={20} aria-hidden="true" /> Sugestões para você</h2>
        <p>{diagnostico.data.resumo}</p>
        {diagnostico.data.requerCautela && <div className="assistente-aviso"><AlertTriangle size={20} aria-hidden="true" /><p>{diagnostico.data.aviso}</p></div>}
        {diagnostico.data.categorias?.length > 0 && <><h3>Categorias recomendadas</h3><div className="assistente-tags">{diagnostico.data.categorias.map(item => <Badge key={item.id}>{item.nome}</Badge>)}</div></>}
        {diagnostico.data.sequenciaSugerida?.length > 0 && <><h3>Por onde começar</h3><ol>{diagnostico.data.sequenciaSugerida.map((item, index) => <li key={`${index}-${item}`}>{item}</li>)}</ol></>}
      </Card>
      {diagnostico.data.ofertas?.length > 0 ? <Card>
        <h2>Profissionais encontrados</h2>
        <div className="assistente-ofertas">{diagnostico.data.ofertas.map(oferta => <article className="assistente-oferta" key={oferta.id}>
          {oferta.fotoCapaUrl && <img src={oferta.fotoCapaUrl} alt="" loading="lazy" />}
          <div><Badge>{oferta.categoria}</Badge><h3><Link to={`/ofertas/${oferta.id}`}>{oferta.titulo}</Link></h3><p>{oferta.prestador} · {oferta.cidade || oferta.bairro || 'Atendimento local'}</p><Link className="assistente-link" to={`/ofertas/${oferta.id}`}>Ver oferta <ArrowRight size={16} aria-hidden="true" /></Link></div>
        </article>)}</div>
      </Card> : <Card><h2>Sem ofertas compatíveis por enquanto</h2><p>Experimente buscar por uma cidade próxima ou consultar as categorias.</p><Link to="/categorias">Ver categorias</Link></Card>}
      <p className="assistente-disclaimer">As sugestões são automatizadas e podem estar incorretas. Confirme o escopo e as condições diretamente com um profissional.</p>
    </section>}
  </div>;
}
