import { useEffect, useState } from 'react'

const MAX_SIZE = 360
const MIN_SIZE = 200

/** `min(innerWidth * 0.9, 360)` (§5.3.2) — 375px ⇒ ~337px; theo dõi `resize` (xoay máy). */
function compute(): number {
  if (typeof window === 'undefined') return MAX_SIZE
  return Math.max(MIN_SIZE, Math.min(Math.floor(window.innerWidth * 0.9), MAX_SIZE))
}

export function useBoardSize(): number {
  const [size, setSize] = useState<number>(compute)
  useEffect(() => {
    const onResize = () => setSize(compute())
    window.addEventListener('resize', onResize)
    return () => window.removeEventListener('resize', onResize)
  }, [])
  return size
}
