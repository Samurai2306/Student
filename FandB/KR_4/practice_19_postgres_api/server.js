require("dotenv").config();

const express = require("express");
const { Pool } = require("pg");
const swaggerUi = require("swagger-ui-express");

const PORT = process.env.PORT ? Number(process.env.PORT) : 3005;
const DATABASE_URL =
  process.env.DATABASE_URL || "postgres://postgres:postgres@localhost:5432/kr4_practice19";

const pool = new Pool({
  connectionString: DATABASE_URL,
  ssl: process.env.PGSSLMODE === "require" ? { rejectUnauthorized: false } : undefined
});

async function ensureSchema() {
  await pool.query(`
    CREATE TABLE IF NOT EXISTS users (
      id SERIAL PRIMARY KEY,
      first_name VARCHAR(100) NOT NULL,
      last_name VARCHAR(100) NOT NULL,
      age INTEGER NOT NULL CHECK (age >= 0),
      created_at BIGINT NOT NULL,
      updated_at BIGINT NOT NULL
    );
  `);
}

function nowUnixMs() {
  return Date.now();
}

const app = express();
app.use(express.json());

const openapi = {
  openapi: "3.0.3",
  info: {
    title: "Practice 19 — PostgreSQL Users API",
    version: "1.0.0",
    description: "CRUD API пользователей с хранением данных в PostgreSQL."
  },
  servers: [{ url: `http://localhost:${PORT}` }],
  paths: {
    "/health": {
      get: {
        summary: "Health check",
        responses: {
          200: { description: "OK" },
          500: { description: "DB error" }
        }
      }
    },
    "/api/users": {
      get: {
        summary: "Получить список пользователей",
        responses: {
          200: {
            description: "OK",
            content: { "application/json": { schema: { type: "array", items: { $ref: "#/components/schemas/User" } } } }
          }
        }
      },
      post: {
        summary: "Создать пользователя",
        requestBody: {
          required: true,
          content: { "application/json": { schema: { $ref: "#/components/schemas/UserCreate" } } }
        },
        responses: {
          201: { description: "Created", content: { "application/json": { schema: { $ref: "#/components/schemas/User" } } } },
          400: { description: "Validation error" }
        }
      }
    },
    "/api/users/{id}": {
      parameters: [{ name: "id", in: "path", required: true, schema: { type: "integer" } }],
      get: {
        summary: "Получить пользователя по id",
        responses: {
          200: { description: "OK", content: { "application/json": { schema: { $ref: "#/components/schemas/User" } } } },
          404: { description: "Not found" }
        }
      },
      patch: {
        summary: "Обновить пользователя (частично)",
        requestBody: {
          required: true,
          content: { "application/json": { schema: { $ref: "#/components/schemas/UserUpdate" } } }
        },
        responses: {
          200: { description: "OK", content: { "application/json": { schema: { $ref: "#/components/schemas/User" } } } },
          400: { description: "Validation error" },
          404: { description: "Not found" }
        }
      },
      delete: {
        summary: "Удалить пользователя",
        responses: {
          200: { description: "OK", content: { "application/json": { schema: { $ref: "#/components/schemas/User" } } } },
          404: { description: "Not found" }
        }
      }
    }
  },
  components: {
    schemas: {
      User: {
        type: "object",
        properties: {
          id: { type: "integer" },
          first_name: { type: "string" },
          last_name: { type: "string" },
          age: { type: "integer" },
          created_at: { type: "integer", description: "Unix time (ms)" },
          updated_at: { type: "integer", description: "Unix time (ms)" }
        },
        required: ["id", "first_name", "last_name", "age", "created_at", "updated_at"]
      },
      UserCreate: {
        type: "object",
        properties: {
          first_name: { type: "string" },
          last_name: { type: "string" },
          age: { type: "integer", minimum: 0 }
        },
        required: ["first_name", "last_name", "age"]
      },
      UserUpdate: {
        type: "object",
        properties: {
          first_name: { type: "string" },
          last_name: { type: "string" },
          age: { type: "integer", minimum: 0 }
        }
      }
    }
  }
};

app.get("/openapi.json", (req, res) => res.json(openapi));
app.use("/docs", swaggerUi.serve, swaggerUi.setup(openapi, { explorer: true }));

app.get("/health", async (req, res) => {
  try {
    await pool.query("SELECT 1;");
    res.json({ ok: true });
  } catch (e) {
    res.status(500).json({ ok: false, error: String(e?.message || e) });
  }
});

// POST /api/users
app.post("/api/users", async (req, res) => {
  const { first_name, last_name, age } = req.body || {};
  if (!first_name || !last_name || age === undefined) {
    return res.status(400).json({ error: "first_name, last_name, age are required" });
  }
  const ageNum = Number(age);
  if (!Number.isInteger(ageNum) || ageNum < 0) {
    return res.status(400).json({ error: "age must be a non-negative integer" });
  }

  const ts = nowUnixMs();
  const result = await pool.query(
    `INSERT INTO users (first_name, last_name, age, created_at, updated_at)
     VALUES ($1, $2, $3, $4, $5)
     RETURNING *;`,
    [String(first_name).trim(), String(last_name).trim(), ageNum, ts, ts]
  );
  res.status(201).json(result.rows[0]);
});

// GET /api/users
app.get("/api/users", async (req, res) => {
  const result = await pool.query("SELECT * FROM users ORDER BY id ASC;");
  res.json(result.rows);
});

// GET /api/users/:id
app.get("/api/users/:id", async (req, res) => {
  const id = Number(req.params.id);
  if (!Number.isInteger(id)) return res.status(400).json({ error: "id must be integer" });
  const result = await pool.query("SELECT * FROM users WHERE id = $1;", [id]);
  if (!result.rows[0]) return res.status(404).json({ error: "user not found" });
  res.json(result.rows[0]);
});

// PATCH /api/users/:id
app.patch("/api/users/:id", async (req, res) => {
  const id = Number(req.params.id);
  if (!Number.isInteger(id)) return res.status(400).json({ error: "id must be integer" });

  const { first_name, last_name, age } = req.body || {};
  const updates = [];
  const values = [];
  let i = 1;

  if (first_name !== undefined) {
    updates.push(`first_name = $${i++}`);
    values.push(String(first_name).trim());
  }
  if (last_name !== undefined) {
    updates.push(`last_name = $${i++}`);
    values.push(String(last_name).trim());
  }
  if (age !== undefined) {
    const ageNum = Number(age);
    if (!Number.isInteger(ageNum) || ageNum < 0) {
      return res.status(400).json({ error: "age must be a non-negative integer" });
    }
    updates.push(`age = $${i++}`);
    values.push(ageNum);
  }

  if (updates.length === 0) return res.status(400).json({ error: "No fields to update" });

  updates.push(`updated_at = $${i++}`);
  values.push(nowUnixMs());

  values.push(id);
  const result = await pool.query(
    `UPDATE users SET ${updates.join(", ")} WHERE id = $${i} RETURNING *;`,
    values
  );
  if (!result.rows[0]) return res.status(404).json({ error: "user not found" });
  res.json(result.rows[0]);
});

// DELETE /api/users/:id
app.delete("/api/users/:id", async (req, res) => {
  const id = Number(req.params.id);
  if (!Number.isInteger(id)) return res.status(400).json({ error: "id must be integer" });
  const result = await pool.query("DELETE FROM users WHERE id = $1 RETURNING *;", [id]);
  if (!result.rows[0]) return res.status(404).json({ error: "user not found" });
  res.json(result.rows[0]);
});

ensureSchema()
  .then(() => {
    app.listen(PORT, () => {
      console.log(`Practice 19 API running on http://localhost:${PORT}`);
      console.log(`Swagger UI: http://localhost:${PORT}/docs`);
    });
  })
  .catch((e) => {
    console.error("Failed to init DB schema:", e);
    process.exit(1);
  });

