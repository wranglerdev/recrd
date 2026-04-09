const esbuild = require("esbuild");

const baseConfig = {
  bundle: true,
  minify: process.argv.includes("--minify"),
  sourcemap: !process.argv.includes("--minify"),
  entryPoints: ["src/extension.ts"],
  outdir: "dist",
  external: ["vscode"],
  format: "cjs",
  platform: "node",
  target: "node20",
};

async function main() {
  if (process.argv.includes("--watch")) {
    const ctx = await esbuild.context(baseConfig);
    await ctx.watch();
    console.log("watching...");
  } else {
    await esbuild.build(baseConfig);
    console.log("build complete");
  }
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
