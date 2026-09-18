import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ToastContainer } from './components';
import AppRoutes from './routes/AppRoutes';
import './assets/App.css';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
      staleTime: 60 * 1000,
    },
  },
});

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AppRoutes />
      <ToastContainer />
    </QueryClientProvider>
  );
}