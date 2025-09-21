// Svelte 5 config (minimal)
/** @type {import('@sveltejs/kit').Config} */
import adapter from '@sveltejs/adapter-static';

/** @type {import('@sveltejs/kit').Config} */
const config = {
  kit: {
    // Static SPA build: use fallback index and disable strict prerender checks
    adapter: adapter({ fallback: 'index.html', strict: false }),
    alias: {},
    // Do not attempt to prerender pages (they rely on runtime /api calls)
    prerender: { entries: [] }
  }
};

export default config;
