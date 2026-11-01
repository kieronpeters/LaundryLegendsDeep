# Image generation auth research — can a ChatGPT subscription power OpenCode images?

**Date:** 2026-11-01 · **Type:** research only (no installs, no config changes, no credentials read or recorded)
**Scope:** how to make `experimental_image_generate` work in this OpenCode install without an unrelated pay-as-you-go spend

## Bottom line

1. **OpenCode does support ChatGPT subscription OAuth — for chat/text only.** It is a first-class, documented provider
   method ("Sign in with ChatGPT"), and OpenAI lists OpenCode under **Open-source integrations** for Sign in with ChatGPT.
2. **Images are not part of that OAuth path in OpenCode's built-in image tool.** The built-in tool authenticates like an
   API client; with a subscription token it reports `Generation credentials are unavailable`. No OpenCode doc claims
   subscription OAuth image support, and the OpenCode binary contains the Codex chat endpoint
   (`chatgpt.com/backend-api/codex`) but **not** the Codex image endpoint (`backend-api/codex/images`).
3. **A ChatGPT subscription is not API credentials.** Plus and Go explicitly exclude API usage ("API usage is not
   included and billed independently"). Only **ChatGPT Pro ($100/$200/$500) and Business** can claim monthly **API
   credits** ($100/$200/$500, $20/$100 per seat), and those credits *do* cover the Images API.
4. **ChatGPT Go includes no API allowance at all** and its Codex access is limited to the desktop app (GPT-6 Luna).
   Go can therefore **not** generate images through OpenCode.
5. Verified locally: the `openai` account in this install is type `oauth`, method `chatgpt-token-sharing`, scopes
   `chatgpt.tokens.use.direct`, `resource.invoke`; an `opencode` (Console) account also exists with no balance. OpenCode
   CLI is **v2.2.1** here (`PROJECT_STATE.md` still says 2.0.28 — stale, not edited in this research task).

## Decision table

| Route | Images in OpenCode? | Cost | Steps | Caveats |
|---|---|---|---|---|
| **A. Existing ChatGPT OAuth (Go/Plus/Pro)** | **No** — chat only | $0 extra (plan) | Already connected; nothing to do | Built-in image tool needs API-style credentials; error observed |
| B. ChatGPT **Go** ($8/mo) | **No** | $8/mo, no API credit | — | No API allowance; Codex limited to desktop app; ads in some regions |
| C. ChatGPT **Plus** ($20/mo) | **No** | $20/mo, no API credit | — | Images only inside ChatGPT/Codex surfaces |
| D. ChatGPT **Pro** ($100/200/500) + linked API org | **Yes, via API key** (not OAuth) | Pro price, credits = plan tier ($100–$500/mo, expire monthly) | Link subscription to API org in ChatGPT Billing or platform.openai.com; create API key; connect as OpenAI API key | Needs ≥7-day-old paid Pro, owner of both accounts, same email; credits do not raise Codex limits |
| E. **OpenAI API key** (pay-as-you-go) | **Yes** | Per-image tokens (gpt-image-2: $30/1M image output tokens ≈ cents/image) | `/connect` → OpenAI → *Manually enter API Key*, or `opencode auth login openai --method key` | Needs prepaid platform balance; separate from any subscription |
| F. **OpenCode Console credits** | **Yes** (`opencode/gpt-image-2.5-sunburst`) | Pay-as-you-go + 4.6% + $0.31 per top-up | Top up at opencode.ai/console, then `/connect` → OpenCode | This is what "Insufficient account funds" means today |
| G. Community plugin `Krablante/opencode-image-generation` | **Technically yes, unverified** | $0 beyond plan, burns plan usage 3–5× faster | Install plugin (package change → needs approval) | Uses undocumented `chatgpt.com/backend-api/codex/images`; built pre-v2 (SQLite credentials), compatibility unverified; can break without notice |

## Recommended path

**Route E — add an OpenAI API key** for image work in OpenCode. It is the only officially documented, low-friction way to
get `openai/gpt-image-2.5-sunburst` working, it costs cents per image, and it does not disturb the existing ChatGPT
OAuth login (OpenCode keeps separate accounts and you can switch/rename them). If the owner is *already* considering
ChatGPT Pro for other reasons, **route D** is the only way a subscription legitimately funds images — but it still ends
in an API key, not the subscription token.

Do **not** rely on route G for production art. It is an unsupported shim against a private endpoint; treat it as an
experiment only, and only after explicit approval (it adds a package dependency).

## What the owner must do personally

1. **Decide the budget** (nobody else can): API key route needs a prepaid platform balance; Pro route needs a $100+/mo plan.
2. **Route E:** sign in at `platform.openai.com` → Billing → add credit → API keys → create key (copy once, never paste into chat or the repo).
3. **Route E (continued):** in OpenCode run `/connect` → **OpenAI** → **Manually enter API Key** (or `opencode auth login openai`), paste the key. Config lives at `C:\Users\Asus\.config\opencode\opencode.json`; credentials live in the SQLite DB `C:\Users\Asus\.local\share\opencode\opencode.db` — never commit either.
4. **Optional default model:** add `"experimental": { "image_generate": { "model": "openai/gpt-image-2.5-sunburst" } }` to that config (config change = owner action, not done here).
5. **Route D only:** in ChatGPT Settings → Billing, claim the API credit benefit for a linked API organization, then create a project API key there and connect it as in step 3.
6. **Revoke/rotate:** `opencode auth logout openai` (and `opencode auth list` to confirm) plus delete/rotate the key at platform.openai.com. To end ChatGPT plan sharing: ChatGPT → **Settings → Security and login → Sign in with ChatGPT → Disconnect**; usage limits are under **Settings → Usage**.

## Security and terms notes

- OpenAI's help says plan sharing is for **participating apps** and "does not permit sharing your account credentials or reselling access". OpenCode is explicitly listed as an open-source integration, so **chat** usage is sanctioned.
- Reusing the OAuth token against Codex's private image endpoint (route G) is **not documented or sanctioned**; OpenAI warns features may be unavailable to third-party apps, and the endpoint has no compatibility guarantee. Plan usage is still consumed and can trip abuse/rate limits.
- Image generations in Codex consume plan limits **3–5× faster** than normal turns; credits are only used after included limits.
- Any plugin runs with full user permissions and can read stored credentials. Installing one is a dependency/approval gate under `.orchestrator/GUARDRAILS.md`.

## Unverified / open items

- Whether OpenCode's ChatGPT OAuth *image* support is a deliberate exclusion or an unimplemented path — **not documented**; conclusion is inferred from the error, the missing endpoint string in the binary, and a community plugin created specifically to fill the gap.
- Exact per-image cost of `openai/gpt-image-2.5-sunburst` and of `opencode/gpt-image-2.5-sunburst` (no price listed in OpenCode's Console model tables).
- Whether the v0.5.0 plugin works with OpenCode v2.2.1's SQLite credential store.
- Whether ChatGPT **Go** can complete a ChatGPT/Codex sign-in in OpenCode at all (Go's Codex access is described as desktop-app-only).

## Sources

- OpenCode providers / ChatGPT sign-in: https://opencode.ai/v2/docs/providers/ · https://opencode.ai/docs/providers/
- OpenCode image tool + config: https://opencode.ai/v2/docs/tools/ · https://opencode.ai/v2/docs/config/ · https://opencode.ai/changelog (v2.2.1, v2.2.2)
- OpenCode Console / Go pricing: https://opencode.ai/v2/docs/console/models/ · https://opencode.ai/v2/docs/console/go/
- OpenAI Sign in with ChatGPT partners (lists OpenCode): https://developers.openai.com/codex/sign-in-with-chatgpt · https://help.openai.com/articles/20001542
- ChatGPT pricing (Go/Plus/Pro, image generation, monthly API credits): https://chatgpt.com/pricing/
- API credits for ChatGPT subscribers: https://developers.openai.com/api/docs/guides/chatgpt-api-credits
- ChatGPT Go contents: https://help.openai.com/en/articles/11989085-what-is-chatgpt-go
- Plus excludes API usage: https://help.openai.com/en/articles/6950777-what-is-chatgpt-plus
- Codex image generation (counts against plan limits): https://developers.openai.com/codex/image-generation · https://developers.openai.com/codex/pricing
- gpt-image-2 model/endpoints: https://developers.openai.com/api/docs/models/gpt-image-2
- Community OAuth image plugin: https://github.com/Krablante/opencode-image-generation
- Community note that OAuth tokens only reach `backend-api/codex/responses`: https://community.openai.com/t/introducing-gpt-image-2-available-today-in-the-api-and-codex/1379479
