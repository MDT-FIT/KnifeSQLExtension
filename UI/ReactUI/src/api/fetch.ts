export const BASE_URL = import.meta.env.VITE_API_URL;

export function appFetch(url: string, options?: RequestInit) {
  return fetch(`${BASE_URL}${url}`, options);
}
