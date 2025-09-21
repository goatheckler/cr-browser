import { z as attr, F as attr_style, G as stringify } from "../../chunks/index.js";
import { a as ssr_context, e as escape_html } from "../../chunks/context.js";
import "ag-grid-community";
function onDestroy(fn) {
  /** @type {SSRContext} */
  ssr_context.r.on_destroy(fn);
}
function _page($$renderer, $$props) {
  $$renderer.component(($$renderer2) => {
    let owner = "";
    let image = "";
    let loadingTags = false;
    let announce = null;
    let gridHeightPx = 400;
    function computeGridHeight() {
      return;
    }
    onDestroy(() => {
      window.removeEventListener("resize", computeGridHeight);
    });
    $$renderer2.push(`<div class="min-h-screen bg-background text-white p-6 space-y-6"><div class="flex items-start justify-between gap-4 flex-wrap w-full"><h1 class="text-2xl font-bold">GHCR Tag Browser</h1> <div class="text-sm text-right">`);
    {
      $$renderer2.push("<!--[-->");
      $$renderer2.push(`<div>Checking health...</div>`);
    }
    $$renderer2.push(`<!--]--></div></div> <section class="space-y-2"><div class="flex gap-2 items-center flex-wrap"><input placeholder="owner"${attr("value", owner)} class="px-2 py-1 bg-surface border border-surface focus:outline-none focus:ring-2 focus:ring-primary"/> <input placeholder="image"${attr("value", image)} class="px-2 py-1 bg-surface border border-surface focus:outline-none focus:ring-2 focus:ring-primary"/> <button class="px-3 py-1 bg-primary hover:bg-primary/80 rounded disabled:opacity-50"${attr("disabled", loadingTags, true)}>Search</button></div> `);
    {
      $$renderer2.push("<!--[!-->");
    }
    $$renderer2.push(`<!--]--></section> <section class="space-y-2"><div role="status">${escape_html(announce)}</div> `);
    {
      $$renderer2.push("<!--[!-->");
    }
    $$renderer2.push(`<!--]--> <div class="ag-theme-alpine ag-theme-alpine-dark border border-surface rounded"${attr_style(`height: ${stringify(gridHeightPx)}px; width: 100%; position:relative;`)}>`);
    {
      $$renderer2.push("<!--[-->");
      $$renderer2.push(`<div class="absolute inset-0 flex items-center justify-center text-sm opacity-60">Initializing grid...</div>`);
    }
    $$renderer2.push(`<!--]--></div></section></div>`);
  });
}
export {
  _page as default
};
