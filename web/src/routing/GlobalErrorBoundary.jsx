import { Component } from 'react';
import { ErrorState } from '../design/components.jsx';

export default class GlobalErrorBoundary extends Component {
  state = { hasError: false };
  static getDerivedStateFromError() { return { hasError: true }; }
  componentDidCatch(error, info) { console.error('Erro não tratado na interface do Gera.', error, info); }
  render() {
    if (this.state.hasError) return <main className="global-error"><ErrorState title="Algo deu errado" description="Ocorreu um erro ao abrir esta página." onRetry={() => window.location.reload()} /></main>;
    return this.props.children;
  }
}
