require("dotenv").config();

const express = require("express");
const mongoose = require("mongoose");

const PORT = process.env.PORT ? Number(process.env.PORT) : 3006;
const MONGO_URL = process.env.MONGO_URL || "mongodb://localhost:27017/kr4_practice20";

const app = express();
app.use(express.json());

const userSchema = new mongoose.Schema(
  {
    first_name: { type: String, required: true, trim: true },
    last_name: { type: String, required: true, trim: true },
    age: { type: Number, required: true, min: 0 },
    created_at: { type: Number, required: true },
    updated_at: { type: Number, required: true }
  },
  { versionKey: false }
);

const User = mongoose.model("User", userSchema);

app.get("/health", async (req, res) => {
  try {
    await mongoose.connection.db.admin().ping();
    res.json({ ok: true });
  } catch (e) {
    res.status(500).json({ ok: false, error: String(e?.message || e) });
  }
});

app.post("/api/users", async (req, res) => {
  const { first_name, last_name, age } = req.body || {};
  if (!first_name || !last_name || age === undefined) {
    return res.status(400).json({ error: "first_name, last_name, age are required" });
  }
  const ageNum = Number(age);
  if (!Number.isInteger(ageNum) || ageNum < 0) {
    return res.status(400).json({ error: "age must be a non-negative integer" });
  }

  const ts = Date.now();
  const user = await User.create({
    first_name,
    last_name,
    age: ageNum,
    created_at: ts,
    updated_at: ts
  });
  res.status(201).json(user);
});

app.get("/api/users", async (req, res) => {
  const users = await User.find({}).sort({ _id: 1 }).lean();
  res.json(users);
});

app.get("/api/users/:id", async (req, res) => {
  const user = await User.findById(req.params.id).lean();
  if (!user) return res.status(404).json({ error: "user not found" });
  res.json(user);
});

app.patch("/api/users/:id", async (req, res) => {
  const updates = {};
  const { first_name, last_name, age } = req.body || {};

  if (first_name !== undefined) updates.first_name = String(first_name).trim();
  if (last_name !== undefined) updates.last_name = String(last_name).trim();
  if (age !== undefined) {
    const ageNum = Number(age);
    if (!Number.isInteger(ageNum) || ageNum < 0) {
      return res.status(400).json({ error: "age must be a non-negative integer" });
    }
    updates.age = ageNum;
  }

  if (Object.keys(updates).length === 0) return res.status(400).json({ error: "No fields to update" });
  updates.updated_at = Date.now();

  const user = await User.findByIdAndUpdate(req.params.id, updates, { new: true, runValidators: true }).lean();
  if (!user) return res.status(404).json({ error: "user not found" });
  res.json(user);
});

app.delete("/api/users/:id", async (req, res) => {
  const user = await User.findByIdAndDelete(req.params.id).lean();
  if (!user) return res.status(404).json({ error: "user not found" });
  res.json(user);
});

mongoose
  .connect(MONGO_URL)
  .then(() => {
    app.listen(PORT, () => {
      console.log(`Practice 20 API running on http://localhost:${PORT}`);
    });
  })
  .catch((e) => {
    console.error("Mongo connection error:", e);
    process.exit(1);
  });

