import React from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import App from './App.jsx';
import { AuthProvider } from './auth/AuthContext.jsx';
import GlobalErrorBoundary from './routing/GlobalErrorBoundary.jsx';
import './design/tokens.css';
import './design/components.css';
import './layout/layout.css';
import './style.css';

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: 1, refetchOnWindowFocus: false } } });
createRoot(document.getElementById('root')).render(
  <React.StrictMode><QueryClientProvider client={queryClient}><BrowserRouter><AuthProvider><GlobalErrorBoundary><App /></GlobalErrorBoundary></AuthProvider></BrowserRouter></QueryClientProvider></React.StrictMode>
);
