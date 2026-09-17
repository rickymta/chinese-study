import { ErrorPage } from './ErrorPage'

/** Vỏ mỏng của `ErrorPage` cho route `/404` (và `*` → `/404`). Sửa bố cục thì sửa ở `ErrorPage`. */
export function NotFoundPage({ homeHref = '/' }: { homeHref?: string }) {
  return <ErrorPage code={404} homeHref={homeHref} />
}
