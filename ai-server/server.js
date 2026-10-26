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
  res.writeHead(status, {
    "content-type": "application/json; charset=utf-8",
    "cache-control": "no-store",
    "x-content-type-options": "nosniff"
  });
  res.end(JSON.stringify(data));
}

function rateAllowed(ip) {
  const now = Date.now();
  const state = clients.get(ip);
  if (!state || now - state.start >= WINDOW_MS) {
    clients.set(ip, { start: now, count: 1 });
    return true;
  }
  state.count += 1;
  return state.count <= MAX_REQUESTS_PER_WINDOW;
}

function readJson(req) {
  return new Promise((resolve, reject) => {
    let body = "";
    req.on("data", chunk => {
      body += chunk;
      if (Buffer.byteLength(body, "utf8") > MAX_BODY) {
        reject(new Error("Request too large"));
        req.destroy();
      }
    });
    req.on("end", () => {
      try { resolve(JSON.parse(body || "{}")); }
      catch { reject(new Error("Invalid JSON")); }
    });
    req.on("error", reject);
  });
}

function cleanText(value, max = 500) {
  return typeof value === "string" ? value.trim().slice(0, max) : "";
}

function normalizeQuest(raw) {
  const allowedObjectives = new Set(["kill_slime", "explore_ruins", "find_relic", "talk_to_survivor"]);
  const title = cleanText(raw.title, 90);
  const story = cleanText(raw.story, 700);
  const objective = cleanText(raw.objective, 180);
  const objectiveType = allowedObjectives.has(raw.objective_type) ? raw.objective_type : "explore_ruins";
  const rewardXp = Number.isFinite(Number(raw.reward_xp))
    ? Math.max(10, Math.min(100, Math.round(Number(raw.reward_xp))))
    : 25;
  const nextHook = cleanText(raw.next_hook, 220);
  if (!title || !story || !objective) throw new Error("AI returned an incomplete quest");
  return { title, story, objective, objective_type: objectiveType, reward_xp: rewardXp, next_hook: nextHook };
}

const server = http.createServer(async (req, res) => {
  const url = new URL(req.url, "http://localhost");
  if (req.method === "GET" && url.pathname === "/health") {
    return send(res, 200, { ok: true, service: "chronicles-of-ash-ai" });
  }
  if (req.method !== "POST" || url.pathname !== "/api/story") {
    return send(res, 404, { error: "Not found" });
  }

  const ip = (req.headers["x-forwarded-for"] || req.socket.remoteAddress || "unknown").toString().split(",")[0].trim();
  if (!rateAllowed(ip)) return send(res, 429, { error: "Слишком много запросов. Подожди минуту." });

  if (!API_KEY || !MODEL) {
    return send(res, 503, { error: "ИИ-сервер ещё не настроен: добавь AI_API_KEY и AI_MODEL в переменные окружения." });
  }

  try {
    const input = await readJson(req);
    const level = Math.max(1, Math.min(100, Number(input.level) || 1));
    const recentEvents = Array.isArray(input.recent_events)
      ? input.recent_events.slice(-8).map(x => cleanText(x, 180)).filter(Boolean)
      : [];
    const completedQuests = Array.isArray(input.completed_quests)
      ? input.completed_quests.slice(-10).map(x => cleanText(x, 100)).filter(Boolean)
      : [];
    const location = cleanText(input.location, 100) || "Пепельная чаща";
    const style = cleanText(input.style, 100) || "мрачное тёмное фэнтези";

    const prompt = [
      "Ты — соавтор оригинальной одиночной RPG «Хроники Пепла». Создавай атмосферные, логичные и разнообразные задания на русском языке.",
      "Верни только один JSON-объект с полями: title, story, objective, objective_type, reward_xp, next_hook.",
      "objective_type должен быть строго одним из: kill_slime, explore_ruins, find_relic, talk_to_survivor.",
      "Не утверждай, что игровые действия уже выполнены. Не создавай реальные URL, код, предметы с произвольными игровыми эффектами или инструкции вне мира игры.",
      "Задание должно быть выполнимым в прототипе: предпочитай kill_slime или explore_ruins; find_relic и talk_to_survivor допускаются как сюжетные зацепки без автоматического завершения.",
      "Не повторяй завершённые задания. Не копируй известные франшизы. Все строки — на русском.",
      "Контекст: " + JSON.stringify({ level, location, style, recent_events: recentEvents, completed_quests: completedQuests })
    ].join("\n");

    const upstream = await fetch(API_URL, {
      method: "POST",
      headers: {
        "authorization": "Bearer " + API_KEY,
        "content-type": "application/json",
        "accept": "application/json"
      },
      body: JSON.stringify({
        model: MODEL,
        temperature: 0.9,
        max_tokens: 650,
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

    const content = upstreamData.choices && upstreamData.choices[0] &&
      upstreamData.choices[0].message && upstreamData.choices[0].message.content;
    if (typeof content !== "string") throw new Error("Provider response has no message content");
    const jsonText = content.replace(/^\s*```(?:json)?/i, "").replace(/```\s*$/, "").trim();
    const quest = normalizeQuest(JSON.parse(jsonText));
    return send(res, 200, { quest, source: "ai" });
  } catch (error) {
    console.error("Story generation failed:", error.message);
    const status = error.message === "Invalid JSON" ? 400 : 500;
    return send(res, status, { error: status === 400 ? "Некорректный запрос." : "Не удалось создать событие. Попробуй ещё раз." });
  }
});

server.listen(PORT, "0.0.0.0", () => {
  console.log("Chronicles of Ash AI story service listening on port " + PORT);
});
