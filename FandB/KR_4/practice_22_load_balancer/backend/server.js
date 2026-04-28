const express = require("express");

const app = express();
const PORT = process.env.PORT ? Number(process.env.PORT) : 3000;
const SERVER_ID = process.env.SERVER_ID || `backend-${PORT}`;

app.get("/", (req, res) => {
  res.json({ server: SERVER_ID, port: PORT });
});

app.get("/health", (req, res) => {
  res.json({ ok: true, server: SERVER_ID });
});

app.listen(PORT, "0.0.0.0", () => {
  console.log(`Backend ${SERVER_ID} listening on http://localhost:${PORT}`);
});

