import { afterEach, describe, expect, it, vi } from 'vitest';
import { api, setAccessToken } from '../services/api.js';

afterEach(() => { vi.unstubAllGlobals(); setAccessToken(null); });
describe('cliente HTTP', () => {
 it('compartilha uma unica chamada de refresh simultanea', async () => {
  const fetchMock = vi.fn().mockResolvedValue({ ok: true, status: 200, json: async () => ({ accessToken: 'novo-token', expiraEm: 'depois' }) });
  vi.stubGlobal('fetch', fetchMock);
  const [first, second] = await Promise.all([api.refresh(), api.refresh()]);
  expect(fetchMock).toHaveBeenCalledTimes(1);
  expect(first).toEqual(second);
  expect(first.accessToken).toBe('novo-token');
 });
});
