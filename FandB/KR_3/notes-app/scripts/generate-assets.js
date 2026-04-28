const fs = require("node:fs");
const path = require("node:path");
const { PNG } = require("pngjs");

const projectRoot = path.join(__dirname, "..");
const publicDir = path.join(projectRoot, "public");
const iconsDir = path.join(publicDir, "icons");
const screenshotsDir = path.join(publicDir, "screenshots");

function ensureDir(dir) {
  fs.mkdirSync(dir, { recursive: true });
}

function writeSquarePng({ filename, size, bg, fg, padRatio = 0.18 }) {
  const png = new PNG({ width: size, height: size });

  for (let y = 0; y < size; y += 1) {
    for (let x = 0; x < size; x += 1) {
      const idx = (size * y + x) << 2;
      png.data[idx] = bg.r;
      png.data[idx + 1] = bg.g;
      png.data[idx + 2] = bg.b;
      png.data[idx + 3] = 255;
    }
  }

  // Simple "N" glyph so icons aren't blank.
  const pad = Math.max(2, Math.floor(size * padRatio));
  const stroke = Math.max(2, Math.floor(size * 0.1));
  for (let y = pad; y < size - pad; y += 1) {
    // Left bar
    for (let x = pad; x < pad + stroke; x += 1) {
      const idx = (size * y + x) << 2;
      png.data[idx] = fg.r;
      png.data[idx + 1] = fg.g;
      png.data[idx + 2] = fg.b;
      png.data[idx + 3] = 255;
    }
    // Right bar
    for (let x = size - pad - stroke; x < size - pad; x += 1) {
      const idx = (size * y + x) << 2;
      png.data[idx] = fg.r;
      png.data[idx + 1] = fg.g;
      png.data[idx + 2] = fg.b;
      png.data[idx + 3] = 255;
    }
    // Diagonal
    const t = (y - pad) / (size - 2 * pad - 1);
    const xDiag = Math.round(pad + stroke + t * (size - 2 * pad - 2 * stroke));
    for (let x = xDiag; x < xDiag + stroke; x += 1) {
      if (x < pad + stroke || x >= size - pad - stroke) continue;
      const idx = (size * y + x) << 2;
      png.data[idx] = fg.r;
      png.data[idx + 1] = fg.g;
      png.data[idx + 2] = fg.b;
      png.data[idx + 3] = 255;
    }
  }

  const outPath = path.join(iconsDir, filename);
  fs.writeFileSync(outPath, PNG.sync.write(png));
  return outPath;
}

function writeScreenshot({ filename, width, height }) {
  const png = new PNG({ width, height });

  // Light background
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      const idx = (width * y + x) << 2;
      png.data[idx] = 245;
      png.data[idx + 1] = 247;
      png.data[idx + 2] = 250;
      png.data[idx + 3] = 255;
    }
  }

  // Simple dark header strip
  const headerH = Math.max(56, Math.floor(height * 0.13));
  for (let y = 0; y < headerH; y += 1) {
    for (let x = 0; x < width; x += 1) {
      const idx = (width * y + x) << 2;
      png.data[idx] = 11;
      png.data[idx + 1] = 18;
      png.data[idx + 2] = 32;
      png.data[idx + 3] = 255;
    }
  }

  // Title block in header
  const titleW = Math.min(Math.floor(width * 0.22), 240);
  const titleH = Math.min(Math.floor(headerH * 0.35), 22);
  const titleX = Math.max(16, Math.floor(width * 0.06));
  const titleY = Math.floor((headerH - titleH) / 2);
  for (let y = titleY; y < titleY + titleH; y += 1) {
    for (let x = titleX; x < titleX + titleW; x += 1) {
      const idx = (width * y + x) << 2;
      png.data[idx] = 255;
      png.data[idx + 1] = 255;
      png.data[idx + 2] = 255;
      png.data[idx + 3] = 255;
    }
  }

  // Card rectangle
  const cardX = Math.floor(width * 0.06);
  const cardY = headerH + Math.floor(height * 0.06);
  const cardW = width - cardX * 2;
  const cardH = height - cardY - Math.floor(height * 0.06);
  for (let y = cardY; y < cardY + cardH; y += 1) {
    for (let x = cardX; x < cardX + cardW; x += 1) {
      const idx = (width * y + x) << 2;
      png.data[idx] = 255;
      png.data[idx + 1] = 255;
      png.data[idx + 2] = 255;
      png.data[idx + 3] = 255;
    }
  }

  const outPath = path.join(screenshotsDir, filename);
  fs.writeFileSync(outPath, PNG.sync.write(png));
  return outPath;
}

function main() {
  ensureDir(publicDir);
  ensureDir(iconsDir);
  ensureDir(screenshotsDir);

  const bg = { r: 66, g: 133, b: 244 }; // #4285f4
  const fg = { r: 255, g: 255, b: 255 };

  const outputs = [
    writeSquarePng({ filename: "icon-48.png", size: 48, bg, fg }),
    writeSquarePng({ filename: "icon-96.png", size: 96, bg, fg }),
    writeSquarePng({ filename: "icon-192.png", size: 192, bg, fg }),
    // For maskable icons we want extra padding so system masks don't cut content.
    writeSquarePng({ filename: "icon-512.png", size: 512, bg, fg, padRatio: 0.25 }),
    writeScreenshot({ filename: "desktop-wide.png", width: 1280, height: 720 }),
    writeScreenshot({ filename: "mobile.png", width: 390, height: 844 })
  ];

  // Minimal ICO is optional; we skip it to avoid binary complexity.
  // Manifest and SW use PNGs which satisfy practice requirements.
  console.log("Generated assets:");
  for (const p of outputs) console.log("-", p);
}

main();

