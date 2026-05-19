require("dotenv").config();

const http = require("http");
const { Server } = require("socket.io");
const { createApp } = require("./app");
const { verifyToken } = require("./lib/auth");

const PORT = Number(process.env.PORT) || 4000;
const ACCESS_SECRET = process.env.ACCESS_SECRET || "access_secret_dev";

const userSockets = new Map();

function notifyUser(userId, payload) {
  const set = userSockets.get(String(userId));
  if (!set) return;
  for (const socketId of set) {
    const io = global.__kr5_io;
    if (io) io.to(socketId).emit("notification", payload);
  }
}

const app = createApp({ notifyUser });
const server = http.createServer(app);

const io = new Server(server, {
  cors: { origin: "*", methods: ["GET", "POST"] }
});
global.__kr5_io = io;

io.use((socket, next) => {
  const token = socket.handshake.auth?.token;
  if (!token) return next(new Error("unauthorized"));
  try {
    const payload = verifyToken(token, ACCESS_SECRET);
    socket.userId = String(payload.sub);
    return next();
  } catch {
    return next(new Error("unauthorized"));
  }
});

io.on("connection", (socket) => {
  const uid = socket.userId;
  if (!userSockets.has(uid)) userSockets.set(uid, new Set());
  userSockets.get(uid).add(socket.id);

  socket.on("disconnect", () => {
    const set = userSockets.get(uid);
    if (set) {
      set.delete(socket.id);
      if (set.size === 0) userSockets.delete(uid);
    }
  });
});

app
  .init()
  .then(() => {
    server.listen(PORT, () => {
      console.log(`KR5 Social Feed API http://localhost:${PORT}`);
      console.log(`Redis: ${app.getRedisReady() ? "enabled" : "disabled"}`);
    });
  })
  .catch((e) => {
    console.error("Failed to start:", e);
    process.exit(1);
  });
