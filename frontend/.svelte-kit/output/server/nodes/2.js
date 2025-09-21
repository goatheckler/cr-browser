

export const index = 2;
let component_cache;
export const component = async () => component_cache ??= (await import('../entries/pages/_page.svelte.js')).default;
export const universal = {
  "ssr": false
};
export const universal_id = "src/routes/+page.ts";
export const imports = ["_app/immutable/nodes/2.DbySDd6x.js","_app/immutable/chunks/Bzak7iHL.js","_app/immutable/chunks/D3zoPib9.js","_app/immutable/chunks/OKBPzi3Z.js","_app/immutable/chunks/XVT2G2ts.js","_app/immutable/chunks/eCOVmg3Y.js","_app/immutable/chunks/C205TI-p.js"];
export const stylesheets = ["_app/immutable/assets/2.B28RcQlA.css"];
export const fonts = [];
