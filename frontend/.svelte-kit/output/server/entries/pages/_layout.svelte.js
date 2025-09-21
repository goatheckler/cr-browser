import { x as head, y as slot } from "../../chunks/index.js";
function _layout($$renderer, $$props) {
  head($$renderer, ($$renderer2) => {
    $$renderer2.title(($$renderer3) => {
      $$renderer3.push(`<title>GHCR Tag Browser</title>`);
    });
  });
  $$renderer.push(`<!--[-->`);
  slot($$renderer, $$props, "default", {});
  $$renderer.push(`<!--]-->`);
}
export {
  _layout as default
};
