import { RouterProvider } from '@tanstack/react-router';

import { type AppRouter, router } from './router';

export function App({ appRouter = router }: { appRouter?: AppRouter }) {
  return <RouterProvider router={appRouter} />;
}
