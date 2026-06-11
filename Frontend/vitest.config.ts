import { defineConfig } from "vitest/config";
import { resolve } from "path";

export default defineConfig({
  test: {
    environment: "node",
    include: ["**/*.{test,spec}.{ts,tsx}"],
  },
  resolve: {
    // Mirror the tsconfig "@/*" -> "./*" path alias so SUT imports resolve.
    alias: {
      "@": resolve(__dirname, "./"),
    },
  },
});
