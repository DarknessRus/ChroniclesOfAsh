"use strict";

const http = require("node:http");

const PORT = Number(process.env.PORT || 3000);
const API_KEY = process.env.AI_API_KEY || "";
const API_URL = process.env.AI_API_URL || "https://openrouter.ai/api/v1/chat/completions";
const MODEL = process.env.AI_MODEL || "";
const MAX_BODY = 16 * 1024;
const WINDOW_MS = 60_000;
const MAX_REQUESTS_PER_WINDOW = 12;
const clients = new Map();

function send(res, status, data) {
  res.writeHead(status, { "content-type": "application/json; charset=utf-8", "cache-control": "no-store", "x-content-type-options": "nosniff" });
  res.end(JSON.stringify(data));
}
function rateAllowed(ip) {
  const now = Date.now();
  const state = clients.get(ip);
  if (!state || now - state.start >= WINDOW_MS) { clients.set(ip, { start: now, count: 1 }); return true; }
  state.count += 1;
  return state.count <= MAX_REQUESTS_PER_WINDOW;
}
function readJson(req) {
  return new Promise((resolve, reject) => {
    let body = "";
    req.on("data", chunk => {
      body += chunk;
      if (Buffer.byteLength(body, "utf8") > MAX_BODY) { reject(new Error("Request too large")); req.destroy(); }
    });
    req.on("end", () => { try { resolve(JSON.parse(body || "{}")); } catch { reject(new Error("Invalid JSON")); } });
    req.on("error", reject);
  });
}
function cleanText(value, max = 500) { return typeof value === "string" ? value.trim().slice(0, max) : ""; }
function normalizeQuest(raw) {
  const allowed = new Set(["kill_slime", "explore_ruins", "find_relic", "talk_to_survivor"]);
  const title = cleanText(raw.title, 90), story = cleanText(raw.story, 700), objective = cleanText(raw.objective, 180);
  if (!title || !story || !objective) throw new Error("AI returned an incomplete quest");
  return {
    title, story, objective,
    objective_type: allowed.has(raw.objective_type) ? raw.objective_type : "explore_ruins",
    reward_xp: Number.isFinite(Number(raw.reward_xp)) ? Math.max(10, Math.min(100, Math.round(Number(raw.reward_xp)))) : 25,
    next_hook: cleanText(raw.next_hook, 220)
  };
}
function normalizeWorldEvent(raw) {
  if (!raw || typeof raw !== "object") return null;
  const allowed = new Set(["haunted_cave", "buried_crypt", "ruined_temple", "rift"]);
  return {
    title: cleanText(raw.title, 80) || "Разлом под пепельным холмом",
    kind: allowed.has(raw.kind) ? raw.kind : "haunted_cave",
    enemy_count: Math.max(3, Math.min(8, Math.round(Number(raw.enemy_count) || 5))),
    chest_count: Math.max(1, Math.min(5, Math.round(Number(raw.chest_count) || 3))),
    lore: cleanText(raw.lore, 300)
  };
}
const server = http.createServer(async (req, res) => {
  const url = new URL(req.url, "http://localhost");
  if (req.method === "GET" && url.pathname === "/health") return send(res, 200, { ok: true, service: "chronicles-of-ash-ai" });
  if (req.method !== "POST" || !["/api/story", "/api/world/segment"].includes(url.pathname)) return send(res, 404, { error: "Not found" });
  const ip = (req.headers["x-forwarded-for"] || req.socket.remoteAddress || "unknown").toString().split(",")[0].trim();
  if (!rateAllowed(ip)) return send(res, 429, { error: "Слишком много запросов. Подожди минуту." });
  if (!API_KEY || !MODEL) return send(res, 503, { error: "ИИ-сервер ещё не настроен: добавь AI_API_KEY и AI_MODEL в переменные окружения." });
  try {
    const input = await readJson(req);
    if (url.pathname === "/api/world/segment") {
      const allowedBiomes = ["ashen_forest", "frozen_ruins", "crystal_wastes", "volcanic_rift"];
      const allowedEnemies = ["ash_wraith", "bone_hound", "rift_stalker", "grave_knight", "crystal_sentinel"];
      const chunkIndex = Math.max(0, Math.min(1_000_000, Math.floor(Number(input.chunkIndex) || 0)));
      const seed = Math.max(0, Math.min(2_147_483_647, Math.floor(Number(input.seed) || 7319)));
      const playerLevel = Math.max(1, Math.min(100, Math.floor(Number(input.playerLevel) || 1)));
      const previousBiomes = Array.isArray(input.previousBiomes)
        ? input.previousBiomes.filter(x => allowedBiomes.includes(x)).slice(-4)
        : [];
      const worldPrompt = [
        "You are the live world director for the original mobile dark-fantasy ARPG Chronicles of Ash.",
        "Create a compact gameplay plan for one 48-meter procedural world segment. Return only JSON.",
        "Pick one supported biome and enemy archetype; vary the result and avoid repeating recent biomes when possible.",
        "Do not invent asset filenames or code. Unity will construct geometry from existing primitives and materials.",
        "Keep difficulty fair for a mobile game. Enemy count 1-5; health multiplier 0.7-2.5; speed multiplier 0.7-1.6; loot tier 1-5.",
        "Supported biome IDs: ashen_forest, frozen_ruins, crystal_wastes, volcanic_rift.",
        "Supported enemy IDs: ash_wraith, bone_hound, rift_stalker, grave_knight, crystal_sentinel.",
        "JSON fields: biome, enemyType, enemyCount, enemyHealthMultiplier, enemySpeedMultiplier, lootTier, description.",
        "All descriptive text must be in Russian and no longer than 180 characters.",
        "Input: " + JSON.stringify({ chunkIndex, seed, playerLevel, previousBiomes })
      ].join("\n");
      const worldUpstream = await fetch(API_URL, {
        method: "POST",
        headers: { "authorization": "Bearer " + API_KEY, "content-type": "application/json", "accept": "application/json" },
        body: JSON.stringify({
          model: MODEL, temperature: 0.9, max_tokens: 350,
          messages: [
            { role: "system", content: "Return only valid JSON, without Markdown fences." },
            { role: "user", content: worldPrompt }
          ],
          response_format: { type: "json_object" }
        }),
        signal: AbortSignal.timeout(20_000)
      });
      const worldData = await worldUpstream.json().catch(() => ({}));
      if (!worldUpstream.ok) {
        console.error("World AI provider returned", worldUpstream.status, JSON.stringify(worldData).slice(0, 600));
        return send(res, 502, { error: "ИИ не смог спланировать участок мира." });
      }
      const worldContent = worldData.choices?.[0]?.message?.content;
      if (typeof worldContent !== "string") throw new Error("Provider response has no world profile");
      const raw = JSON.parse(worldContent.replace(/^\\s*```(?:json)?/i, "").replace(/```\\s*$/, "").trim());
      const numberInRange = (value, min, max, fallback) => {
        const n = Number(value);
        return Number.isFinite(n) ? Math.max(min, Math.min(max, n)) : fallback;
      };
      const profile = {
        chunkIndex,
        seed,
        biome: allowedBiomes.includes(raw.biome) ? raw.biome : "ashen_forest",
        enemyType: allowedEnemies.includes(raw.enemyType) ? raw.enemyType : "ash_wraith",
        enemyCount: Math.round(numberInRange(raw.enemyCount, 1, 5, 2)),
        enemyHealthMultiplier: numberInRange(raw.enemyHealthMultiplier, 0.7, 2.5, 1),
        enemySpeedMultiplier: numberInRange(raw.enemySpeedMultiplier, 0.7, 1.6, 1),
        lootTier: Math.round(numberInRange(raw.lootTier, 1, 5, 1)),
        description: cleanText(raw.description, 180) || "Пепел скрывает следы древнего сражения.",
        source: "ai"
      };
      return send(res, 200, profile);
    }
    const level = Math.max(1, Math.min(100, Number(input.level) || 1));
    const context = {
      level, class: cleanText(input.class, 30) || "warrior",
      location: cleanText(input.location, 100) || "Мир Пепельных Королевств",
      style: cleanText(input.style, 150) || "мрачное оригинальное тёмное фэнтези",
      recent_events: Array.isArray(input.recent_events) ? input.recent_events.slice(-8).map(x => cleanText(x, 180)).filter(Boolean) : [],
      completed_quests: Array.isArray(input.completed_quests) ? input.completed_quests.slice(-10).map(x => cleanText(x, 100)).filter(Boolean) : []
    };
    const prompt = [
      "Ты — системный соавтор оригинальной экшен-RPG «Хроники Пепла», мрачное фэнтези с готической атмосферой, но без копирования чужих франшиз.",
      "Верни только JSON-объект с двумя объектами: quest и world_event.",
      "quest: title, story, objective, objective_type (kill_slime, explore_ruins, find_relic, talk_to_survivor), reward_xp (10-100), next_hook.",
      "world_event: title, kind (haunted_cave, buried_crypt, ruined_temple, rift), enemy_count (3-8), chest_count (1-5), lore.",
      "Каждый запрос должен создавать оригинальную локацию: пещеру или склеп, с врагами, сундуками, лутом и зацепкой для следующей истории. Чередуй биомы, противников, атмосферу и находки, не повторяй недавние события.",
      "Игровой мир сам создаёт событие без подтверждения игрока. Не заявляй, что игрок уже выполнил цель. Все тексты на русском, кратко и выразительно.",
      "Контекст: " + JSON.stringify(context)
    ].join("\n");
    const upstream = await fetch(API_URL, {
      method: "POST",
      headers: { "authorization": "Bearer " + API_KEY, "content-type": "application/json", "accept": "application/json" },
      body: JSON.stringify({
        model: MODEL, temperature: 0.95, max_tokens: 950,
        messages: [
          { role: "system", content: "Отвечай только корректным JSON без Markdown." },
          { role: "user", content: prompt }
        ],
        response_format: { type: "json_object" }
      }),
      signal: AbortSignal.timeout(25_000)
    });
    const upstreamData = await upstream.json().catch(() => ({}));
    if (!upstream.ok) {
      console.error("AI provider returned", upstream.status, JSON.stringify(upstreamData).slice(0, 600));
      return send(res, 502, { error: "ИИ временно не ответил. Попробуй ещё раз позже." });
    }
    const content = upstreamData.choices?.[0]?.message?.content;
    if (typeof content !== "string") throw new Error("Provider response has no message content");
    const parsed = JSON.parse(content.replace(/^\s*```(?:json)?/i, "").replace(/```\s*$/, "").trim());
    const quest = normalizeQuest(parsed.quest || parsed);
    const world_event = normalizeWorldEvent(parsed.world_event);
    return send(res, 200, { quest, world_event, source: "ai" });
  } catch (error) {
    console.error("Story generation failed:", error.message);
    return send(res, error.message === "Invalid JSON" ? 400 : 500, { error: "Не удалось создать событие. Попробуй ещё раз." });
  }
});
server.listen(PORT, "0.0.0.0", () => console.log("Chronicles of Ash AI story service listening on port " + PORT));
